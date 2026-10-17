using System.Runtime.CompilerServices;
using DRYL.Components;
using DRYL.Components.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

/// <summary>
/// I21 — framework-native tool approval. A tool wrapped in <c>ApprovalRequiredAIFunction</c>
/// makes the agent emit a <see cref="ToolApprovalRequestContent"/> instead of running it; the
/// runner turns that into a pending call, rests the run at <see cref="AiState.Proposed"/>, and
/// answers on the same run once every pending call is decided.
/// </summary>
public class DrylAgentRunnerApprovalTests
{
    private static async IAsyncEnumerable<AgentResponseUpdate> Updates(
        IEnumerable<AgentResponseUpdate> items,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var i in items) { ct.ThrowIfCancellationRequested(); await Task.Yield(); yield return i; }
    }

    private static AgentResponseUpdate Content(params AIContent[] c) => new(ChatRole.Assistant, c.ToList());

    private static ToolApprovalRequestContent Ask(string callId, string tool = "delete_customer") =>
        new($"req-{callId}", new FunctionCallContent(callId, tool, new Dictionary<string, object?> { ["id"] = 7 }));

    private static async Task Until(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++) await Task.Delay(10);
        Assert.True(condition(), "condition not reached");
    }

    /// <summary>A resume function that records what it was handed and replays a scripted continuation.</summary>
    private sealed class Resume
    {
        private readonly Queue<AgentResponseUpdate[]> _turns;
        public List<IReadOnlyList<ToolApprovalResponseContent>> Calls { get; } = new();

        public Resume(params AgentResponseUpdate[][] turns) => _turns = new(turns);

        public IAsyncEnumerable<AgentResponseUpdate> Invoke(
            IReadOnlyList<ToolApprovalResponseContent> responses, CancellationToken ct)
        {
            Calls.Add(responses);
            return Updates(_turns.Count > 0 ? _turns.Dequeue() : Array.Empty<AgentResponseUpdate>(), ct);
        }
    }

    [Fact]
    public async Task Request_becomes_a_pending_call_and_the_run_rests_at_proposed()
    {
        var resume = new Resume();
        var run = new DrylAgentRunner().Replay(Updates(new[] { Content(Ask("c1")) }), resume.Invoke);

        await Until(() => run.State == AiState.Proposed);

        var call = Assert.Single(run.ToolCalls);
        Assert.Equal("c1", call.CallId);
        Assert.Equal("delete_customer", call.ToolName);
        Assert.Contains("7", call.Arguments);
        Assert.Equal(DrylToolApproval.Pending, call.Approval);
        Assert.Equal(AiState.Proposed, call.State);
        Assert.Same(call, Assert.Single(run.PendingApprovals));
        Assert.Empty(resume.Calls);
        Assert.False(run.WaitForCompletionAsync().IsCompleted);
    }

    [Fact]
    public async Task Approve_answers_the_agent_and_the_same_run_streams_on()
    {
        var resume = new Resume(new[]
        {
            Content(new FunctionResultContent("c1", "deleted")),
            Content(new TextContent("Done, customer 7 is gone.")),
        });
        var run = new DrylAgentRunner().Replay(
            Updates(new[] { Content(new TextContent("Deleting. ")), Content(Ask("c1")) }), resume.Invoke);
        await Until(() => run.State == AiState.Proposed);

        Assert.True(run.Approve("c1"));
        await run.WaitForCompletionAsync();

        var response = Assert.Single(Assert.Single(resume.Calls));
        Assert.True(response.Approved);
        Assert.Equal("c1", response.ToolCall.CallId);
        Assert.Equal("Deleting. Done, customer 7 is gone.", run.Text);
        var call = Assert.Single(run.ToolCalls);
        Assert.Equal(DrylToolApproval.Approved, call.Approval);
        Assert.Equal("\"deleted\"", call.Result);
        Assert.Equal(AiState.Generated, call.State);
        Assert.Equal(AiState.Generated, run.State);
    }

    [Fact]
    public async Task Deny_answers_not_approved_and_the_call_stays_quietly_declined()
    {
        var resume = new Resume(new[]
        {
            Content(new FunctionResultContent("c1", "Tool call invocation rejected.")
                { Exception = new InvalidOperationException("rejected") }),
            Content(new TextContent("Okay, I left it.")),
        });
        var run = new DrylAgentRunner().Replay(Updates(new[] { Content(Ask("c1")) }), resume.Invoke);
        await Until(() => run.State == AiState.Proposed);

        Assert.True(run.Deny("c1"));
        await run.WaitForCompletionAsync();

        Assert.False(Assert.Single(Assert.Single(resume.Calls)).Approved);
        var call = Assert.Single(run.ToolCalls);
        Assert.Equal(DrylToolApproval.Declined, call.Approval);
        Assert.Null(call.Error);
        Assert.Equal(AiState.None, call.State);
        Assert.Equal(AiState.Generated, run.State);
    }

    [Fact]
    public async Task Waiting_clears_the_scope_and_resuming_lights_it_again()
    {
        var activity = new DRYL.Components.Ai.DrylAiActivityService();
        var gate = new TaskCompletionSource();
        async IAsyncEnumerable<AgentResponseUpdate> Continuation(
            IReadOnlyList<ToolApprovalResponseContent> _, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await gate.Task;
            yield return Content(new TextContent("ok"));
        }
        var run = new DrylAgentRunner(activity).Replay(
            Updates(new[] { Content(Ask("c1")) }), Continuation, aiKey: "admin");
        await Until(() => run.State == AiState.Proposed);

        Assert.Equal(AiState.None, activity.GetState("admin"));

        run.Approve("c1");
        await Until(() => activity.GetState("admin") == AiState.Thinking);
        gate.SetResult();
        await run.WaitForCompletionAsync();
    }

    [Fact]
    public async Task The_run_waits_for_the_last_pending_call()
    {
        var resume = new Resume();
        var run = new DrylAgentRunner().Replay(
            Updates(new[] { Content(Ask("c1"), Ask("c2", "send_mail")) }), resume.Invoke);
        await Until(() => run.State == AiState.Proposed);

        run.Approve("c1");
        await Task.Delay(50);
        Assert.Equal(AiState.Proposed, run.State);
        Assert.Empty(resume.Calls);
        Assert.Equal(new[] { "c2" }, run.PendingApprovals.Select(t => t.CallId));

        run.Deny("c2");
        await run.WaitForCompletionAsync();

        var responses = Assert.Single(resume.Calls);
        Assert.Equal(2, responses.Count);
        Assert.True(responses.Single(r => r.ToolCall.CallId == "c1").Approved);
        Assert.False(responses.Single(r => r.ToolCall.CallId == "c2").Approved);
    }

    [Fact]
    public async Task Each_decision_raises_on_change()
    {
        var run = new DrylAgentRunner().Replay(
            Updates(new[] { Content(Ask("c1"), Ask("c2")) }), new Resume().Invoke);
        await Until(() => run.State == AiState.Proposed);
        var raised = 0;
        run.OnChange += () => raised++;

        run.Approve("c1");

        Assert.True(raised >= 1);
    }

    [Fact]
    public async Task Unknown_or_decided_ids_return_false()
    {
        var run = new DrylAgentRunner().Replay(Updates(new[] { Content(Ask("c1"), Ask("c2")) }), new Resume().Invoke);
        await Until(() => run.State == AiState.Proposed);

        Assert.False(run.Approve("nope"));
        Assert.False(run.Deny("nope"));
        Assert.True(run.Approve("c1"));
        Assert.False(run.Deny("c1"));
        Assert.Equal(DrylToolApproval.Approved, run.ToolCalls[0].Approval);
    }

    [Fact]
    public async Task A_decision_before_the_response_ends_needs_no_wait()
    {
        var gate = new TaskCompletionSource();
        async IAsyncEnumerable<AgentResponseUpdate> Script()
        {
            yield return Content(Ask("c1"));
            await gate.Task;   // the human decides while the model is still talking
            yield return Content(new TextContent("…"));
        }
        var resume = new Resume(new[] { Content(new TextContent(" ok")) });
        var run = new DrylAgentRunner().Replay(Script(), resume.Invoke);
        await Until(() => run.PendingApprovals.Count == 1);

        run.Approve("c1");
        gate.SetResult();
        await run.WaitForCompletionAsync();

        Assert.True(Assert.Single(Assert.Single(resume.Calls)).Approved);
        Assert.Equal(AiState.Generated, run.State);
    }

    [Fact]
    public async Task A_continuation_that_asks_again_returns_to_proposed()
    {
        var resume = new Resume(
            new[] { Content(new FunctionResultContent("c1", "ok")), Content(Ask("c2", "send_mail")) },
            new[] { Content(new FunctionResultContent("c2", "sent")), Content(new TextContent("All done.")) });
        var run = new DrylAgentRunner().Replay(Updates(new[] { Content(Ask("c1")) }), resume.Invoke);
        await Until(() => run.State == AiState.Proposed);

        run.Approve("c1");
        await Until(() => run.State == AiState.Proposed && run.PendingApprovals.Any(t => t.CallId == "c2"));
        run.Approve("c2");
        await run.WaitForCompletionAsync();

        Assert.Equal(2, resume.Calls.Count);
        Assert.Equal("c2", Assert.Single(resume.Calls[1]).ToolCall.CallId);   // only the new request
        Assert.Equal(2, run.ToolCalls.Count);
        Assert.Equal("All done.", run.Text);
    }

    [Fact]
    public async Task A_reissued_function_call_does_not_add_a_second_card()
    {
        var resume = new Resume(new[]
        {
            Content(new FunctionCallContent("c1", "delete_customer")),
            Content(new FunctionResultContent("c1", "deleted")),
        });
        var run = new DrylAgentRunner().Replay(Updates(new[] { Content(Ask("c1")) }), resume.Invoke);
        await Until(() => run.State == AiState.Proposed);

        run.Approve("c1");
        await run.WaitForCompletionAsync();

        var call = Assert.Single(run.ToolCalls);
        Assert.Equal(DrylToolApproval.Approved, call.Approval);
    }

    [Fact]
    public async Task Cancelling_while_pending_declines_and_settles_at_none()
    {
        using var cts = new CancellationTokenSource();
        var run = new DrylAgentRunner().Replay(
            Updates(new[] { Content(Ask("c1")) }), new Resume().Invoke, ct: cts.Token);
        await Until(() => run.State == AiState.Proposed);

        cts.Cancel();
        await run.WaitForCompletionAsync();

        Assert.Equal(DrylToolApproval.Declined, run.ToolCalls[0].Approval);
        Assert.Equal(AiState.None, run.State);
        Assert.Null(run.Error);
        Assert.Empty(run.PendingApprovals);
    }

    [Fact]
    public async Task Disposing_while_pending_ends_the_wait()
    {
        var run = new DrylAgentRunner().Replay(Updates(new[] { Content(Ask("c1")) }), new Resume().Invoke);
        await Until(() => run.State == AiState.Proposed);

        await run.DisposeAsync();

        Assert.True(run.WaitForCompletionAsync().Wait(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Replay_without_resume_ignores_approval_requests_as_before()
    {
        var run = new DrylAgentRunner().Replay(Updates(new[] { Content(Ask("c1")), Content(new TextContent("x")) }));

        await run.WaitForCompletionAsync();

        Assert.Empty(run.ToolCalls);
        Assert.Empty(run.PendingApprovals);
        Assert.Equal(AiState.Generated, run.State);
    }

    // ── Through the real framework ───────────────────────────────────────

    /// <summary>
    /// A scripted model: first turn calls the tool, every later turn just answers. The real
    /// <see cref="ChatClientAgent"/> and its function-invoking client sit on top, so the
    /// approval request and the resumption are the framework's own.
    /// </summary>
    private sealed class ScriptedModel : IChatClient
    {
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var list = messages.ToList();
            var result = list.SelectMany(m => m.Contents).OfType<FunctionResultContent>().LastOrDefault();
            if (result is null)
                yield return new ChatResponseUpdate(ChatRole.Assistant,
                    new List<AIContent> { new FunctionCallContent("call-1", "delete_customer",
                        new Dictionary<string, object?> { ["id"] = 7 }) });
            else
                yield return new ChatResponseUpdate(ChatRole.Assistant, $"Result: {result.Result}");
        }

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var updates = new List<ChatResponseUpdate>();
            await foreach (var u in GetStreamingResponseAsync(messages, options, cancellationToken)) updates.Add(u);
            return updates.ToChatResponse();
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }

    private static (AIAgent Agent, Func<int> Invocations) ApprovalAgent()
    {
        var invocations = 0;
        var tool = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(
            (int id) => { invocations++; return $"customer {id} deleted"; }, "delete_customer"));
        var agent = new ChatClientAgent(new ScriptedModel(), instructions: null, name: "Admin",
            description: null, tools: new List<AITool> { tool });
        return (agent, () => invocations);
    }

    [Fact]
    public async Task Framework_round_trip_approve_runs_the_tool_on_the_same_session()
    {
        var (agent, invocations) = ApprovalAgent();
        var session = await agent.CreateSessionAsync();
        var run = new DrylAgentRunner().Start(agent, session, "Delete customer 7.");

        await Until(() => run.State == AiState.Proposed);
        var pending = Assert.Single(run.PendingApprovals);
        Assert.Equal("delete_customer", pending.ToolName);
        Assert.Equal(0, invocations());

        run.Approve(pending.CallId);
        await run.WaitForCompletionAsync();

        Assert.Null(run.Error);
        Assert.Equal(1, invocations());
        Assert.Contains("customer 7 deleted", run.Text);
        Assert.Single(run.ToolCalls);
        Assert.Equal(AiState.Generated, run.ToolCalls[0].State);
        Assert.Equal(AiState.Generated, run.State);
    }

    [Fact]
    public async Task Framework_round_trip_deny_never_runs_the_tool()
    {
        var (agent, invocations) = ApprovalAgent();
        var session = await agent.CreateSessionAsync();
        var run = new DrylAgentRunner().Start(agent, session, "Delete customer 7.");

        await Until(() => run.State == AiState.Proposed);
        run.Deny(run.PendingApprovals[0].CallId);
        await run.WaitForCompletionAsync();

        Assert.Null(run.Error);
        Assert.Equal(0, invocations());
        var call = Assert.Single(run.ToolCalls);
        Assert.Equal(DrylToolApproval.Declined, call.Approval);
        Assert.Null(call.Error);
        Assert.Equal(AiState.None, call.State);
    }
}
