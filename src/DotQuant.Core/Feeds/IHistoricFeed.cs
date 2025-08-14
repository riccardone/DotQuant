using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds;

/// <summary>
/// Historic feed represents a feed of historic data, useful for backtesting.
/// Defines access to Timeline, Timeframe, and Assets.
/// </summary>
public interface IHistoricFeed : IAssetFeed
{
    /// <summary>
    /// Timeline of this feed.
    /// </summary>
    Timeline Timeline { get; }

    /// <summary>
    /// Timeframe of this feed, defaults to Timeline.Timeframe.
    /// </summary>
    Timeframe Timeframe => Timeline.Timeframe;

    /// <summary>
    /// Draw random sampled timeframe from timeline.
    /// </summary>
    List<Timeframe> Sample(int size, int samples = 1, Random? random = null)
        => Timeline.Sample(size, samples, random ?? new Random());

    /// <summary>
    /// Split timeframe into multiple periods.
    /// </summary>
    List<Timeframe> Split(TimeSpan period, TimeSpan? overlap = null)
        => Timeframe.Split(period, overlap);

    /// <summary>
    /// Split timeline into multiple timeframes with fixed size.
    /// </summary>
    List<Timeframe> Split(int size)
        => Timeline.Split(size);
}