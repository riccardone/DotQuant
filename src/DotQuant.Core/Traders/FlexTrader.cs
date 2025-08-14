using DotQuant.Core.Common;
using Microsoft.Extensions.Logging;

namespace DotQuant.Core.Traders;

public class FlexTrader : Trader
{
    private readonly ILogger<FlexTrader> _logger;
    private readonly FlexPolicyConfig _config;
    private readonly Dictionary<IAsset, int> _exitHits = new();
    private readonly Dictionary<IAsset, Size> _recycledPositions = new();

    public FlexTrader(FlexPolicyConfig? config = null, ILogger<FlexTrader>? logger = null)
    {
        _config = config ?? new FlexPolicyConfig();
        _config.SafetyMargin = _config.SafetyMargin <= 0 ? _config.OrderPercentage : _config.SafetyMargin;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<FlexTrader>.Instance;
    }

    protected virtual Size CalcSize(decimal amount, Signal signal, decimal price)
    {
        if (_config.Fractions < 0)
            throw new ArgumentException($"Fractions must be >= 0, found {_config.Fractions}");

        var value = signal.Asset.Value(Size.One, price).Value;
        var rawSize = amount / value;

        if (_config.Fractions == 0)
            return new Size(Math.Floor(rawSize));

        var factor = DecimalPow(_config.Fractions);
        var floored = Math.Floor(rawSize * factor) / factor;
        return new Size(floored);
    }

    private static decimal DecimalPow(int exponent)
    {
        decimal result = 1m;
        for (int i = 0; i < exponent; i++)
            result *= 10;
        return result;
    }

    protected virtual Order? CreateOrder(Signal signal, Size size, PriceItem priceItem)
    {
        return new Order(signal.Asset, size, priceItem.GetPrice());
    }

    protected virtual Amount AmountPerOrder(IAccount account)
    {
        return new Amount(account.BaseCurrency, account.EquityAmount().Value * _config.OrderPercentage);
    }

    private bool MeetsMinPrice(IAsset asset, decimal price, DateTime time)
    {
        return _config.MinPrice == null || _config.MinPrice.Convert(asset.Currency, time) <= price;
    }

    private decimal GetDynamicExitFraction(IAsset asset)
    {
        if (!_exitHits.ContainsKey(asset))
            _exitHits[asset] = 0;

        _exitHits[asset]++;
        return Math.Min(1m, _config.ExitFraction * _exitHits[asset]);
    }

    public override List<Order> CreateOrders(List<Signal> signals, IAccount account, Event evt)
    {
        var orders = new List<Order>();
        if (signals == null || signals.Count == 0) return orders;

        var equity = account.EquityAmount();
        var safety = equity.Value * _config.SafetyMargin;
        var time = evt.Time;
        var buyingPower = account.CashAmount.Value - safety;
        var amountPerOrder = AmountPerOrder(account);

        foreach (var signal in signals)
        {
            var asset = signal.Asset;
            var hasPosition = account.Positions.TryGetValue(asset, out var position);
            position ??= Position.Empty();

            if (!evt.Prices.TryGetValue(asset, out var priceItem))
            {
                Log(signal, null, position, "No price available");
                continue;
            }

            if (_config.OneOrderOnly && account.Orders.Any(o => o.Asset == asset))
            {
                Log(signal, priceItem, position, "One order only");
                continue;
            }

            var price = priceItem.GetPrice(_config.PriceType);

            if (signal.Exit && position.Size.Quantity > 0)
            {
                Size exitSize;

                switch (_config.ExitStrategy)
                {
                    case ExitMode.Full:
                        exitSize = new Size(-position.Size.Quantity);
                        _logger.LogInformation("Full exit for {Asset}: qty={Qty}", asset.Symbol, exitSize.Quantity);
                        break;

                    case ExitMode.Layered:
                        var layeredFraction = GetDynamicExitFraction(asset);
                        var layeredQty = Math.Max(1, (int)(position.Size.Quantity * layeredFraction));
                        exitSize = new Size(-layeredQty);
                        _logger.LogInformation("Layered exit for {Asset}: qty={Qty} of total={Total}",
                            asset.Symbol, layeredQty, position.Size.Quantity);
                        break;

                    case ExitMode.Recycle:
                        var recycleFraction = GetDynamicExitFraction(asset);
                        var recycleQty = Math.Max(1, (int)(position.Size.Quantity * recycleFraction));
                        exitSize = new Size(-recycleQty);

                        if (!_recycledPositions.ContainsKey(asset) ||
                            position.Size.Quantity > _recycledPositions[asset].Quantity)
                        {
                            _recycledPositions[asset] = position.Size;
                            _logger.LogDebug("Tracking recycled position for {Asset}: {Qty}", asset.Symbol, position.Size.Quantity);
                        }

                        _logger.LogInformation("Recycling exit for {Asset}: qty={Qty} of total={Total}, fraction={Fraction:P1}",
                            asset.Symbol, recycleQty, position.Size.Quantity, recycleFraction);
                        break;

                    default:
                        exitSize = new Size(-position.Size.Quantity);
                        break;
                }

                var exitOrder = CreateOrder(signal, exitSize, priceItem);
                if (exitOrder != null)
                {
                    orders.Add(exitOrder);

                    if (_config.ExitStrategy == ExitMode.Recycle)
                    {
                        var exposureReleased = asset.Value(new Size(Math.Abs(exitSize.Quantity)), price)
                            .Convert(account.BaseCurrency, time.DateTime);
                        buyingPower += exposureReleased;
                        _logger.LogInformation("Recycled {Amount} buying power from {Asset}", exposureReleased, asset.Symbol);
                    }
                }

                continue;
            }

            var canScaleIn = !_config.AllowScaleInAfterRecycle || CanScaleIn(position, asset);

            if (!signal.Entry || amountPerOrder.Value > buyingPower || !canScaleIn)
            {
                Log(signal, priceItem, position, "Skipped: Entry blocked or insufficient buying power or scale-in condition not met");
                continue;
            }

            var assetAmount = amountPerOrder.Convert(asset.Currency, time.DateTime);
            var size = CalcSize(assetAmount, signal, price);
            if (size.IsZero || (size.IsNegative && !_config.Shorting) || !MeetsMinPrice(asset, price, time.DateTime)) continue;

            var newOrder = CreateOrder(signal, size, priceItem);
            if (newOrder == null) continue;

            var assetExposure = asset.Value(size, price);
            var exposure = assetExposure.Convert(account.BaseCurrency, time.DateTime);
            buyingPower -= exposure;
            orders.Add(newOrder);
        }

        return orders;
    }

    private bool CanScaleIn(Position position, IAsset asset)
    {
        // Allow all entries unless we've tracked a recycled size before
        if (!_recycledPositions.TryGetValue(asset, out var recycledSize))
        {
            _logger.LogDebug("CanScaleIn: no recycled position tracked for {Asset}, allowing entry", asset.Symbol);
            return true;
        }

        var result = position.Size.Quantity < recycledSize.Quantity;
        _logger.LogDebug("CanScaleIn: posQty={Qty}, recycledQty={RecycledQty}, result={Result}",
            position.Size.Quantity, recycledSize.Quantity, result);
        return result;
    }

    private void Log(Signal signal, PriceItem? price, Position position, string reason)
    {
        _logger.LogInformation("Signal={Signal}, Price={Price}, Position={Position}, Reason={Reason}", signal, price, position, reason);
    }
}
