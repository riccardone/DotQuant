using DotQuant.Core.Common;

namespace DotQuant.Core.Brokers;

/// <summary>
/// Base class for broker implementations, whether simulated or connected to a real API.
/// </summary>
public abstract class Broker
{
    /// <summary>
    /// Sync the broker state with a new event. Called on every event tick.
    /// </summary>
    /// <param name="evt">The current market event.</param>
    /// <returns>The updated account state.</returns>
    public abstract IAccount Sync(Event evt);

    /// <summary>
    /// Get the final account state at the end of a session.
    /// </summary>
    /// <returns>The final broker account snapshot.</returns>
    public abstract IAccount Sync();

    /// <summary>
    /// Place a list of orders based on strategy and trader decisions.
    /// </summary>
    /// <param name="orders">The orders to execute.</param>
    public abstract void PlaceOrders(List<Order> orders);
}