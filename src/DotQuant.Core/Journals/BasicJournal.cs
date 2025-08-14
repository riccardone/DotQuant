using DotQuant.Core.Common;
using Microsoft.Extensions.Logging;

namespace DotQuant.Core.Journals;

/// <summary>
/// Tracks basic progress metrics and logs them using Microsoft.Extensions.Logging.
/// This journal has low overhead and provides basic insights into the trading process.
/// </summary>
public class BasicJournal(ILogger<BasicJournal> logger, bool logProgress = false) : Journal
{
    private readonly ILogger<BasicJournal> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private long _nItems;
    private long _nOrders;
    private long _nSignals;
    private long _nEvents;
    private int _maxPositions;
    private DateTimeOffset? _lastTime;

    public override void Track(Event evt, IAccount account, List<Signal> signals, List<Order> orders)
    {
        _nItems += evt.Items.Count;
        _nOrders += orders.Count;
        _nSignals += signals.Count;
        _nEvents += 1;
        _lastTime = evt.Time;
        _maxPositions = Math.Max(_maxPositions, account.Positions.Count);

        if (logProgress)
            _logger.LogInformation(ToString());
    }

    public override string ToString()
    {
        return $"time={_lastTime} items={_nItems} signals={_nSignals} orders={_nOrders} max-positions={_maxPositions}";
    }
}