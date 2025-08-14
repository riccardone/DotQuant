namespace DotQuant.Core.Common;

/// <summary>
/// Represents the type of signal: Entry, Exit, or Both.
/// Can be used by advanced strategies to differentiate entry/exit signals.
/// </summary>
public enum SignalType
{
    Entry,
    Exit,
    Both
}

/// <summary>
/// Signal provides a rating for an asset and is typically created by a strategy.
/// How these signals are translated into actual orders depends on the signal converter logic.
/// </summary>
public sealed class Signal
{
    public IAsset Asset { get; }
    public decimal Rating { get; }
    public SignalType Type { get; }

    /// <summary>
    /// Describes the strategic purpose of the signal for trading logic (e.g., Entry, ScaleIn, ExitFull).
    /// Optional but recommended for audit and classification.
    /// </summary>
    public TradeIntent? Intent { get; init; }

    public Signal(IAsset asset, decimal rating, SignalType type = SignalType.Both, TradeIntent? intent = null)
    {
        Asset = asset;
        Rating = rating;
        Type = type;
        Intent = intent;
    }

    /// <summary>
    /// Factory method to create a BUY signal with default rating 1.0.
    /// </summary>
    public static Signal Buy(IAsset asset, SignalType type = SignalType.Both, TradeIntent? intent = null)
        => new(asset, 1.0m, type, intent);

    /// <summary>
    /// Factory method to create a SELL signal with default rating -1.0.
    /// </summary>
    public static Signal Sell(IAsset asset, SignalType type = SignalType.Both, TradeIntent? intent = null)
        => new(asset, -1.0m, type, intent);

    /// <summary>
    /// True if this signal can be used to exit or decrease a position.
    /// </summary>
    public bool Exit => Type == SignalType.Exit || Type == SignalType.Both;

    /// <summary>
    /// True if this signal can be used to enter or increase a position.
    /// </summary>
    public bool Entry => Type == SignalType.Entry || Type == SignalType.Both;

    /// <summary>
    /// Returns true if this signal conflicts with another signal for the same asset but with opposite direction.
    /// </summary>
    public bool Conflicts(Signal other)
        => Asset.Equals(other.Asset) && Direction != other.Direction;

    /// <summary>
    /// Direction: -1 for sell, 1 for buy, 0 for hold/neutral.
    /// </summary>
    public int Direction
    {
        get
        {
            if (IsBuy) return 1;
            if (IsSell) return -1;
            return 0;
        }
    }

    /// <summary>
    /// True if this is a positive (buy) signal.
    /// </summary>
    public bool IsBuy => Rating > 0.0m;

    /// <summary>
    /// True if this is a negative (sell) signal.
    /// </summary>
    public bool IsSell => Rating < 0.0m;
}