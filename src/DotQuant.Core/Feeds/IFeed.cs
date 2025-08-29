using System.Threading.Channels;
using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds;

/// <summary>
/// A source of market events that can be replayed into a channel for simulation or real-time processing.
/// </summary>
public interface IFeed : IAsyncDisposable
{
    /// <summary>
    /// Gets the timeframe this feed covers. Defaults to <see cref="Timeframe.Infinite"/>.
    /// </summary>
    Timeframe Timeframe => Timeframe.Infinite;

    /// <summary>
    /// Replay events into the specified channel.
    /// </summary>
    /// <param name="channel">The channel to write events into.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task PlayAsync(ChannelWriter<Event> channel, CancellationToken ct = default);

    /// <summary>
    /// Replay events into an EventChannel's writer (convenience overload).
    /// </summary>
    Task PlayAsync(EventChannel eventChannel, CancellationToken ct = default)
        => PlayAsync(eventChannel.Writer, ct);

    /// <summary>
    /// Starts event replay in the background using the given channel.
    /// Automatically completes the channel when done.
    /// </summary>
    Task PlayBackgroundAsync(ChannelWriter<Event> channel, CancellationToken ct = default)
        => Task.Run(async () =>
        {
            await PlayAsync(channel, ct);
            channel.Complete();
        }, ct);

    /// <summary>
    /// Starts event replay in the background using an EventChannel.
    /// </summary>
    Task PlayBackgroundAsync(EventChannel eventChannel, CancellationToken ct = default)
        => PlayBackgroundAsync(eventChannel.Writer, ct);

    /// <summary>
    /// Asynchronously dispose of feed resources (default is no-op).
    /// </summary>
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await Task.CompletedTask;
    }
}