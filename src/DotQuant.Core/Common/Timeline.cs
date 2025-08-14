namespace DotQuant.Core.Common;

/// <summary>
/// Timeline is an ordered list of DateTimeOffset instances, sorted from old to new.
/// </summary>
public class Timeline : List<DateTimeOffset>
{
    public Timeline() { }

    public Timeline(IEnumerable<DateTimeOffset> times) : base(times.OrderBy(x => x))
    {
    }

    /// <summary>
    /// Get the overall timeframe covered by this timeline.
    /// </summary>
    public Timeframe Timeframe
        => Count == 0 ? Timeframe.Empty : new Timeframe(this.First(), this.Last(), inclusive: true);

    /// <summary>
    /// Sample random timeframes of a certain size.
    /// </summary>
    public List<Timeframe> Sample(int size, int samples = 1, Random? random = null)
    {
        if (size < 1 || size >= Count)
            throw new ArgumentException("Invalid size for sampling");

        var rnd = random ?? new Random();
        var maxStart = Count - size;
        var result = new List<Timeframe>();

        for (int i = 0; i < samples; i++)
        {
            var start = rnd.Next(0, maxStart);
            var tf = new Timeframe(this[start], this[start + size]);
            result.Add(tf);
        }

        return result;
    }

    /// <summary>
    /// Return the index of the latest time not after the provided time.
    /// </summary>
    public int? LatestNotAfter(DateTimeOffset time)
    {
        var idx = BinarySearch(time);
        idx = idx < 0 ? ~idx - 1 : idx;
        return idx >= 0 ? idx : null;
    }

    /// <summary>
    /// Return the index of the earliest time not before the provided time.
    /// </summary>
    public int? EarliestNotBefore(DateTimeOffset time)
    {
        var idx = BinarySearch(time);
        idx = idx < 0 ? ~idx : idx;
        return idx < Count ? idx : null;
    }

    /// <summary>
    /// Split the timeline in chunks of given size and return timeframes.
    /// </summary>
    public List<Timeframe> Split(int size)
    {
        if (size <= 1)
            throw new ArgumentException("Size must be at least 2");

        var chunks = this
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / size)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();

        var timeframes = new List<Timeframe>();

        foreach (var chunk in chunks)
        {
            if (chunk.Count > 1)
                timeframes.Add(new Timeframe(chunk.First(), chunk.Last()));
        }

        if (timeframes.Count > 0)
            timeframes[^1] = timeframes[^1].ToInclusive();

        return timeframes;
    }

    /// <summary>
    /// Binary search helper for DateTimeOffset.
    /// </summary>
    private int BinarySearch(DateTimeOffset time)
    {
        int left = 0;
        int right = Count - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            var cmp = this[mid].CompareTo(time);

            if (cmp == 0)
                return mid;
            if (cmp < 0)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return ~left;
    }
}