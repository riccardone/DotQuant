namespace DotQuant.Api.Contracts.Models;

public class AppSettings
{
    public Schema Schema { get; set; } = new()
    {
        File = "schema.json",
        PathRoot = "Schemas",
        References = "refs"
    };

    public string MonitoringElasticSearchIndex { get; set; }

    public string ElasticSearchConnectionString { get; set; }
    public string ElasticSearchUsername { get; set; }
    public string ElasticSearchPassword { get; set; }
    public string BusConnectionString { get; set; } = "localhost";

    public OpenAi OpenAi { get; set; }
    public AlphaVantage AlphaVantage { get; set; }
}

public class OpenAi
{
    public string ApiKey { get; set; }
}

public class AlphaVantage
{
    public string ApiKey { get; set; }
}