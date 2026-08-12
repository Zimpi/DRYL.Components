using Microsoft.Agents.AI;

namespace DRYL.Components.Agents;

/// <summary>
/// Definition of one agent in a multi-agent flow (see
/// <see cref="DrylAgentRunner.StartSequential"/> / <see cref="DrylAgentRunner.StartConcurrent"/>).
/// The <see cref="Name"/> labels the step in <c>DrylHandoffTrace</c>.
/// </summary>
public sealed class DrylAgentStep
{
    /// <summary>Display name of the agent, shown per lane in <c>DrylHandoffTrace</c>.</summary>
    public required string Name { get; init; }

    /// <summary>The agent that runs this step.</summary>
    public AIAgent? Agent { get; init; }

    /// <summary>Optional session for the step's agent; omit for a one-shot run.</summary>
    public AgentSession? Session { get; init; }

    /// <summary>
    /// Test/replay seam: supplies the step's update stream (input message, cancellation) instead
    /// of a live agent. Takes precedence over <see cref="Agent"/> when set.
    /// </summary>
    internal Func<string, CancellationToken, IAsyncEnumerable<AgentResponseUpdate>>? UpdatesFactory { get; init; }
}
