using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Feeds.Csv;
using Microsoft.Extensions.Logging;

namespace DotQuant.FeedFromCsv;

/// <summary>
/// Reads historic price data from CSV files and loads into memory.
/// </summary>
public class CsvFeed : HistoricPriceFeed
{
    private readonly ILogger<CsvFeed> _logger;
    private readonly CsvConfig _config;

    public CsvFeed(ILogger<CsvFeed> logger, string pathStr, CsvConfig? config = null, Action<CsvConfig>? configure = null)
    {
        _logger = logger;
        if (!Directory.Exists(pathStr) && !File.Exists(pathStr))
            throw new ArgumentException($"{pathStr} does not exist");

        _config = config ?? CsvConfig.FromFile(pathStr);
        configure?.Invoke(_config);

        // Read files synchronously
        ReadFiles(pathStr).GetAwaiter().GetResult();

        _logger.LogInformation($"events={Timeline.Count} assets={Assets.Count} timeframe={Timeframe}");
    }

    public CsvFeed(ILogger<CsvFeed> logger, string path, Action<CsvConfig> configure)
        : this(logger, path, null, configure)
    {
    }

    private List<FileInfo> ReadPath(string pathStr)
    {
        var files = new List<FileInfo>();
        var fileAttr = File.GetAttributes(pathStr);

        if (fileAttr.HasFlag(FileAttributes.Directory))
        {
            var dir = new DirectoryInfo(pathStr);
            files.AddRange(dir.EnumerateFiles("*.*", SearchOption.AllDirectories)
                .Where(_config.ShouldInclude)
                .ToList());
        }
        else if (fileAttr.HasFlag(FileAttributes.Archive))
        {
            files.Add(new FileInfo(pathStr));
        }

        return files;
    }

    private async Task ReadFiles(string pathStr)
    {
        var files = ReadPath(pathStr);
        if (files.Count == 0)
        {
            _logger.LogInformation($"Found no CSV files at {pathStr}");
            return;
        }
        _logger.LogInformation($"Found {files.Count} CSV files");

        var tasks = files.Select(file => Task.Run(() =>
        {
            var asset = _config.AssetBuilder.Build(Path.GetFileNameWithoutExtension(file.Name), Currency.EUR);
            var entries = ReadFile(asset, file);
            foreach (var entry in entries)
            {
                Add(entry.Time, entry.Price);
            }
        }));

        await Task.WhenAll(tasks);
    }

    private List<PriceEntry> ReadFile(IAsset asset, FileInfo file)
    {
        var entries = new List<PriceEntry>();
        var errors = 0;
        var hasHeader = _config.HasHeader;

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeader,
            IgnoreBlankLines = true,
            Delimiter = _config.Separator.ToString()
        };

        using var reader = new StreamReader(file.FullName);
        using var csv = new CsvReader(reader, config);

        try
        {
            var isFirst = hasHeader;
            while (csv.Read())
            {
                var row = csv.Parser.Record;
                if (isFirst)
                {
                    _config.InitParsers(row);
                    isFirst = false;
                    continue;
                }

                try
                {
                    var entry = _config.ProcessLine(row, asset);
                    entries.Add(entry);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"{ex} -- {asset.Symbol} line skipped");
                    errors++;
                }
            }
            if (errors > 0)
                _logger.LogError($"Skipped {errors} lines due to errors in {file.FullName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading {file.FullName}: {ex}");
        }

        return entries;
    }
}