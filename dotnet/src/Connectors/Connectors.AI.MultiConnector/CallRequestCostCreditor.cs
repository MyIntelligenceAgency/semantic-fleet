// Copyright (c) MyIA. All rights reserved.

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

using System.Threading;

/// <summary>
/// Manages the ongoing cost of call requests in a thread-safe manner.
/// </summary>
public class CallRequestCostCreditor
{
    /// <summary>
    /// Stores the ongoing cost in ticks for atomic operations.
    /// </summary>
    private long _ongoingCostInTicks;

    /// <summary>
    /// Gets the ongoing cost of call requests.
    /// </summary>
    public decimal OngoingCost
    {
        get => this.DecimalFromTicks(Interlocked.Read(ref this._ongoingCostInTicks)); // Read the value atomically
    }

    /// <summary>
    /// Resets the ongoing cost to zero.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref this._ongoingCostInTicks, 0);
    }

    /// <summary>
    /// Credits the specified cost to the ongoing cost.
    /// </summary>
    /// <param name="cost">The cost to be credited.</param>
    public void Credit(decimal cost)
    {
        long tickChange = this.TicksFromDecimal(cost);
        Interlocked.Add(ref this._ongoingCostInTicks, tickChange);
    }

    /// <summary>
    /// Converts a decimal value to its equivalent long value in ticks.
    /// </summary>
    /// <param name="value">The decimal value to convert.</param>
    /// <returns>The equivalent long value in ticks.</returns>
    private long TicksFromDecimal(decimal value)
    {
        return (long)(value * 1_000_000_000); // Assuming 9 decimal places of precision
    }

    /// <summary>
    /// Converts a long value in ticks to its equivalent decimal value.
    /// </summary>
    /// <param name="ticks">The long value in ticks to convert.</param>
    /// <returns>The equivalent decimal value.</returns>
    private decimal DecimalFromTicks(long ticks)
    {
        return ticks / 1_000_000_000m; // Convert back to decimal
    }
}
