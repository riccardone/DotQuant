using System.Globalization;
using System.Text.RegularExpressions;

namespace DotQuant.Core.Feeds.Csv;

/// <summary>
/// Interface for time parsers that parse a line of columns into an Instant (DateTimeOffset).
/// </summary>
public interface ITimeParser
{
    void Init(string[] header) { /* optional */ }

    DateTimeOffset Parse(string[] line);
}

internal interface IAutoDetectParser
{
    DateTimeOffset Parse(string text);
}

internal class LocalTimeParser : IAutoDetectParser
{
    private readonly string _pattern;
    private readonly Exchange _exchange;
    private readonly DateTimeFormatInfo _dtf = CultureInfo.InvariantCulture.DateTimeFormat;

    public LocalTimeParser(string pattern, Exchange? exchange = null)
    {
        _pattern = pattern;
        _exchange = exchange ?? Exchange.US;
    }

    public DateTimeOffset Parse(string text)
    {
        var dt = DateTime.ParseExact(text, _pattern, _dtf, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        return _exchange.GetInstant(dt);
    }
}

internal class LocalDateParser : IAutoDetectParser
{
    private readonly string _pattern;
    private readonly Exchange _exchange;
    private readonly DateTimeFormatInfo _dtf = CultureInfo.InvariantCulture.DateTimeFormat;

    public LocalDateParser(string pattern, Exchange? exchange = null)
    {
        _pattern = pattern;
        _exchange = exchange ?? Exchange.US;
    }

    public DateTimeOffset Parse(string text)
    {
        var date = DateTime.ParseExact(text, _pattern, _dtf);
        return _exchange.GetClosingTime(date);
    }
}

public class AutoDetectTimeParser : ITimeParser
{
    private int _timeColumn;
    private readonly Exchange _exchange;
    private IAutoDetectParser? _parser;

    public AutoDetectTimeParser(int timeColumn = -1, Exchange? exchange = null)
    {
        _timeColumn = timeColumn;
        _exchange = exchange ?? Exchange.US;
    }

    public void Init(string[] header)
    {
        if (_timeColumn != -1) return;

        var notCapital = new Regex("[^A-Z]");
        for (int i = 0; i < header.Length; i++)
        {
            var clean = notCapital.Replace(header[i].ToUpperInvariant(), "");
            if (clean is "TIME" or "DATE" or "DAY" or "DATETIME" or "TIMESTAMP")
            {
                _timeColumn = i;
                break;
            }
        }
    }

    public DateTimeOffset Parse(string[] line)
    {
        var text = line[_timeColumn];
        if (_parser == null)
            Detect(text);
        return _parser!.Parse(text);
    }

    private void Detect(string sample)
    {
        lock (this)
        {
            if (_parser != null) return;

            foreach (var (pattern, parser) in Patterns)
            {
                if (pattern.IsMatch(sample))
                {
                    _parser = parser;
                    return;
                }
            }
            throw new ConfigurationException($"No suitable time parser found for time={sample}");
        }
    }

    private static readonly List<(Regex, IAutoDetectParser)> Patterns = new()
    {
        (new Regex(@"19\d{6}"), new LocalDateParser("yyyyMMdd")),
        (new Regex(@"20\d{6}"), new LocalDateParser("yyyyMMdd")),
        (new Regex(@"\d{8} \d{6}"), new LocalTimeParser("yyyyMMdd HHmmss")),
        (new Regex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z"), new InstantParser()),
        (new Regex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}"), new LocalTimeParser("yyyy-MM-dd HH:mm:ss")),
        (new Regex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}"), new LocalTimeParser("yyyy-MM-dd HH:mm")),
        (new Regex(@"\d{4}-\d{2}-\d{2}"), new LocalDateParser("yyyy-MM-dd")),
        (new Regex(@"\d{8} \d{2}:\d{2}:\d{2}"), new LocalTimeParser("yyyyMMdd HH:mm:ss")),
        (new Regex(@"\d{8}  \d{2}:\d{2}:\d{2}"), new LocalTimeParser("yyyyMMdd  HH:mm:ss")),
        (new Regex(@"-?\d{1,19}"), new EpochMillisParser())
    };
}

internal class InstantParser : IAutoDetectParser
{
    public DateTimeOffset Parse(string text)
    {
        return DateTimeOffset.Parse(text, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}

internal class EpochMillisParser : IAutoDetectParser
{
    public DateTimeOffset Parse(string text)
    {
        var ms = long.Parse(text);
        return DateTimeOffset.FromUnixTimeMilliseconds(ms);
    }
}

/// <summary>
/// Example Exchange helper to support exchange time conversion (stub).
/// You must implement GetInstant and GetClosingTime in your own code.
/// </summary>
public class Exchange
{
    public static readonly Exchange US = new();

    public DateTimeOffset GetInstant(DateTime localTime) => new(localTime, TimeSpan.Zero);

    public DateTimeOffset GetClosingTime(DateTime localDate) => new(localDate.AddHours(16), TimeSpan.Zero); // Example: 4 PM close
}

public class ConfigurationException : Exception
{
    public ConfigurationException(string msg) : base(msg) { }
}