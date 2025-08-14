using DotQuant.Core.Common;

namespace DotQuant.Core.Strategies;

/// <summary>
/// The Strategy is the interface that any trading strategy will need to implement. A strategy receives an
/// [Event] and can generate zero or more Signals.
/// No assumptions on the type of strategy. It can range from a technical indicator all the way
/// to sentiment analysis using machine learning.
/// </summary>
public interface IStrategy
{
    List<Signal> CreateSignals(Event evt);
}