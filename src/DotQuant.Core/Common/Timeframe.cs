using System.Globalization;

namespace DotQuant.Core.Common;

public sealed class Timeframe : IComparable<TimeSpan>
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
    public bool Inclusive { get; }

    public static readonly DateTimeOffset Min = new(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset Max = new(2200, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly Timeframe Infinite = new(Min, Max, true);
    public static readonly Timeframe Empty = new(Min, Min, false);

    public TimeSpan Duration => End - Start;

    public Timeframe(DateTimeOffset start, DateTimeOffset end, bool inclusive = false)
    {
        if (end < start)
            throw new ArgumentException($"End time must be after start time, found {start:u} - {end:u}");
        if (start < Min)
            throw new ArgumentException($"Start must be after {Min:u}");
        if (end > Max)
            throw new ArgumentException($"End must be before {Max:u}");

        Start = start;
        End = end;
        Inclusive = inclusive;
    }

    public bool IsInfinite() => Equals(Infinite);
    public bool IsFinite => !Equals(Infinite);
    public bool IsEmpty() => Start == End && !Inclusive;

    public static Timeframe Parse(string first, string last, bool inclusive = false)
    {
        var start = ToInstant(first);
        var end = ToInstant(last);
        return new Timeframe(start, end, inclusive);
    }

    public static Timeframe FromYears(int first, int last)
    {
        var start = new DateTimeOffset(first, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(last, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return new Timeframe(start, end);
    }

    private static DateTimeOffset ToInstant(string value)
    {
        return value.Length switch
        {
            4 => DateTimeOffset.ParseExact($"{value}-01-01T00:00:00Z", "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            7 => DateTimeOffset.ParseExact($"{value}-01T00:00:00Z", "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            10 => DateTimeOffset.ParseExact($"{value}T00:00:00Z", "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            19 => DateTimeOffset.ParseExact($"{value}Z", "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            _ => DateTimeOffset.Parse(value, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
        };
    }

    public bool Contains(DateTimeOffset time)
    {
        if (IsInfinite()) return true;
        return time >= Start && (time < End || (Inclusive && time == End));
    }

    public Timeframe ToInclusive() => new(Start, End, true);

    public bool IsSingleDay(TimeZoneInfo zone)
    {
        var startLocal = TimeZoneInfo.ConvertTime(Start, zone).Date;
        var endAdjusted = Inclusive ? End : End.AddTicks(-1);
        var endLocal = TimeZoneInfo.ConvertTime(endAdjusted, zone).Date;
        return startLocal == endLocal;
    }

    public List<DateTimeOffset> ToTimeline(TimeSpan step)
    {
        var times = new List<DateTimeOffset>();
        var time = Start;
        while (Contains(time))
        {
            times.Add(time);
            time = time.Add(step);
        }
        return times;
    }

    public (Timeframe train, Timeframe test) SplitTwoWay(double testSize)
    {
        if (testSize is < 0.0 or > 1.0)
            throw new ArgumentException("Test size must be between 0.0 and 1.0");

        var diff = Duration.TotalMilliseconds;
        var trainMillis = diff * (1.0 - testSize);
        var border = Start.AddMilliseconds(trainMillis);
        return (new Timeframe(Start, border), new Timeframe(border, End, Inclusive));
    }

    public (Timeframe first, Timeframe second) SplitTwoWay(TimeSpan offset, TimeSpan? overlap = null)
    {
        var border = Start.Add(offset);
        if (!Contains(border))
            throw new ArgumentException("Offset must be smaller than timeframe");

        var overlapValue = overlap ?? TimeSpan.Zero;
        return (new Timeframe(Start, border), new Timeframe(border - overlapValue, End, Inclusive));
    }

    public List<Timeframe> Split(TimeSpan period, TimeSpan? overlap = null, bool includeRemaining = true)
    {
        var tfs = new List<Timeframe>();
        var begin = Start;
        var overlapValue = overlap ?? TimeSpan.Zero;

        while (true)
        {
            var last = begin.Add(period);
            Timeframe? tf = null;

            if (last < End)
                tf = new Timeframe(begin, last);
            else if (last == End && Inclusive)
                tf = new Timeframe(begin, End, true);
            else if (includeRemaining)
                tf = new Timeframe(begin, End, Inclusive);

            if (tf == null) break;

            tfs.Add(tf);
            if (tf.End == End) break;
            begin = last - overlapValue;
        }

        return tfs;
    }

    public Timeframe Minus(TimeSpan period)
    {
        var newStart = Clamp(Start - period);
        var newEnd = Clamp(End - period);
        return new Timeframe(newStart, newEnd, Inclusive);
    }

    public Timeframe Plus(TimeSpan period)
    {
        var newStart = Clamp(Start + period);
        var newEnd = Clamp(End + period);
        return new Timeframe(newStart, newEnd, Inclusive);
    }

    public Timeframe Extend(TimeSpan before, TimeSpan? after = null)
    {
        var afterValue = after ?? before;
        var newStart = Clamp(Start - before);
        var newEnd = Clamp(End + afterValue);
        return new Timeframe(newStart, newEnd, Inclusive);
    }

    private static DateTimeOffset Clamp(DateTimeOffset time)
    {
        if (time < Min) return Min;
        if (time > Max) return Max;
        return time;
    }

    public override string ToString()
    {
        var s1 = Start == Min ? "MIN" : Start.ToString("o");
        var s2 = End == Max ? "MAX" : End.ToString("o");
        var endChar = Inclusive ? "]" : ">";
        return $"[{s1} - {s2}{endChar}";
    }

    public string ToPrettyString()
    {
        var d = Duration.TotalSeconds;
        string fmt = d switch
        {
            < 10 => "yyyy-MM-dd HH:mm:ss.fff",
            < 86400 => "yyyy-MM-dd HH:mm:ss",
            _ => "yyyy-MM-dd"
        };

        var s1 = Start == Min ? "MIN" : Start.ToUniversalTime().ToString(fmt, CultureInfo.InvariantCulture);
        var s2 = End == Max ? "MAX" : End.ToUniversalTime().ToString(fmt, CultureInfo.InvariantCulture);
        return $"{s1} - {s2}";
    }

    public double Annualize(double rate)
    {
        return Math.Pow(1.0 + rate, ToYears()) - 1.0;
    }

    private double ToYears()
    {
        const double oneYearMillis = 365.0 * 24.0 * 3600.0 * 1000.0;
        return oneYearMillis / Duration.TotalMilliseconds;
    }

    public int CompareTo(TimeSpan other)
    {
        return Duration.CompareTo(other);
    }
}