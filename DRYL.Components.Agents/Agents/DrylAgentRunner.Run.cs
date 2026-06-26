using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

public sealed partial class DrylAgentRunner
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    /// <summary>
    /// Start an agent run and return an observable <see cref="DrylAgentRun"/>. The run drives
    /// <see cref="AiState"/> automatically and (when <paramref name="aiKey"/> is set and the
    /// activity service is registered) lights up a surrounding <c>DrylAiScope Key="..."</c>.
    /// </summary>
    public DrylAgentRun Start(AIAgent agent, AgentSession session, string message,
                              string? aiKey = null, CancellationToken ct = default)
    {
        var updates = agent.RunStreamingAsync(message, session, options: null, cancellationToken: ct);
        return StartFromUpdates(updates, aiKey, ct);
    }

    /// <summary>
    /// Drive a <see cref="DrylAgentRun"/> from a pre-built sequence of
    /// <see cref="AgentResponseUpdate"/>s instead of a live agent — useful for replaying a
    /// recorded run, demos, or tests. Behaves exactly like <see cref="Start"/> (same
    /// automatic <see cref="AiState"/> and tool-call mapping).
    /// </summary>
    public DrylAgentRun Replay(
        IAsyncEnumerable<AgentResponseUpdate> updates, string? aiKey = null, CancellationToken ct = default)
        => StartFromUpdates(updates, aiKey, ct);

    internal DrylAgentRun StartFromUpdates(
        IAsyncEnumerable<AgentResponseUpdate> updates, string? aiKey, CancellationToken ct)
    {
        var run = new DrylAgentRun();
        _ = ProcessAsync(run, updates, aiKey, ct);
        return run;
    }

    private async Task ProcessAsync(
        DrylRunBase run, IAsyncEnumerable<AgentResponseUpdate> updates, string? aiKey, CancellationToken ct)
    {
        SetState(run, AiState.Thinking, aiKey);
        var byCallId = new Dictionary<string, DrylToolInvocation>();

        try
        {
            await foreach (var update in updates.WithCancellation(ct))
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case TextReasoningContent:
                            SetState(run, AiState.Thinking, aiKey);
                            break;

                        case FunctionCallContent fc:
                        {
                            var inv = new DrylToolInvocation
                            {
                                CallId = fc.CallId,
                                ToolName = fc.Name,
                                Arguments = fc.Arguments is null
                                    ? null : JsonSerializer.Serialize(fc.Arguments, JsonOpts),
                            };
                            byCallId[fc.CallId] = inv;
                            run.AddToolCall(inv);
                            SetState(run, AiState.Thinking, aiKey);
                            break;
                        }

                        case FunctionResultContent fr:
                        {
                            if (byCallId.TryGetValue(fr.CallId, out var inv))
                            {
                                if (fr.Exception is not null) inv.Error = fr.Exception.Message;
                                else inv.Result = SerializeResult(fr.Result);
                                run.Raise();
                            }
                            break;
                        }

                        case TextContent tc when !string.IsNullOrEmpty(tc.Text):
                            run.Text += tc.Text;
                            run.PushText(tc.Text);
                            SetState(run, AiState.Streaming, aiKey);
                            break;
                    }
                }
            }

            SetState(run, AiState.Generated, aiKey);
        }
        catch (OperationCanceledException)
        {
            SetStateRaw(run, AiState.None, aiKey);   // clear the scope on cancel
        }
        catch (Exception)
        {
            SetStateRaw(run, AiState.None, aiKey);    // surface via consumer; keep UI consistent
        }
        finally
        {
            run.CompleteText();
            run.MarkCompleted();
        }
    }

    private static string SerializeResult(object? result) =>
        result switch
        {
            null => "null",
            string s => JsonSerializer.Serialize(s, JsonOpts),
            JsonElement je => je.GetRawText(),
            _ => JsonSerializer.Serialize(result, JsonOpts),
        };

    private void SetState(DrylRunBase run, AiState state, string? aiKey)
    {
        run.State = state;
        if (_activity is not null && aiKey is not null) _activity.Set(aiKey, state);
        run.Raise();
    }

    private void SetStateRaw(DrylRunBase run, AiState state, string? aiKey)
    {
        run.State = state;
        if (_activity is not null && aiKey is not null)
        {
            if (state == AiState.None) _activity.Clear(aiKey);
            else _activity.Set(aiKey, state);
        }
        run.Raise();
    }
}
