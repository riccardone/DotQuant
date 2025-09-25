namespace DotQuant.Ai.Contracts;

public interface IPriceVolumeProvider
{
    decimal? GetVolume(string ticker, DateTime date);
}