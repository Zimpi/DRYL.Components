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
    /// <remarks>
    /// <b>Key / aiKey coupling:</b> pass the same string to <paramref name="aiKey"/> here and to
    /// <c>DrylAiBuild.Key</c> when rendering this run. The runner owns the activity service mid-run
    /// and drives the scope's live glow; <c>DrylAiBuild</c> only touches it at settle. Without a
    /// matching key the surrounding <c>DrylAiScope</c> will not glow during the build.
    /// </remarks>
    public DrylArtifactRun<T> StartBuild<T>(
        AIAgent agent, AgentSession session, string prompt,
        DrylBuildOptions? options = null, string? aiKey = null, CancellationToken ct = default)
    {
        options ??= new DrylBuildOptions();
        var run = new DrylArtifactRun<T>(JsonOpts);

        // Stop an in-flight reveal when the caller cancels OR the run is disposed.
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, run.DisposalToken);

        var runOptions = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = options.Guidance ?? DefaultBuildGuidance,
                Tools = new List<AITool> { CreateUpdateTool(run, options, linkedCts.Token) },
            }
        };

        var updates = agent.RunStreamingAsync(prompt, session, runOptions, linkedCts.Token);
        _ = ProcessAsync(run, updates, aiKey, linkedCts.Token)
            .ContinueWith(_ => linkedCts.Dispose(), TaskScheduler.Default);
        return run;
    }

    /// <summary>
    /// Builds the auto-generated <c>update_&lt;T&gt;</c> tool: it accepts a partial-<typeparamref name="T"/>
    /// JSON patch, reveals it into <paramref name="run"/> over <see cref="DrylBuildOptions.RevealDuration"/>,
    /// and returns a receipt for the model. The delegate is async, so the framework's function-invocation
    /// loop awaits the reveal and the model's next turn paces behind it.
    /// </summary>
    internal static AITool CreateUpdateTool<T>(
        DrylArtifactRun<T> run, DrylBuildOptions options, CancellationToken ct = default)
    {
        var typeName = typeof(T).Name;
        var backtick = typeName.IndexOf('`');
        if (backtick >= 0) typeName = typeName[..backtick];
        var toolName = options.UpdateToolName ?? $"update_{typeName.ToLowerInvariant()}";
        var schema = AIJsonUtilities.CreateJsonSchema(typeof(T), serializerOptions: JsonOpts).GetRawText();
        var description =
            "Record or refine the artifact as you learn more. Call this repeatedly — include only " +
            "the fields you want to set or change; all fields are optional. Artifact shape: " + schema;

        return AIFunctionFactory.Create(
            (JsonElement patch) => run.ApplyPatchAsync(patch, options.MaxRounds, options.RevealDuration, ct),
            toolName, description);
    }
}
