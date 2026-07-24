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
    // One op per beat so the user sees a choreography, not a jump (cadence like DrylAgentAttachments).
    private const int OpStaggerMs = 260;

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
            "Update the current artifact in place — patch props, insert, remove or move nodes. Put ALL " +
            "concrete data and numbers the update needs into the brief; the generator sees only this " +
            "brief plus the current artifact, nothing else from the conversation. Requires an artifact " +
            "already created via create_artifact.");
        All = new List<AITool> { CreateArtifact, UpdateArtifact };
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

    /// <summary>Update-artifact tool (<c>update_artifact</c>): runs a structured-streaming patch
    /// generation against the current <see cref="DrylCanvasRun.Spec"/> and applies its ops in place,
    /// staggered one per beat, as they become safe to apply (see <see cref="UpdateArtifactImpl"/>).</summary>
    public AITool UpdateArtifact { get; }

    /// <summary>The tool set to hand to the chat agent: <see cref="CreateArtifact"/> and <see cref="UpdateArtifact"/>.</summary>
    public IList<AITool> All { get; }

    private async Task<string> CreateArtifactImpl(
        [Description("What the artifact should show, incl. all concrete data/numbers it needs.")] string brief,
        [Description("Short artifact title.")] string? title = null,
        CancellationToken ct = default)
    {
        _run.BeginCreate();
        var reader = new PartialJsonReader<CanvasSpec>(CanvasJson.Options);
        try
        {
            CanvasSpec? last = null;
            await foreach (var delta in _generate(CanvasPrompt.CreatePrompt(brief, title), ct))
            {
                var snapshot = reader.Append(delta);
                if (snapshot is not null) _run.RevealSnapshot(last = snapshot);
            }
            CanvasSpec? final;
            string? recovery = null;
            try
            {
                final = JsonSerializer.Deserialize<CanvasSpec>(reader.Buffer, CanvasJson.Options);
            }
            catch (JsonException)
            {
                // Real models occasionally fumble the closing brackets. The tolerant reader kept
                // the last well-formed snapshot — complete from that instead of tearing down an
                // artifact the user has already watched stream in.
                final = last;
                recovery = " Note: the generated JSON ended malformed; the artifact was completed "
                         + "from the last valid snapshot.";
            }
            if (final?.Root is null)
                throw new InvalidOperationException("generator returned no artifact root");

            var problems = new List<string>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            int nodes = 0, interactive = 0;
            Walk(final.Root, n =>
            {
                nodes++;
                if (CanvasCatalog.IsInteractive(n.Type)) interactive++;
                if (!seenIds.Add(n.Id))
                    problems.Add($"duplicate id '{n.Id}' — ids must be unique across the artifact.");
                if (CanvasCatalog.Validate(n) is { } e) problems.Add(e);
            });
            _run.CompleteReveal(final);
            var receipt = $"Artifact created: {nodes} elements, {interactive} inputs." + recovery;
            return problems.Count == 0 ? receipt
                : receipt + " Some elements were invalid and are shown as placeholders — fix via update_artifact: "
                  + string.Join(" ", problems.Take(3));
        }
        catch (OperationCanceledException) { _run.CancelGeneration(); throw; }
        catch (Exception ex)
        {
            _run.FailGeneration(ex);
            return "Artifact generation failed: " + ex.Message + " You may retry with a simpler brief.";
        }
    }

    /// <summary>
    /// Runs a structured-streaming patch generation against the current spec and applies its ops
    /// to <see cref="DrylCanvasRun.Spec"/> as they stream in. An op may still be truncated mid-stream
    /// (its JSON not yet complete), so only ops strictly before the last parsed one are safe to apply
    /// while streaming; the remainder (including that last op) applies once the stream ends and the
    /// full op list is known.
    /// </summary>
    private async Task<string> UpdateArtifactImpl(
        [Description("What should change, incl. any new data needed.")] string brief,
        CancellationToken ct = default)
    {
        if (_run.Spec?.Root is null)
            return "There is no artifact yet — call create_artifact first.";
        _run.BeginGeneration();
        var reader = new PartialJsonReader<CanvasPatchDoc>(CanvasJson.Options);
        var applied = 0;
        var skipped = new List<string>();
        try
        {
            List<CanvasOp>? lastOps = null;
            var current = JsonSerializer.Serialize(_run.Spec, CanvasJson.Options);
            await foreach (var delta in _generate(CanvasPrompt.UpdatePrompt(brief, current), ct))
            {
                var ops = reader.Append(delta)?.Ops;
                if (ops is not null) lastOps = ops;
                while (ops is not null && applied < ops.Count - 1)   // last op may still be truncated
                    await ApplyStaggeredAsync(ops[applied++], skipped, ct);
            }
            List<CanvasOp> final;
            string? recovery = null;
            try
            {
                final = JsonSerializer.Deserialize<CanvasPatchDoc>(reader.Buffer, CanvasJson.Options)?.Ops
                        ?? new List<CanvasOp>();
            }
            catch (JsonException)
            {
                // Same tolerance as the create path: a malformed stream tail falls back to the last
                // op list the tolerant reader parsed. A trailing half-parsed op is safe to attempt —
                // ApplyOp validates and skips anything incoherent.
                final = lastOps ?? new List<CanvasOp>();
                recovery = " Note: the generated JSON ended malformed; trailing ops may have been dropped.";
            }
            while (applied < final.Count)
                await ApplyStaggeredAsync(final[applied++], skipped, ct);
            _run.CompleteGeneration();
            var receipt = $"Artifact updated: {applied - skipped.Count} changes applied." + recovery;
            return skipped.Count == 0 ? receipt
                : receipt + $" {skipped.Count} ops skipped: " + string.Join(" ", skipped.Take(3));
        }
        catch (OperationCanceledException) { _run.CancelGeneration(); throw; }
        catch (Exception ex)
        {
            _run.FailGeneration(ex);
            return "Artifact update failed: " + ex.Message;
        }
    }

    /// <summary>Applies one op via <see cref="DrylCanvasRun.ApplyOp"/>; on success, waits
    /// <see cref="OpStaggerMs"/> so the user sees each change land as its own beat. A skipped op
    /// (unknown id, invalid props, …) is recorded but does not pause the choreography.</summary>
    private async Task ApplyStaggeredAsync(CanvasOp op, List<string> skipped, CancellationToken ct)
    {
        if (_run.ApplyOp(op) is { } reason) skipped.Add(reason);
        else await Task.Delay(OpStaggerMs, ct);
    }

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
