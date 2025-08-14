using System.Threading.Channels;
using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds;

public class EventChannel : IDisposable, ICloneable
{
    private readonly Channel<Event> _channel;
    private readonly Timeframe _timeframe;
    private readonly int _capacity;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly BufferOverflow _overflowBehavior;
    private bool _closed;

    public bool IsClosed => _closed;

    public EventChannel(Timeframe? timeframe = null, int capacity = 10, BufferOverflow overflowBehavior = BufferOverflow.Suspend)
    {
        _timeframe = timeframe ?? Timeframe.Infinite;
        _capacity = capacity;
        _overflowBehavior = overflowBehavior;

        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = overflowBehavior switch
            {
                BufferOverflow.DropOldest => BoundedChannelFullMode.DropOldest,
                BufferOverflow.DropLatest => BoundedChannelFullMode.DropNewest,
                _ => BoundedChannelFullMode.Wait
            }
        };
        _channel = Channel.CreateBounded<Event>(options);
    }

    public ChannelReader<Event> Reader => _channel.Reader;
    public ChannelWriter<Event> Writer => _channel.Writer;

    public async Task SendAsync(Event evt, CancellationToken ct = default)
    {
        if (_closed) return;

        if (_timeframe.Contains(evt.Time))
        {
            await _channel.Writer.WriteAsync(evt, ct);
        }
        else if (evt.Time > _timeframe.End)
        {
            Close();
        }
    }

    public async Task SendIfNotEmptyAsync(Event evt, CancellationToken ct = default)
    {
        if (evt.IsNotEmpty())
            await SendAsync(evt, ct);
    }

    public async Task<Event> ReceiveAsync(int timeoutMillis = -1, CancellationToken ct = default)
    {
        if (timeoutMillis <= 0)
            return await _channel.Reader.ReadAsync(ct);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMillis);
        try
        {
            return await _channel.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            var now = DateTime.UtcNow;
            if (!_timeframe.Contains(now))
            {
                Close();
                throw new ChannelClosedException();
            }
            return Event.Empty(now);
        }
    }

    public void Close()
    {
        if (_closed) return;
        _closed = true;
        _channel.Writer.TryComplete();
        _mutex.Release();
    }

    public async Task WaitOnCloseAsync()
    {
        if (_closed) return;
        await _mutex.WaitAsync();
    }

    public object Clone()
    {
        return new EventChannel(_timeframe, _capacity, _overflowBehavior);
    }

    public void Dispose()
    {
        Close();
        _mutex.Dispose();
    }
}