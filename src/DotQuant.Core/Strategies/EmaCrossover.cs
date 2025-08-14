using DotQuant.Core.Common;
using DotQuant.Core.Traders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotQuant.Core.Strategies;

public sealed class EmaCrossover(
    ILogger<EmaCrossover> logger,
    int fastPeriod = 12,
    int slowPeriod = 26,
    decimal smoothing = 2.0m,
    int? minEvents = null,
    string priceType = "DEFAULT",
    FlexPolicyConfig? config = null) : IStrategy
{
    private readonly decimal _fastAlpha = 1.0m - (smoothing / (fastPeriod + 1));
    private readonly decimal _slowAlpha = 1.0m - (smoothing / (slowPeriod + 1));
    private readonly int _minEvents = minEvents ?? slowPeriod;
    private readonly Dictionary<IAsset, EMACalculator> _calculators = new();
    private readonly Dictionary<IAsset, decimal> _lastDirection = new();
    private readonly Dictionary<IAsset, DateTime> _lastExitTime = new();
    private readonly Dictionary<IAsset, DateTime> _lastReentryTime = new();
    private readonly FlexPolicyConfig _config = config ?? new FlexPolicyConfig();

    /// <summary>
    /// Cooldown before reentry after a recycle exit.
    /// </summary>
    private readonly TimeSpan _reentryCooldown = TimeSpan.FromHours(1);

    public List<Signal> CreateSignals(Event evt)
    {
        var signals = new List<Signal>();

        foreach (var (asset, priceItem) in evt.Prices)
        {
            var price = priceItem.GetPrice("close");
            var signal = Generate(asset, price, evt.Time.UtcDateTime);
            if (signal != null)
                signals.Add(signal);
        }

        return signals;
    }

    private Signal? Generate(IAsset asset, decimal price, DateTime eventTime)
    {
        // Initialize EMA calculator if missing
        if (!_calculators.TryGetValue(asset, out var calculator))
        {
            calculator = new EMACalculator(price, _fastAlpha, _slowAlpha);
            _calculators[asset] = calculator;
            return null; // Skip signal on first tick
        }

        calculator.AddPrice(price);
        if (calculator.Step < _minEvents) return null;

        var currentDirection = calculator.GetDirection();
        var hasLast = _lastDirection.TryGetValue(asset, out var lastDirection);

        // First valid signal after enough data
        if (!hasLast)
        {
            _lastDirection[asset] = currentDirection;

            return new Signal(asset, currentDirection, SignalType.Entry, TradeIntent.Entry);
        }

        // Detect crossover → generate Exit or Entry signal
        if (currentDirection != lastDirection)
        {
            SignalType type;
            TradeIntent intent;

            if (lastDirection == 1.0m && currentDirection == -1.0m ||
                lastDirection == -1.0m && currentDirection == 1.0m)
            {
                type = SignalType.Exit;
                intent = TradeIntent.ExitPartial; // Let trader refine this to ExitFull if needed
                _lastExitTime[asset] = eventTime;
            }
            else
            {
                type = SignalType.Entry;
                intent = TradeIntent.Entry;
            }

            _lastDirection[asset] = currentDirection;

            logger.LogInformation("[Signal] {Asset} @ {Price} | Crossover: {Direction} | Type={Type}",
                asset.Symbol, price, currentDirection, type);

            return new Signal(asset, currentDirection, type, intent);
        }

        // Reentry after recycle cooldown
        if (_config.ExitStrategy == ExitMode.Recycle &&
            lastDirection == currentDirection &&
            currentDirection != 0 &&
            _lastExitTime.TryGetValue(asset, out var lastExitTime) &&
            (eventTime - lastExitTime) >= _reentryCooldown &&
            (!_lastReentryTime.TryGetValue(asset, out var lastReentry) || (eventTime - lastReentry) >= _reentryCooldown))
        {
            _lastReentryTime[asset] = eventTime;

            logger.LogInformation("[Signal] {Asset} @ {Price} | Reentry signal after cooldown", asset.Symbol, price);

            return new Signal(asset, currentDirection, SignalType.Entry, TradeIntent.Reentry);
        }

        return null;
    }

    private sealed class EMACalculator
    {
        public decimal EmaFast { get; private set; }
        public decimal EmaSlow { get; private set; }
        public long Step { get; private set; }

        private readonly decimal _fastAlpha;
        private readonly decimal _slowAlpha;

        public EMACalculator(decimal initialPrice, decimal fastAlpha, decimal slowAlpha)
        {
            EmaFast = EmaSlow = initialPrice;
            Step = 1;
            _fastAlpha = fastAlpha;
            _slowAlpha = slowAlpha;
        }

        public void AddPrice(decimal price)
        {
            EmaFast = EmaFast * _fastAlpha + (1.0m - _fastAlpha) * price;
            EmaSlow = EmaSlow * _slowAlpha + (1.0m - _slowAlpha) * price;
            Step++;
        }

        public decimal GetDirection() => EmaFast > EmaSlow ? 1.0m : -1.0m;
    }

    // Convenient presets
    public static EmaCrossover Periods50_200 => new(NullLogger<EmaCrossover>.Instance, 50, 200);
    public static EmaCrossover Periods12_26 => new(NullLogger<EmaCrossover>.Instance, 12, 26);
    public static EmaCrossover Periods5_15 => new(NullLogger<EmaCrossover>.Instance, 5, 15);
}
