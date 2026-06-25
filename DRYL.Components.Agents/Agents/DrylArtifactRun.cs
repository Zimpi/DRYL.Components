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
    private readonly JsonSerializerOptions _jsonOptions;
    private JsonNode? _json;

    internal DrylArtifactRun(JsonSerializerOptions jsonOptions) => _jsonOptions = jsonOptions;

    /// <summary>The live, progressively-merged artifact (fields not yet provided are null/default).</summary>
    public T? Artifact { get; private set; }

    /// <summary>The number of <c>update_&lt;T&gt;</c> merge steps applied so far.</summary>
    public int Round { get; private set; }

    /// <summary>
    /// Merge a partial-<typeparamref name="T"/> patch into the running artifact, raise
    /// <see cref="DrylRunBase.OnChange"/>, and return a short receipt for the model. When
    /// <paramref name="maxRounds"/> is reached, returns a finalize nudge instead.
    /// </summary>
    internal string ApplyPatch(JsonElement patch, int? maxRounds)
    {
        var patchNode = patch.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : JsonNode.Parse(patch.GetRawText());

        _json = JsonMerge.Merge(_json, patchNode);
        Round++;
        var opts = new JsonSerializerOptions(_jsonOptions) { PropertyNameCaseInsensitive = true };
        Artifact = _json is null ? default : _json.Deserialize<T>(opts);
        Raise();

        return maxRounds is { } m && Round >= m
            ? "Maximum refinement rounds reached — stop refining and give your final answer now."
            : $"Updated (round {Round}).";
    }
}
