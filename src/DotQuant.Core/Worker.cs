using System.Text;
using DotQuant.Core.Brokers;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Journals;
using DotQuant.Core.Strategies;
using DotQuant.Core.Traders;
using Microsoft.Extensions.Logging;

namespace DotQuant.Core;

public class Worker
{
    public static IAccount Run(
        ILoggerFactory loggerFactory,
        IFeed feed,
        IStrategy strategy,
        IBroker broker, // Required now
        Journal? journal = null,
        Trader? trader = null,
        Timeframe? timeframe = null,
        EventChannel? eventChannel = null,
        int timeoutMillis = -1,
        bool showProgressBar = false)
    {
        var logger = loggerFactory.CreateLogger<Worker>();

        trader ??= new FlexTrader(logger: loggerFactory.CreateLogger<FlexTrader>());
        timeframe ??= Timeframe.Infinite;
        eventChannel ??= new EventChannel(timeframe);
        journal ??= new BasicJournal(loggerFactory.CreateLogger<BasicJournal>(), logProgress: true);

        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            logger.LogWarning("Cancellation requested via Ctrl+C.");
            cts.Cancel();
        };

        return RunAsync(feed, strategy, journal, trader, timeframe, broker, eventChannel,
                        timeoutMillis, showProgressBar, logger, cts.Token)
            .GetAwaiter().GetResult();
    }

    private static async Task<IAccount> RunAsync(
        IFeed feed,
        IStrategy strategy,
        Journal journal,
        Trader trader,
        Timeframe timeframe,
        IBroker broker,
        EventChannel eventChannel,
        int timeoutMillis,
        bool showProgressBar,
        ILogger<Worker> logger,
        CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = linkedCts.Token;

        logger.LogInformation("Starting trading session...");

        var feedTask = feed.PlayBackground(eventChannel, token);
        ProgressBar? progressBar = null;

        if (showProgressBar)
        {
            var tf = timeframe.IsFinite ? timeframe : feed.Timeframe;
            progressBar = new ProgressBar(tf);
            progressBar.Start();
        }

        try
        {
            while (await eventChannel.Reader.WaitToReadAsync(token))
            {
                while (eventChannel.Reader.TryRead(out var evt))
                {
                    if (evt == null) continue;

                    progressBar?.Update(evt.Time);

                    var signals = strategy.CreateSignals(evt) ?? new List<Signal>();

                    if (signals.Any())
                    {
                        var summary = string.Join(", ", signals.Select(s =>
                            $"{s.Asset.Symbol} {s.Type}({s.Intent}) Dir={s.Rating:+0.0;-0.0;0}"));
                        logger.LogInformation("Signals at {time} [{count}]: {summary}",
                            evt.Time, signals.Count, summary);
                    }

                    var preTradeAccount = broker.Sync();
                    var orders = trader.CreateOrders(signals, preTradeAccount, evt);
                    broker.PlaceOrders(orders);

                    var postTradeAccount = broker.Sync(evt);
                    journal.Track(evt, postTradeAccount, signals, orders);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Trading session cancelled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during session.");
        }
        finally
        {
            if (!feedTask.IsCompleted)
                linkedCts.Cancel();

            progressBar?.Stop();
            logger.LogInformation("Session complete. Finalizing...");
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
