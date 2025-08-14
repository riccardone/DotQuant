namespace DotQuant.Core.Common;

/// <summary>
/// Describes the strategic intent behind a trading signal.
/// Used to classify how a signal should be interpreted or acted upon.
/// </summary>
public enum TradeIntent
{
    /// <summary>
    /// A new position is being opened (first entry).
    /// </summary>
    Entry,

    /// <summary>
    /// An existing position is being increased (adding to position).
    /// </summary>
    ScaleIn,

    /// <summary>
    /// The entire position is being closed (full exit).
    /// </summary>
    ExitFull,

    /// <summary>
    /// A partial exit is being executed (layered or recycled exit).
    /// </summary>
    ExitPartial,

    /// <summary>
    /// A new entry following a prior recycled exit, typically after cooldown.
    /// </summary>
    Reentry,

    /// <summary>
    /// The signal was ignored or not actionable (e.g., no buying power, duplicate).
    /// </summary>
    Ignored
}