using DotQuant.Core.Common;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace DotQuant.Core.Feeds;

/// <summary>
/// Base abstract class for live data feeds.
/// Feeds data in near real-time and supports pushing events to multiple listeners.
/// </summary>
public abstract class LiveFeed : IFeed
{
    private readonly List<ChannelWriter<Event>> _channels = new();
    private readonly object _lock = new();
    private IMarketStatusService? _marketStatusService;
    private ILogger? _logger;

    /// <summary>
    /// Enables per-symbol market status checking for derived live feeds.
    /// Should be called during feed construction.
    /// </summary>
    protected void EnableMarketStatus(IMarketStatusService marketStatusService, ILogger logger)
    {
        _marketStatusService = marketStatusService;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the market for the given symbol is currently open.
    /// Returns false if market status service is not configured.
    /// </summary>
    protected async Task<bool> IsMarketOpenAsync(Symbol symbol, CancellationToken ct)
    {
        if (_marketStatusService == null)
            return true; // allow by default

        var isOpen = await _marketStatusService.IsMarketOpenAsync(symbol, ct);
        if (!isOpen)
            _logger?.LogInformation("Market is closed for {Symbol}, skipping tick.", symbol);
        else
            _logger?.LogDebug("Market is open for {Symbol}.", symbol);

        return isOpen;
    }

    /// <summary>
    /// Returns true if the live feed is active (has any channels).
    /// </summary>
    public bool IsActive
    {
        get
        {
            lock (_lock)
            {
                return _channels.Count > 0;
            }
        }
    }

    /// <summary>
    /// Send an event to all active channels. Blocks until all writes are complete.
    /// </summary>
    protected void Send(Event evt)
    {
        SendAsync(evt).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Send an event to all active channels asynchronously.
    /// </summary>
    protected async Task SendAsync(Event evt)
    {
        List<ChannelWriter<Event>> currentChannels;
        lock (_lock)
        {
            currentChannels = _channels.ToList();
        }

        var closedChannels = new List<ChannelWriter<Event>>();

        foreach (var channel in currentChannels)
        {
            try
            {
                await channel.WriteAsync(evt);
            }
            catch (ChannelClosedException)
            {
                closedChannels.Add(channel);
            }
        }

        if (closedChannels.Count > 0)
        {
            lock (_lock)
            {
                _channels.RemoveAll(ch => closedChannels.Contains(ch));
            }
        }
    }

    /// <summary>
    /// Start playing to the given channel (registers it and waits for its close).
    /// </summary>
    public virtual async Task PlayAsync(ChannelWriter<Event> channel, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _channels.Add(channel);
        }

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            // Expected, no action
        }
        finally
        {
            lock (_lock)
            {
                _channels.Remove(channel);
            }
        }
    }

    /// <summary>
    /// Dispose logic (if needed).
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            _channels.Clear();
        }
        await Task.CompletedTask;
    }
}