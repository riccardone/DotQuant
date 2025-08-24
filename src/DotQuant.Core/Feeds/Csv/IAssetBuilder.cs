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
    private static readonly Regex SymbolPartsRegex = new Regex(@"[^A-Z0-9.]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IAsset Build(string name, Currency currency)
    {
        var baseName = Path.GetFileNameWithoutExtension(name).ToUpperInvariant();
        baseName = SymbolPartsRegex.Replace(baseName, "");

        string ticker, exchange;

        if (baseName.Contains('.'))
        {
            var parts = baseName.Split('.', 2);
            ticker = parts[0];
            exchange = parts[1];
        }
        else
        {
            ticker = baseName;
            exchange = "UNKNOWN"; // Fallback if not provided
        }

        var symbol = new Symbol(ticker, exchange);
        return new Stock(symbol, currency);
    }
}