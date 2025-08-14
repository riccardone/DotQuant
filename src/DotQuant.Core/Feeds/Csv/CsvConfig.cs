using System.Text.RegularExpressions;
using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds.Csv;

public class CsvConfig
{
    private string FilePattern { get; set; } = @".*\\.csv";
    private List<string> FileSkip { get; set; } = new();
    public bool HasHeader { get; set; } = true;
    public char Separator { get; set; } = ',';

    private ITimeParser TimeParser { get; set; } = new AutoDetectTimeParser();
    private IPriceParser PriceParser { get; set; } = new PriceBarParser();
    public IAssetBuilder AssetBuilder { get; } = new StockBuilder();

    private Regex? _compiledPattern;
    private bool _isInitialized;

    public void Compile() => _compiledPattern = new Regex(FilePattern, RegexOptions.Compiled);

    public bool ShouldInclude(FileInfo file)
        => file.Exists && _compiledPattern?.IsMatch(file.Name) == true && !FileSkip.Contains(file.Name);

    public void InitParsers(string[] header)
    {
        if (_isInitialized) return;
        TimeParser.Init(header);
        PriceParser.Init(header);
        _isInitialized = true;
    }

    public PriceEntry ProcessLine(string[] line, IAsset asset)
    {
        var time = TimeParser.Parse(line);
        var price = PriceParser.Parse(line, asset);
        return new PriceEntry(time, price);
    }

    public static CsvConfig FromFile(string path)
    {
        var config = new CsvConfig();
        var configPath = Path.Combine(path, "config.properties");

        if (!File.Exists(configPath))
            return config;

        foreach (var line in File.ReadAllLines(configPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            if (key == "file.pattern") config.FilePattern = value;
            if (key == "file.skip") config.FileSkip = value.Split(',').Select(s => s.Trim()).ToList();
        }

        config.Compile();
        return config;
    }
}