namespace DRYL.Components.Agents;

/// <summary>How a multi-agent run executes its steps.</summary>
public enum DrylAgentFlow
{
    /// <summary>Steps run one after another; each agent receives the previous agent's answer (a handoff chain).</summary>
    Sequential,

    /// <summary>All steps run in parallel on the same input message.</summary>
    Concurrent,
}

/// <summary>
/// One agent's slot in a multi-agent run. <see cref="Run"/> is null until the step starts
/// (a pending lane in <c>DrylHandoffTrace</c>); afterwards the child run carries the step's
/// own text, tool calls, usage and error.
/// </summary>
public sealed class DrylAgentHandoff
{
    internal DrylAgentHandoff(string name, int index)
    {
        Name = name;
        Index = index;
    }

    /// <summary>Display name of the step's agent.</summary>
    public string Name { get; }

    /// <summary>Zero-based position of the step in the flow.</summary>
    public int Index { get; }

    /// <summary>The step's own observable run; null while the step is still pending.</summary>
    public DrylAgentRun? Run { get; internal set; }

    /// <summary>True once the step's agent has been started.</summary>
    public bool HasStarted => Run is not null;

    /// <summary>The step's AI state — <see cref="AiState.None"/> while pending.</summary>
    public AiState State => Run?.State ?? AiState.None;

    /// <summary>The step's terminal error, if it failed.</summary>
    public DrylRunError? Error => Run?.Error;
}

/// <summary>
/// Observable handle to a multi-agent flow (see <see cref="DrylAgentRunner.StartSequential"/> /
/// <see cref="DrylAgentRunner.StartConcurrent"/>). The shared run surface aggregates the flow:
/// <see cref="DrylRunBase.State"/> mirrors the active step, <see cref="DrylRunBase.Text"/> /
/// <see cref="DrylRunBase.TextStream"/> carry the final agent's answer (sequential flows only),
/// <see cref="DrylRunBase.Usage"/> sums the steps, and <see cref="DrylRunBase.Error"/> reports
/// the first failing step (with its name as <see cref="DrylRunError.Source"/>). Per-step detail
/// lives on <see cref="Steps"/> — render with <c>DrylHandoffTrace</c>.
/// </summary>
public sealed class DrylMultiAgentRun : DrylRunBase
{
    private readonly List<DrylAgentHandoff> _steps;

    internal DrylMultiAgentRun(DrylAgentFlow flow, IEnumerable<string> stepNames)
    {
        Flow = flow;
        _steps = stepNames.Select((name, index) => new DrylAgentHandoff(name, index)).ToList();
    }

    /// <summary>Whether the steps run sequentially (handoff chain) or concurrently.</summary>
    public DrylAgentFlow Flow { get; }

    /// <summary>The flow's steps in execution order, each with its own child run once started.</summary>
    public IReadOnlyList<DrylAgentHandoff> Steps => _steps;

    /// <summary>Index of the currently running step (sequential flows), or null.</summary>
    public int? ActiveIndex { get; internal set; }

    /// <summary>Cancels the flow and disposes every started child run.</summary>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();   // cancels DisposalToken first so children stop producing
        foreach (var step in _steps)
        {
            if (step.Run is not null) await step.Run.DisposeAsync();
        }
    }
}
