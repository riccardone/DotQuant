using System.Threading.Channels;
using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds;

public static class FeedExtensions
{
    public static async Task<List<(DateTimeOffset, T)>> FilterAsync<T>(
        this IFeed feed,
        Timeframe? timeframe = null,
        Func<T, bool>? filter = null,
        CancellationToken ct = default
    ) where T : IItem
    {
        var channel = Channel.CreateUnbounded<Event>();
        var results = new List<(DateTimeOffset, T)>();
        var playTask = feed.PlayBackground(channel.Writer, ct);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            {
                var items = evt.Items.OfType<T>();
                if (filter != null)
                    items = items.Where(filter);

                results.AddRange(items.Select(x => (evt.Time, x)));
            }
        }
        finally
        {
            channel.Writer.Complete();
            await playTask;
        }

        return results;
    }

    public static async Task ApplyAsync<T>(
        this IFeed feed,
        Func<T, DateTimeOffset, Task> action,
        Timeframe? timeframe = null,
        CancellationToken ct = default
    ) where T : IItem
    {
        var channel = Channel.CreateUnbounded<Event>();
        var playTask = feed.PlayBackground(channel.Writer, ct);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            {
                foreach (var item in evt.Items.OfType<T>())
                    await action(item, evt.Time);
            }
        }
        finally
        {
            channel.Writer.Complete();
            await playTask;
        }
    }

    public static async Task ApplyEventsAsync(
        this IFeed feed,
        Func<Event, Task> action,
        Timeframe? timeframe = null,
        CancellationToken ct = default
    )
    {
        var channel = Channel.CreateUnbounded<Event>();
        var playTask = feed.PlayBackground(channel.Writer, ct);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            {
                await action(evt);
            }
        }
        finally
        {
            channel.Writer.Complete();
            await playTask;
        }
    }

    public static async Task<List<Event>> ToListAsync(
        this IFeed feed,
        Timeframe? timeframe = null,
        CancellationToken ct = default
    )
    {
        var channel = Channel.CreateUnbounded<Event>();
        var results = new List<Event>();
        var playTask = feed.PlayBackground(channel.Writer, ct);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            {
                results.Add(evt);
            }
        }
        finally
        {
            channel.Writer.Complete();
            await playTask;
        }

        return results;
    }

    public static async Task<List<(DateTimeOffset, PriceItem)>> ValidateAsync(
        this IFeed feed,
        decimal maxDiff = 0.5m,
        string priceType = "DEFAULT",
        Timeframe? timeframe = null,
        CancellationToken ct = default
    )
    {
        var channel = Channel.CreateUnbounded<Event>();
        var playTask = feed.PlayBackground(channel.Writer, ct);

        var lastPrices = new Dictionary<IAsset, decimal>();
        var errors = new List<(DateTimeOffset, PriceItem)>();

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            {
                foreach (var (asset, priceItem) in evt.Prices)
                {
                    var price = priceItem.GetPrice(priceType);
                    if (lastPrices.TryGetValue(asset, out var prev))
                    {
                        var diff = (price - prev) / prev;
                        if (Math.Abs(diff) > maxDiff)
                            errors.Add((evt.Time, priceItem));
                    }
                    lastPrices[asset] = price;
                }
            }
        }
        finally
        {
            channel.Writer.Complete();
            await playTask;
        }

        return errors;
    }
}