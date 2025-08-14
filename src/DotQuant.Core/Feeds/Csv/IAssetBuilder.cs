using System.Text.RegularExpressions;
using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds.Csv;

/// <summary>
/// Interface for building assets from a name (e.g., file name).
/// </summary>
public interface IAssetBuilder
{
    IAsset Build(string name, Currency currency);
}

/// <summary>
/// Default stock asset builder using file name without extension as symbol name.
/// </summary>
public sealed class StockBuilder : IAssetBuilder
{
    private static readonly Regex NotCapitalRegex = new Regex("[^A-Z]", RegexOptions.Compiled);

    public IAsset Build(string name, Currency currency)
    {
        var symbol = Path.GetFileNameWithoutExtension(name).ToUpperInvariant();
        symbol = NotCapitalRegex.Replace(symbol, ".");
        return new Stock(symbol, currency);
    }
}