namespace DotQuant.Core.Common;

/// <summary>
/// Service for checking if a specific market is currently open.
/// </summary>
public interface IMarketStatusService
{
    /// <summary>
    /// Determines if the market associated with the given symbol is currently open.
    /// </summary>
    Task<bool> IsMarketOpenAsync(Symbol symbol, CancellationToken ct = default);
}