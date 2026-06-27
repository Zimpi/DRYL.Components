using System.Text.Json;
using System.Text.Json.Nodes;
using DRYL.Components.Agents.Generation;

namespace DRYL.Components.Agents;

/// <summary>
/// Observable handle to an iterative artifact build (see <see cref="DrylAgentRunner.StartBuild{T}"/>).
/// Adds the live, progressively-merged <see cref="Artifact"/> and the <see cref="Round"/> counter to
/// the shared run surface.
/// </summary>
public sealed class DrylArtifactRun<T> : DrylRunBase
{
    private readonly JsonSerializerOptions _deserializeOptions;
    private JsonNode? _json;

    internal DrylArtifactRun(JsonSerializerOptions jsonOptions)
    {
        _deserializeOptions = new JsonSerializerOptions(jsonOptions) { PropertyNameCaseInsensitive = true };
    }

    /// <summary>The live, progressively-merged artifact (fields not yet provided are null/default).</summary>
    public T? Artifact { get; private set; }

    /// <summary>The number of <c>update_&lt;T&gt;</c> merge steps applied so far.</summary>
    public int Round { get; private set; }

    /// <summary>
    /// Merge a partial-<typeparamref name="T"/> patch into the running artifact and return a short
    /// receipt for the model. When <paramref name="revealDuration"/> is positive, the patch's
    /// new/changed fields materialize progressively (a guided, type-as-you-go reveal) over that
    /// span while previously-committed fields stay stable; otherwise the merge is atomic. The
    /// commit always uses the exact, full patch, so committed state can never be a repaired prefix.
    /// When <paramref name="maxRounds"/> is reached, returns a finalize nudge instead of the receipt.
    /// </summary>
    internal async Task<string> ApplyPatchAsync(
        JsonElement patch, int? maxRounds, TimeSpan revealDuration, CancellationToken ct)
    {
        var committedBase = _json;   // stable base this round overlays; earlier fields never re-animate
        var patchRaw = patch.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : patch.GetRawText();

        if (patchRaw is not null && revealDuration > TimeSpan.Zero)
        {
            try
            {
                await RevealAsync(committedBase, patchRaw, revealDuration, ct);
            }
            catch (OperationCanceledException)
            {
                // Cancelled mid-reveal — fall through and commit the final merged state so no
                // half-revealed artifact is left behind.
            }
        }

        // Commit the exact, full patch (never a repaired prefix): committed state is always the true merge.
        var fullPatch = patchRaw is null ? null : JsonNode.Parse(patchRaw);
        _json = JsonMerge.Merge(committedBase, fullPatch);
        Round++;
        Artifact = _json is null ? default : _json.Deserialize<T>(_deserializeOptions);
        Raise();

        return maxRounds is { } m && Round >= m
            ? "Maximum refinement rounds reached — stop refining and give your final answer now."
            : $"Updated (round {Round}).";
    }

    // Replays patchRaw as a growing prefix in ~N steps over revealDuration. Each snapshot is
    // repaired into valid JSON (JsonPartialRepair) and overlaid onto the stable committed base, so
    // a mid-token snapshot holds the last good artifact rather than flashing to default.
    private async Task RevealAsync(JsonNode? committedBase, string patchRaw, TimeSpan revealDuration, CancellationToken ct)
    {
        const int minSteps = 4;
        const int maxSteps = 40;
        var steps = Math.Clamp(patchRaw.Length, minSteps, maxSteps);
        var stepDelay = revealDuration / steps;

        State = AiState.Streaming;

        var prev = 0;
        for (var step = 1; step <= steps; step++)
        {
            var target = (int)((long)patchRaw.Length * step / steps);
            target = ExtendToWordBoundary(patchRaw, target);
            if (target <= prev && step < steps) continue;   // skip zero-width steps; always emit the last
            prev = target;

            var repaired = JsonPartialRepair.Close(patchRaw[..target]);
            var merged = JsonMerge.Merge(committedBase, JsonNode.Parse(repaired));
            Artifact = merged is null ? default : merged.Deserialize<T>(_deserializeOptions);
            Raise();

            if (step < steps) await Task.Delay(stepDelay, ct);
        }
    }

    // Advance to the end of the current token so the reveal grows word-by-word, not mid-word.
    private static int ExtendToWordBoundary(string s, int idx)
    {
        if (idx >= s.Length) return s.Length;
        while (idx < s.Length && !char.IsWhiteSpace(s[idx])) idx++;
        return idx;
    }
}
