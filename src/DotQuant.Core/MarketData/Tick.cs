namespace DotQuant.Core.MarketData;

/// <summary>
/// A normalized market data tick for event-driven processing.
/// </summary>
public sealed record Tick(
    string Symbol,
    string Exchange,
    string Currency,
    DateTimeOffset Timestamp,
    decimal? BidPrice,
    decimal? AskPrice,
    decimal? LastPrice,
    decimal? BidSize,
    decimal? AskSize,
    decimal? LastSize
);