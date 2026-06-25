using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

public sealed partial class DrylAgentRunner
{
    /// <summary>
    /// Stream a typed structured generation: instructs the model to emit JSON conforming to
    /// <typeparamref name="T"/>'s schema and yields the raw JSON text deltas — drop the result
    /// straight into <c>&lt;DrylAiGenerate T Source="..."&gt;</c>.
    /// </summary>
    public IAsyncEnumerable<string> GenerateStreamingAsync<T>(
        AIAgent agent, AgentSession session, string prompt,
        string? aiKey = null, CancellationToken ct = default)
    {
        var runOptions = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<T>(
                    JsonOpts, typeof(T).Name, $"A {typeof(T).Name} object."),
            }
        };
        var updates = agent.RunStreamingAsync(prompt, session, runOptions, ct);
        return ExtractJsonDeltas(updates, ct);
    }

    /// <summary>Yields the text content of each update — the raw JSON token stream.</summary>
    internal static async IAsyncEnumerable<string> ExtractJsonDeltas(
        IAsyncEnumerable<AgentResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var update in updates.WithCancellation(ct))
        {
            foreach (var content in update.Contents)
            {
                if (content is TextContent tc && !string.IsNullOrEmpty(tc.Text))
                    yield return tc.Text;
            }
        }
    }
}
