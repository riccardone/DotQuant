using System.Text.Json;

namespace DotQuant.Feeds.AlphaVantage.AlphaVantage;

public class IntradayTimeSeriesResponse
{
    public Dictionary<string, Dictionary<string, string>> TimeSeries { get; set; }
    public string Interval { get; set; } // e.g. "1min", "5min"

    public static IntradayTimeSeriesResponse? FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        var seriesProperty = root.EnumerateObject()
            .FirstOrDefault(p => p.Name.StartsWith("Time Series", StringComparison.OrdinalIgnoreCase));

        if (seriesProperty.Value.ValueKind != JsonValueKind.Object)
            return null;

        var timeSeries = new Dictionary<string, Dictionary<string, string>>();

        foreach (var entry in seriesProperty.Value.EnumerateObject())
        {
            var date = entry.Name;
            var fields = new Dictionary<string, string>();

            foreach (var field in entry.Value.EnumerateObject())
            {
                fields[field.Name] = field.Value.GetString() ?? string.Empty;
            }

            timeSeries[date] = fields;
        }

        return new IntradayTimeSeriesResponse
        {
            TimeSeries = timeSeries,
            Interval = seriesProperty.Name.Replace("Time Series (", "").Replace(")", "")
        };
    }
}