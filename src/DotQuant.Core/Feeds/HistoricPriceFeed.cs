using System.Threading.Channels;
using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds;

/// <summary>
/// Base class providing foundation for historic price feeds.
/// Stores in-memory price items per timestamp.
/// </summary>
public class HistoricPriceFeed : IHistoricFeed
{
    private readonly SortedDictionary<DateTimeOffset, List<PriceItem>> _events = new();

    public Timeline Timeline => new Timeline(_events.Keys);

    public Timeframe Timeframe => _events.Count == 0
        ? Timeframe.Infinite
        : new Timeframe(_events.First().Key, _events.Last().Key, true);

    public IReadOnlyCollection<IAsset> Assets =>
        _events.Values.SelectMany(list => list.Select(item => item.Asset)).ToHashSet();

    /// <summary>
    /// Get the first event.
    /// </summary>
    public Event First()
    {
        var firstKey = _events.First().Key;
        return new Event(firstKey, _events[firstKey]);
    }

    /// <summary>
    /// Get the last event.
    /// </summary>
    public Event Last()
    {
        var lastKey = _events.Last().Key;
        return new Event(lastKey, _events[lastKey]);
    }

    /// <summary>
    /// Remove all events and release memory.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _events.Clear();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Play events into channel (replay).
    /// </summary>
    public async Task Play(ChannelWriter<Event> channel, CancellationToken ct = default)
    {
        foreach (var kvp in _events)
        {
            if (ct.IsCancellationRequested)
                break;

            var evt = new Event(kvp.Key, kvp.Value);
            await channel.WriteAsync(evt, ct);
        }
    }

    /// <summary>
    /// Add a price item to a specific timestamp.
    /// </summary>
    protected void Add(DateTimeOffset time, PriceItem action)
    {
        lock (_events)
        {
            if (!_events.TryGetValue(time, out var list))
            {
                list = new List<PriceItem>();
                _events[time] = list;
            }
            list.Add(action);
        }
    }

    /// <summary>
    /// Add multiple price items to a specific timestamp.
    /// </summary>
    protected void AddAll(DateTimeOffset time, IEnumerable<PriceItem> actions)
    {
        lock (_events)
        {
            if (!_events.TryGetValue(time, out var list))
            {
                list = new List<PriceItem>();
                _events[time] = list;
            }
            list.AddRange(actions);
        }
    }

    /// <summary>
    /// Merge another historic feed into this feed.
    /// </summary>
    public void Merge(HistoricPriceFeed feed)
    {
        foreach (var kvp in feed._events)
        {
            AddAll(kvp.Key, kvp.Value);
        }
    }

    public override string ToString()
    {
        return _events.Count == 0
            ? "events=0 assets=0"
            : $"events={_events.Count} start={_events.First().Key:u} end={_events.Last().Key:u} assets={Assets.Count}";
    }
}