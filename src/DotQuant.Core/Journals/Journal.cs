using DotQuant.Core.Common;

namespace DotQuant.Core.Journals;

/// <summary>
/// For tracking progress during a run
/// </summary>
public abstract class Journal
{
    public abstract void Track(Event evt, IAccount account, List<Signal> signals, List<Order> orders);
}