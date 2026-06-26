using DRYL.Components.Ai;

namespace DRYL.Components.Agents;

/// <summary>
/// Scoped service that starts Microsoft Agent Framework runs and bridges them to
/// DRYL's AI vocabulary. Registered via <c>AddDrylAgents()</c>.
/// </summary>
public sealed partial class DrylAgentRunner
{
    private readonly IDrylAiActivityService? _activity;

    /// <summary>Creates the runner. The optional AI-activity service drives a surrounding <c>DrylAiScope</c>.</summary>
    public DrylAgentRunner(IDrylAiActivityService? activity = null) => _activity = activity;
}
