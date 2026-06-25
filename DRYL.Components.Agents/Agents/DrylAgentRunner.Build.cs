using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

public sealed partial class DrylAgentRunner
{
    private const string DefaultBuildGuidance =
        "Build the result collaboratively and iteratively. Do not gather everything up front and " +
        "then dump a result. Instead: ask the user one focused question via your question tools, " +
        "record progress with the update tool, then ask the next — alternating question -> refine " +
        "-> question. Call the update tool many times as the picture sharpens. When the user is " +
        "satisfied and the artifact is complete, give a brief final confirmation and stop.";

    /// <summary>
    /// Start an iterative, collaborative artifact build. The model alternates asking the user (via
    /// the agent's own tools), thinking, and refining a <typeparamref name="T"/> through an
    /// auto-injected <c>update_&lt;T&gt;</c> tool, until it produces a final answer. Returns an
    /// observable <see cref="DrylArtifactRun{T}"/> whose <see cref="DrylArtifactRun{T}.Artifact"/>
    /// grows round by round.
    /// </summary>
    public DrylArtifactRun<T> StartBuild<T>(
        AIAgent agent, AgentSession session, string prompt,
        DrylBuildOptions? options = null, string? aiKey = null, CancellationToken ct = default)
    {
        options ??= new DrylBuildOptions();
        var run = new DrylArtifactRun<T>(JsonOpts);

        var runOptions = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = options.Guidance ?? DefaultBuildGuidance,
                Tools = new List<AITool> { CreateUpdateTool(run, options) },
            }
        };

        var updates = agent.RunStreamingAsync(prompt, session, runOptions, ct);
        _ = ProcessAsync(run, updates, aiKey, ct);
        return run;
    }

    /// <summary>
    /// Builds the auto-generated <c>update_&lt;T&gt;</c> tool: it accepts a partial-<typeparamref name="T"/>
    /// JSON patch, merges it into <paramref name="run"/>, and returns a receipt for the model.
    /// </summary>
    internal static AITool CreateUpdateTool<T>(DrylArtifactRun<T> run, DrylBuildOptions options)
    {
        var toolName = options.UpdateToolName ?? $"update_{typeof(T).Name.ToLowerInvariant()}";
        var schema = AIJsonUtilities.CreateJsonSchema(typeof(T), serializerOptions: JsonOpts).GetRawText();
        var description =
            "Record or refine the artifact as you learn more. Call this repeatedly — include only " +
            "the fields you want to set or change; all fields are optional. Artifact shape: " + schema;

        return AIFunctionFactory.Create(
            (JsonElement patch) => run.ApplyPatch(patch, options.MaxRounds),
            toolName, description);
    }
}
