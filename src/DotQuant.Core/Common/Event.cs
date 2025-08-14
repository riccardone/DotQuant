namespace DotQuant.Core.Common;

public sealed class Event : IComparable<Event>
{
    public DateTimeOffset Time { get; init; }
    public IReadOnlyList<PriceItem> Items { get; init; }

    private readonly Lazy<Dictionary<IAsset, PriceItem>> _prices;

    public IReadOnlyDictionary<IAsset, PriceItem> Prices => _prices.Value;

    public Event(DateTimeOffset time, IReadOnlyList<PriceItem> items)
    {
        Time = time;
        Items = items;
        _prices = new Lazy<Dictionary<IAsset, PriceItem>>(() =>
        {
            var result = new Dictionary<IAsset, PriceItem>(Items.Count);
            foreach (var action in Items)
            {
                if (action is PriceItem priceItem)
                {
                    result[priceItem.Asset] = priceItem;
                }
            }
            return result;
        });
    }

    public decimal? GetPrice(IAsset asset, string type = "DEFAULT")
    {
        return Prices.TryGetValue(asset, out var priceItem) ? priceItem.GetPrice(type) : null;
    }

    public bool IsNotEmpty() => Items.Count > 0;

    public bool IsEmpty() => Items.Count == 0;

    public int CompareTo(Event? other)
    {
        if (other is null) return 1;
        return Time.CompareTo(other.Time);
    }

    public override string ToString() => $"Event(time={Time}, actions={Items.Count})";

    public static Event Empty(DateTimeOffset? time = null)
    {
        return new Event(time ?? DateTimeOffset.UtcNow, Array.Empty<PriceItem>());
    }
}

// This is a Stub... if not needed, remove
//public abstract class Item { }

//public class PriceItem : Item
//{
//    public IAsset Asset { get; }

//    public PriceItem(IAsset asset)
//    {
//        Asset = asset;
//    }

//    public double GetPrice(string type = "DEFAULT")
//    {
//        // Example
//        return 100.0;
//    }
//}