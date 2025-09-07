using System.Globalization;
using System.Text;
using DotQuant.Core.Brokers;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Journals;
using DotQuant.Core.Services;
using DotQuant.Core.Services.GraphModels;
using DotQuant.Core.Strategies;
using DotQuant.Core.Traders;
using Microsoft.Extensions.Logging;

namespace DotQuant.Core;

public class Worker
{
    private readonly ILogger<Worker> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly InMemorySessionGraphProvider _sessionGraph;

    public Worker(ILoggerFactory loggerFactory, ISessionGraphProvider sessionGraph)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<Worker>();
        _sessionGraph = sessionGraph as InMemorySessionGraphProvider
                        ?? throw new ArgumentException("Worker requires InMemorySessionGraphProvider");
    }

    public async Task<IAccount> RunAsync(
        IFeed feed,
        IStrategy strategy,
        IBroker broker,
        Journal? journal = null,
        Trader? trader = null,
        Timeframe? timeframe = null,
        EventChannel? eventChannel = null,
        int timeoutMillis = -1,
        bool showProgressBar = false,
        CancellationToken cancellationToken = default)
    {
        trader ??= new FlexTrader(logger: _loggerFactory.CreateLogger<FlexTrader>());
        timeframe ??= Timeframe.Infinite;
        eventChannel ??= new EventChannel(timeframe);
        journal ??= new BasicJournal(_loggerFactory.CreateLogger<BasicJournal>(), logProgress: true);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = linkedCts.Token;

        _logger.LogInformation("Starting trading session...");

        var feedTask = feed.PlayBackgroundAsync(eventChannel, token);
        ProgressBar? progressBar = null;

        if (showProgressBar)
        {
            var tf = timeframe.IsFinite ? timeframe : feed.Timeframe;
            progressBar = new ProgressBar(tf);
            progressBar.Start();
        }

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (await eventChannel.Reader.WaitToReadAsync(token))
                {
                    while (eventChannel.Reader.TryRead(out var evt))
                    {
                        if (evt == null || evt.IsEmpty()) continue;

                        progressBar?.Update(evt.Time);

                        var signals = strategy.CreateSignals(evt) ?? new List<Signal>();

                        foreach (var priceItem in evt.Prices)
                        {
                            var price = priceItem.Value;
                            _logger.LogInformation("Tick: {Symbol} OHLC: {Open}/{High}/{Low}/{Close} (Time: {Time})",
                                price.Asset.Symbol, price.Open, price.High, price.Low, price.Close,
                                evt.Time.UtcDateTime.ToString("HH:mm:ss"));

                            _sessionGraph.AddPrice(new PricePoint(
                                Ticker: priceItem.Key.Symbol,
                                Time: evt.Time.DateTime,
                                Open: price.Open,
                                High: price.High,
                                Low: price.Low,
                                Close: price.Close
                            ));
                        }

                        if (signals.Any())
                        {
                            var summary = string.Join(", ", signals.Select(s =>
                                $"{s.Asset.Symbol} {s.Type}({s.Intent}) Dir={s.Rating:+0.0;-0.0;0}"));

                            _logger.LogInformation("Signals at {time} [{count}]: {summary}",
                                evt.Time.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                                signals.Count, summary);

                            foreach (var signal in signals)
                            {
                                _sessionGraph.AddSignal(new SignalPoint(
                                    Ticker: signal.Asset.Symbol,
                                    Time: evt.Time.DateTime,
                                    Type: signal.Type.ToString(),
                                    Confidence: (int)Math.Abs(signal.Rating)
                                ));
                            }
                        }

                        var preTradeAccount = broker.Sync();
                        var orders = trader.CreateOrders(signals, preTradeAccount, evt);

                        broker.PlaceOrders(orders);

                        foreach (var order in orders)
                        {
                            _sessionGraph.AddOrder(new OrderPoint(
                                Ticker: order.Asset.Symbol, 
                                Time: evt.Time.DateTime,
                                Side: order.Buy ? "Buy" : order.Sell ? "Sell" : "Hold",
                                Price: order.Limit,
                                Quantity: order.Size.Quantity
                            ));
                        }

                        var postTradeAccount = broker.Sync(evt);
                        journal.Track(evt, postTradeAccount, signals, orders);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Trading session cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during session.");
        }
        finally
        {
            if (!feedTask.IsCompleted)
                linkedCts.Cancel();

            progressBar?.Stop();
            _logger.LogInformation("Session complete. Finalizing...");
        }

        return broker.Sync();
    }

    private class ProgressBar
    {
        private const int TotalBarLength = 36;
        private readonly Timeframe _timeframe;
        private int _currentPercent = -1;
        private readonly char _progressChar;
        private readonly string _pre;
        private string _lastOutput = "";
        private DateTimeOffset _nextUpdate = DateTimeOffset.MinValue;

        public ProgressBar(Timeframe timeframe)
        {
            _timeframe = timeframe;
            _progressChar = GetProgressChar();
            _pre = $"{timeframe.ToPrettyString()} | ";
        }

        public void Start() => Draw();

        public void Update(DateTimeOffset currentTime)
        {
            var total = _timeframe.Duration.TotalSeconds;
            var current = (currentTime - _timeframe.Start).TotalSeconds;
            var percent = (int)Math.Min(100.0, Math.Round(current * 100.0 / total));

            if (percent == _currentPercent) return;

            var now = DateTimeOffset.UtcNow;
            if (now < _nextUpdate) return;

            _nextUpdate = now.AddMilliseconds(500);
            _currentPercent = percent;
            Draw();
        }

        private void Draw()
        {
            var sb = new StringBuilder(100);
            sb.Append('\r').Append(_pre);
            sb.Append($"{_currentPercent,3}% |");

            int filled = _currentPercent * TotalBarLength / 100;
            for (int i = 0; i < TotalBarLength; i++)
                sb.Append(i <= filled ? _progressChar : ' ');

            if (_currentPercent == 100)
                sb.Append('\n');

            var output = sb.ToString();
            if (output != _lastOutput)
            {
                Console.Write(output);
                _lastOutput = output;
                Console.Out.Flush();
            }
        }

        public void Stop()
        {
            if (_currentPercent < 100)
            {
                _currentPercent = 100;
                Draw();
                Console.Out.Flush();
            }
        }

        private static char GetProgressChar()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT ? '=' : '█';
        }
    }
}
