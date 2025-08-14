using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.FeedFromCsv;

public sealed class CsvFeedFactory : IFeedFactory
{
    public string Key => "csv";
    public string Name => "CSV Historic Feed";

    public IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args)
    {
        var csvLogger = sp.GetRequiredService<ILogger<CsvFeed>>();

        var path = FirstNonEmpty(
            args.TryGetValue("--file", out var cliPath) ? cliPath : null,
            config["Csv:Path"],
            DiscoverDefaultCsvPath(logger) // <-- automatic fallback
        ) ?? throw new ArgumentException(
            "CSV path not provided and no default file could be discovered. Use --file <path> or set Csv:Path."
        );

        return new CsvFeed(csvLogger, path, cfg =>
        {
            var sep = args.TryGetValue("--csv:sep", out var sSep) ? sSep : config["Csv:Separator"];
            if (!string.IsNullOrWhiteSpace(sep) && sep!.Length == 1)
                cfg.Separator = sep[0];

            var hasHeaderSetting = args.TryGetValue("--csv:hasHeader", out var sHdr) ? sHdr : config["Csv:HasHeader"];
            if (bool.TryParse(hasHeaderSetting, out var hasHeader))
                cfg.HasHeader = hasHeader;
        });
    }

    private static string? DiscoverDefaultCsvPath(ILogger logger)
    {
        const string dataFolder = "data";

        if (!Directory.Exists(dataFolder))
        {
            logger.LogWarning("CSV default discovery: '{Folder}' not found.", dataFolder);
            return null;
        }

        var firstCsv = Directory.GetFiles(dataFolder, "*.csv", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (firstCsv == null)
        {
            logger.LogWarning("CSV default discovery: no CSV files found in '{Folder}'.", dataFolder);
            return null;
        }

        logger.LogInformation("CSV default discovery selected: {Csv}", firstCsv);
        return firstCsv;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}