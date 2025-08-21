namespace DotQuant.Core.Feeds;

public interface IPriceVolumeProvider
{
    decimal? GetVolume(string ticker, DateTime date);
}