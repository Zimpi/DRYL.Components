using System.Runtime.CompilerServices;
using DRYL.Components.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentRunnerGenerateTests
{
    private static async IAsyncEnumerable<AgentResponseUpdate> Updates(
        string[] textChunks, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var c in textChunks)
        {
            yield return new AgentResponseUpdate(ChatRole.Assistant,
                new List<AIContent> { new TextContent(c) });
            await Task.Yield();
        }
    }

    [Fact]
    public async Task ExtractJsonDeltas_yields_only_text_content()
    {
        var deltas = new List<string>();
        await foreach (var d in DrylAgentRunner.ExtractJsonDeltas(
            Updates(new[] { "{\"a\":", "1}" })))
        {
            deltas.Add(d);
        }
        Assert.Equal(new[] { "{\"a\":", "1}" }, deltas);
    }
}
