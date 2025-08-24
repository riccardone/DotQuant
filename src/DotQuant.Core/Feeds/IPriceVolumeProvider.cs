using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds;

/// <summary>
/// Interface for providing volume fallback data for a given symbol and date.
/// </summary>
public interface IPriceVolumeProvider
{
    decimal? GetVolume(Symbol symbol, DateTime date);
}