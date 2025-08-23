using DotQuant.Core.Common;

namespace DotQuant.Core.Brokers;

/// <summary>
/// Interface for broker implementations, whether simulated or connected to a real API.
/// </summary>
public interface IBroker
{
    /// <summary>
    /// Sync the broker state with a new event. Called on every event tick.
    /// </summary>
    /// <param name="evt">The current market event.</param>
    /// <returns>The updated account state.</returns>
    public IAccount Sync(Event evt);

    /// <summary>
    /// Get the final account state at the end of a session.
    /// </summary>
    /// <returns>The final broker account snapshot.</returns>
    public IAccount Sync();

    /// <summary>
    /// Place a list of orders based on strategy and trader decisions.
    /// </summary>
    /// <param name="orders">The orders to execute.</param>
    public void PlaceOrders(List<Order> orders);
}