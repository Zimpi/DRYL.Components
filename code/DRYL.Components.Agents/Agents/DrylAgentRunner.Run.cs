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
        // Approval-required tools: answer the agent on the same session, one user message
        // carrying every decision of the response.
        return StartFromUpdates(updates, aiKey, ct, (responses, c) => agent.RunStreamingAsync(
            new ChatMessage(ChatRole.User, responses.Cast<AIContent>().ToList()),
            session, options: null, cancellationToken: c));
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

    /// <summary>
    /// Like <see cref="Replay(IAsyncEnumerable{AgentResponseUpdate}, string?, CancellationToken)"/>,
    /// and answers approval requests: once the last pending call has a decision,
    /// <paramref name="resume"/> receives the responses and its updates continue the same run —
    /// what <see cref="Start"/> does with the agent and session it was given.
    /// </summary>
    public DrylAgentRun Replay(
        IAsyncEnumerable<AgentResponseUpdate> updates,
        Func<IReadOnlyList<ToolApprovalResponseContent>, CancellationToken, IAsyncEnumerable<AgentResponseUpdate>> resume,
        string? aiKey = null, CancellationToken ct = default)
        => StartFromUpdates(updates, aiKey, ct, resume);

    internal DrylAgentRun StartFromUpdates(
        IAsyncEnumerable<AgentResponseUpdate> updates, string? aiKey, CancellationToken ct,
        Func<IReadOnlyList<ToolApprovalResponseContent>, CancellationToken, IAsyncEnumerable<AgentResponseUpdate>>? resume = null)
    {
        var run = new DrylAgentRun();
        _ = ProcessAsync(run, updates, aiKey, ct, resume);
        return run;
    }

    /// <param name="resume">
    /// Answers approval requests and yields the agent's continuation. Without it, approval
    /// requests are ignored (the generation, build and orchestration paths).
    /// </param>
    private async Task ProcessAsync(
        DrylRunBase run, IAsyncEnumerable<AgentResponseUpdate> updates, string? aiKey, CancellationToken ct,
        Func<IReadOnlyList<ToolApprovalResponseContent>, CancellationToken, IAsyncEnumerable<AgentResponseUpdate>>? resume = null)
    {
        SetState(run, AiState.Thinking, aiKey);
        var byCallId = new Dictionary<string, DrylToolInvocation>();
        var awaiting = new List<DrylToolInvocation>();   // asked in this response, not yet answered

        try
        {
          while (true)
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

                        case ToolApprovalRequestContent ar when resume is not null:
                        {
                            var fcall = ar.ToolCall as FunctionCallContent;
                            if (!byCallId.TryGetValue(ar.ToolCall.CallId, out var inv))
                            {
                                inv = new DrylToolInvocation
                                {
                                    CallId = ar.ToolCall.CallId,
                                    ToolName = fcall?.Name ?? string.Empty,
                                    Arguments = fcall?.Arguments is null
                                        ? null : JsonSerializer.Serialize(fcall.Arguments, JsonOpts),
                                    Approval = DrylToolApproval.Pending,
                                    ApprovalRequest = ar,
                                };
                                byCallId[inv.CallId] = inv;
                                run.AddToolCall(inv);
                            }
                            else
                            {
                                inv.Approval = DrylToolApproval.Pending;
                                inv.ApprovalRequest = ar;
                                run.Raise();
                            }
                            awaiting.Add(inv);
                            break;
                        }

                        case FunctionCallContent fc when byCallId.TryGetValue(fc.CallId, out var known):
                            // The framework re-issues an approved call on resumption: one card per call.
                            if (known.Arguments is null && fc.Arguments is not null)
                                known.Arguments = JsonSerializer.Serialize(fc.Arguments, JsonOpts);
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
                                // A denied call's failed result is the framework telling the model,
                                // not a failure to show: the call stays quietly declined.
                                if (inv.Approval == DrylToolApproval.Declined) inv.Result = SerializeResult(fr.Result);
                                else if (fr.Exception is not null) inv.Error = fr.Exception.Message;
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

                        case UsageContent uc:
                            run.AddUsage(uc.Details);
                            break;
                    }
                }
            }

            if (awaiting.Count == 0 || resume is null) break;

            // The response ended asking a human: rest at Proposed until every call is decided,
            // then answer them all at once and stream the continuation into this same run.
            // The scope is cleared rather than set to Proposed: nothing is working, and the
            // components inheriting it carry no accept action (the I20 anti-decoration guard).
            if (run.PendingApprovals.Count > 0)
            {
                run.State = AiState.Proposed;
                if (_activity is not null && aiKey is not null) _activity.Clear(aiKey);
                run.Raise();
            }
            await run.WaitForDecisionsAsync(ct);
            var responses = awaiting
                .Select(i => i.ApprovalRequest!.CreateResponse(i.Approval == DrylToolApproval.Approved))
                .ToList();
            awaiting.Clear();
            SetState(run, AiState.Thinking, aiKey);
            updates = resume(responses, ct);
          }

            SetState(run, AiState.Generated, aiKey);
        }
        catch (OperationCanceledException)
        {
            SetStateRaw(run, AiState.None, aiKey);   // clear the scope on cancel
        }
        catch (Exception ex)
        {
            // Failed = Error set + AiState.None (mirrors DrylToolInvocation's convention).
            run.Error = new DrylRunError(ex.Message, ex);
            SetStateRaw(run, AiState.None, aiKey);
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
