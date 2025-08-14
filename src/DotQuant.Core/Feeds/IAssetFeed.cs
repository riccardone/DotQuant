using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds;

public interface IAssetFeed : IFeed
{
    IReadOnlyCollection<IAsset> Assets { get; }
}