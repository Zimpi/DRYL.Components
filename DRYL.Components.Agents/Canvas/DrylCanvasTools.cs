using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DRYL.Components.Agents.Generation;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

/// <summary>Chat-agent tools that build and iterate a canvas artifact via a dedicated
/// structured-streaming sub-generation. Hand <see cref="All"/> to the chat agent.</summary>
public sealed class DrylCanvasTools
{
    private readonly DrylCanvasRun _run;
    private readonly Func<string, CancellationToken, IAsyncEnumerable<string>> _generate;

    private DrylCanvasTools(
        DrylCanvasRun run, Func<string, CancellationToken, IAsyncEnumerable<string>> generate)
    {
        _run = run;
        _generate = generate;

        CreateArtifact = AIFunctionFactory.Create(CreateArtifactImpl, "create_artifact",
            "Create a live visual artifact next to the chat — a card, chart, table or form the user " +
            "sees rendered live. Put ALL concrete data and numbers the artifact needs into the brief; " +
            "the generator sees only this brief, nothing else from the conversation. Use this once per " +
            "distinct artifact.");
        UpdateArtifact = AIFunctionFactory.Create(UpdateArtifactImpl, "update_artifact",
            "Update the current artifact in place. Not available yet.");
        All = new List<AITool> { CreateArtifact };
    }

    /// <summary>Create the tools; <paramref name="generator"/> runs the artifact generations
    /// (a fresh session per call — generations are stateless, the current spec travels in the prompt).</summary>
    public static DrylCanvasTools Create(DrylCanvasRun run, AIAgent generator) =>
        new(run, LiveGenerate(generator));

    /// <summary>Replay/demo/test seam: like <see cref="Create"/>, but generations come from
    /// <paramref name="generate"/> (prompt → raw JSON delta stream) instead of a live agent.</summary>
    public static DrylCanvasTools CreateReplay(
        DrylCanvasRun run, Func<string, CancellationToken, IAsyncEnumerable<string>> generate) =>
        new(run, generate);

    /// <summary>Create-artifact tool (<c>create_artifact</c>): runs a fresh structured-streaming
    /// generation and progressively fills <see cref="DrylCanvasRun.Spec"/> as it streams.</summary>
    public AITool CreateArtifact { get; }

    /// <summary>Update-artifact tool (<c>update_artifact</c>): patches the current artifact in place.
    /// Registered as a placeholder — its implementation ships in Task 6 and currently throws
    /// <see cref="NotImplementedException"/>. Not yet included in <see cref="All"/>.</summary>
    public AITool UpdateArtifact { get; }

    /// <summary>The tool set to hand to the chat agent. Only <see cref="CreateArtifact"/> until
    /// <see cref="UpdateArtifact"/> lands in Task 6.</summary>
    public IList<AITool> All { get; }

    private async Task<string> CreateArtifactImpl(
        [Description("What the artifact should show, incl. all concrete data/numbers it needs.")] string brief,
        [Description("Short artifact title.")] string? title = null,
        CancellationToken ct = default)
    {
        _run.BeginGeneration();
        var reader = new PartialJsonReader<CanvasSpec>(CanvasJson.Options);
        try
        {
            await foreach (var delta in _generate(CanvasPrompt.CreatePrompt(brief, title), ct))
            {
                var snapshot = reader.Append(delta);
                if (snapshot is not null) _run.ApplySnapshot(snapshot);
            }
            var final = JsonSerializer.Deserialize<CanvasSpec>(reader.Buffer, CanvasJson.Options);
            if (final?.Root is null)
                throw new InvalidOperationException("generator returned no artifact root");

            var problems = new List<string>();
            int nodes = 0, interactive = 0;
            Walk(final.Root, n =>
            {
                nodes++;
                if (CanvasCatalog.IsInteractive(n.Type)) interactive++;
                if (CanvasCatalog.Validate(n) is { } e) problems.Add(e);
            });
            _run.CompleteGeneration(final);
            var receipt = $"Artifact created: {nodes} elements, {interactive} inputs.";
            return problems.Count == 0 ? receipt
                : receipt + " Some elements were invalid and are shown as placeholders — fix via update_artifact: "
                  + string.Join(" ", problems.Take(3));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _run.FailGeneration(ex);
            return "Artifact generation failed: " + ex.Message + " You may retry with a simpler brief.";
        }
    }

    private static string UpdateArtifactImpl(
        [Description("What to change on the current artifact, and why.")] string instructions) =>
        throw new NotImplementedException("update_artifact ships in Task 6.");

    private static void Walk(CanvasNode n, Action<CanvasNode> visit)
    {
        visit(n);
        if (n.Children is null) return;
        foreach (var child in n.Children) Walk(child, visit);
    }

    private static Func<string, CancellationToken, IAsyncEnumerable<string>> LiveGenerate(AIAgent generator) =>
        (prompt, ct) => LiveGenerateStream(generator, prompt, ct);

    private static async IAsyncEnumerable<string> LiveGenerateStream(
        AIAgent generator, string prompt, [EnumeratorCancellation] CancellationToken ct)
    {
        var session = await generator.CreateSessionAsync(ct).ConfigureAwait(false);
        var options = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions { ResponseFormat = ChatResponseFormat.Json },
        };
        await foreach (var delta in DrylAgentRunner.ExtractJsonDeltas(
            generator.RunStreamingAsync(prompt, session, options, ct), ct).ConfigureAwait(false))
        {
            yield return delta;
        }
    }
}
