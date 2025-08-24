using DotQuant.Core.Common;
using DotQuant.Core.Feeds.Model;

namespace DotQuant.Core.Feeds;

/// <summary>
/// Interface for reading price data (historical and live) for a given symbol.
/// </summary>
public interface IDataReader
{
    bool TryGetPrices(Symbol symbol, DateTime startDate, DateTime endDate, out IEnumerable<Price>? prices);
    bool TryGetLatestPrice(Symbol symbol, out Price? price);
}