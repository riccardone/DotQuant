using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds.Csv;

public class PriceEntry : IComparable<PriceEntry>
{
    public DateTimeOffset Time { get; }
    public PriceItem Price { get; }

    public PriceEntry(DateTimeOffset time, PriceItem price)
    {
        Time = time;
        Price = price;
    }

    public int CompareTo(PriceEntry? other)
    {
        if (other is null) return 1;
        return Time.CompareTo(other.Time);
    }
}