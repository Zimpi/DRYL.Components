using System.ComponentModel;
using System.Text.Json;
using DRYL.Components.Agents;
using Microsoft.Extensions.AI;
using Xunit;

namespace DRYL.Components.Tests.Agents.Voice;

/// <summary>The run is what the dock watches and what the host owns. The browser only reports
/// into it — every decision that matters (which tool exists, what it returns) stays here.</summary>
public class DrylVoiceRunTests
{
    private static DrylVoiceRun Run(DrylVoiceOptions? options = null) =>
        new DrylVoiceRunner(new NoopJsRuntime())
            .Create(options ?? new DrylVoiceOptions { ApiKey = "sk-test" });

    [Fact]
    public void A_fresh_run_is_idle_and_silent()
    {
        var run = Run();

        Assert.Equal(VoicePhase.Idle, run.Phase);
        Assert.Equal(AiState.None, run.State);
        Assert.False(run.IsActive);
        Assert.Empty(run.Transcript);
    }

    [Theory]
    [InlineData(VoiceActivity.Listening, AiState.Active)]
    [InlineData(VoiceActivity.UserSpeaking, AiState.Active)]
    [InlineData(VoiceActivity.Thinking, AiState.Thinking)]
    [InlineData(VoiceActivity.Speaking, AiState.Streaming)]
    public void Live_activity_maps_onto_the_shared_ai_vocabulary(
        VoiceActivity activity, AiState expected)
    {
        var run = Run();
        run.OnConnected();
        run.OnActivity(activity.ToString());

        Assert.Equal(expected, run.State);
    }

    [Fact]
    public void Connecting_reads_as_thinking_not_as_idle()
    {
        // The dock breathes while the peer connection is being set up — an idle-looking dock
        // during a two-second handshake reads as "the button did nothing".
        var run = Run();
        run.MarkConnecting();

        Assert.Equal(AiState.Thinking, run.State);
        Assert.True(run.IsActive);
    }

    [Fact]
    public void Activity_reported_before_the_channel_is_open_is_ignored()
    {
        var run = Run();
        run.MarkConnecting();
        run.OnActivity(nameof(VoiceActivity.Speaking));

        Assert.Equal(AiState.Thinking, run.State);
    }

    [Fact]
    public void Transcript_lines_arrive_in_order_and_keep_their_speaker()
    {
        var run = Run();
        run.OnConnected();
        run.OnTranscript("User", "Zeig mir die Projekte");
        run.OnTranscript("Assistant", "Ist gebaut.");

        Assert.Collection(
            run.Transcript,
            m =>
            {
                Assert.Equal(VoiceRole.User, m.Role);
                Assert.Equal("Zeig mir die Projekte", m.Text);
            },
            m =>
            {
                Assert.Equal(VoiceRole.Assistant, m.Role);
                Assert.Equal("Ist gebaut.", m.Text);
            });
    }

    [Fact]
    public void Empty_transcript_lines_are_dropped()
    {
        // A cough gets transcribed as "" and would otherwise become a blank bubble in the log.
        var run = Run();
        run.OnConnected();
        run.OnTranscript("User", "   ");

        Assert.Empty(run.Transcript);
    }

    [Fact]
    public async Task A_tool_call_runs_the_real_function_and_returns_its_json()
    {
        var options = new DrylVoiceOptions { ApiKey = "sk-test" };
        options.Tools.Add(AIFunctionFactory.Create(
            ([Description("the city")] string city) => $"sunny in {city}", "get_weather"));
        var run = Run(options);
        run.OnConnected();

        var result = await run.OnToolCallAsync("call_1", "get_weather", """{"city":"Bonn"}""");

        Assert.Contains("sunny in Bonn", result);
        var call = Assert.Single(run.ToolCalls);
        Assert.Equal("get_weather", call.ToolName);
        Assert.Equal("call_1", call.CallId);
        Assert.NotNull(call.Result);
        Assert.Null(call.Error);
    }

    [Fact]
    public async Task An_invented_tool_name_comes_back_as_an_error_result_not_an_exception()
    {
        // Returning nothing would leave the model waiting for a result that never arrives, and
        // the conversation dies mid-sentence. An error it can read keeps it talking.
        var run = Run();
        run.OnConnected();

        var result = await run.OnToolCallAsync("call_1", "no_such_tool", "{}");

        using var json = JsonDocument.Parse(result);
        Assert.Contains("no_such_tool", json.RootElement.GetProperty("error").GetString());

        var call = Assert.Single(run.ToolCalls);
        Assert.Equal("no_such_tool", call.ToolName);
        Assert.NotNull(call.Error);
    }

    [Fact]
    public async Task A_throwing_tool_comes_back_as_an_error_result()
    {
        var options = new DrylVoiceOptions { ApiKey = "sk-test" };
        options.Tools.Add(AIFunctionFactory.Create(
            (Func<string>)(() => throw new InvalidOperationException("Datenbank weg")),
            "list_projects"));
        var run = Run(options);
        run.OnConnected();

        var result = await run.OnToolCallAsync("call_1", "list_projects", "{}");

        Assert.Contains("Datenbank weg", result);
        Assert.NotNull(Assert.Single(run.ToolCalls).Error);
    }

    [Fact]
    public async Task A_tool_without_arguments_is_called_with_none()
    {
        var options = new DrylVoiceOptions { ApiKey = "sk-test" };
        options.Tools.Add(AIFunctionFactory.Create(() => "drei Projekte", "count_projects"));
        var run = Run(options);
        run.OnConnected();

        Assert.Contains("drei Projekte", await run.OnToolCallAsync("call_1", "count_projects", ""));
    }

    // ── continuing a turn the model ended on its own ─────────────────────────

    [Fact]
    public async Task Without_a_predicate_a_finished_turn_stays_finished()
    {
        // The default has to be the old behaviour: a plain conversation gives the floor back
        // after every answer, and a host that never opted in must not suddenly be talked at.
        var run = Run();
        run.OnConnected();

        Assert.False(await run.OnTurnEndedAsync());
    }

    [Fact]
    public async Task Open_work_sends_the_model_back_to_it()
    {
        var run = Run();
        run.OnConnected();
        run.ShouldContinue = () => ValueTask.FromResult(true);

        Assert.True(await run.OnTurnEndedAsync());
        Assert.Equal(AiState.Thinking, run.State);   // the dock keeps breathing, not listening
    }

    [Fact]
    public async Task Finished_work_hands_the_floor_back()
    {
        var run = Run();
        run.OnConnected();
        run.ShouldContinue = () => ValueTask.FromResult(false);

        Assert.False(await run.OnTurnEndedAsync());
    }

    [Fact]
    public async Task A_turn_ending_outside_a_live_session_continues_nothing()
    {
        // response.done can arrive as the session is being torn down; prompting a connection
        // that is on its way out would be an error event and, before this, a dead session.
        var run = Run();
        run.ShouldContinue = () => ValueTask.FromResult(true);

        Assert.False(await run.OnTurnEndedAsync());
    }

    [Fact]
    public async Task A_predicate_that_never_goes_false_still_runs_out_of_turns()
    {
        // The backstop. A task the model cannot finish would otherwise have it nudging itself
        // forever — billed per minute, and impossible to interrupt politely.
        var run = Run();
        run.OnConnected();
        run.MaxAutoContinuations = 3;
        run.ShouldContinue = () => ValueTask.FromResult(true);

        Assert.True(await run.OnTurnEndedAsync());
        Assert.True(await run.OnTurnEndedAsync());
        Assert.True(await run.OnTurnEndedAsync());
        Assert.False(await run.OnTurnEndedAsync());
    }

    [Fact]
    public async Task Running_a_tool_earns_the_budget_back()
    {
        // Only fruitless turns are capped. A long plan that is actually progressing must not run
        // out of continuations precisely because it is going well.
        var options = new DrylVoiceOptions { ApiKey = "sk-test" };
        options.Tools.Add(AIFunctionFactory.Create(() => "ok", "list_projects"));
        var run = Run(options);
        run.OnConnected();
        run.MaxAutoContinuations = 2;
        run.ShouldContinue = () => ValueTask.FromResult(true);

        Assert.True(await run.OnTurnEndedAsync());
        Assert.True(await run.OnTurnEndedAsync());
        Assert.False(await run.OnTurnEndedAsync());

        await run.OnToolCallAsync("call_1", "list_projects", "{}");

        Assert.True(await run.OnTurnEndedAsync());
    }

    [Fact]
    public async Task The_user_speaking_earns_the_budget_back()
    {
        var run = Run();
        run.OnConnected();
        run.MaxAutoContinuations = 1;
        run.ShouldContinue = () => ValueTask.FromResult(true);

        Assert.True(await run.OnTurnEndedAsync());
        Assert.False(await run.OnTurnEndedAsync());

        run.OnActivity(nameof(VoiceActivity.UserSpeaking));

        Assert.True(await run.OnTurnEndedAsync());
    }

    [Fact]
    public async Task A_throwing_predicate_ends_the_turn_instead_of_the_session()
    {
        var run = Run();
        run.OnConnected();
        run.ShouldContinue = () => throw new InvalidOperationException("Taskliste weg");

        Assert.False(await run.OnTurnEndedAsync());
        Assert.Equal(VoicePhase.Live, run.Phase);    // the conversation carries on regardless
        Assert.Null(run.Error);
    }

    [Fact]
    public async Task A_new_session_starts_with_a_full_budget()
    {
        var run = Run();
        run.OnConnected();
        run.MaxAutoContinuations = 1;
        run.ShouldContinue = () => ValueTask.FromResult(true);

        Assert.True(await run.OnTurnEndedAsync());
        Assert.False(await run.OnTurnEndedAsync());

        run.OnClosed();
        run.MarkConnecting();
        run.OnConnected();

        Assert.True(await run.OnTurnEndedAsync());
    }

    [Fact]
    public void A_failure_settles_the_run_instead_of_leaving_it_breathing()
    {
        var run = Run();
        run.MarkConnecting();
        run.OnFailed("Kein Zugriff aufs Mikrofon.");

        Assert.Equal(VoicePhase.Idle, run.Phase);
        Assert.Equal(AiState.None, run.State);
        Assert.Equal("Kein Zugriff aufs Mikrofon.", run.Error!.Message);
    }

    [Fact]
    public void Starting_clears_the_error_of_the_previous_attempt()
    {
        var run = Run();
        run.OnFailed("Kein Zugriff aufs Mikrofon.");
        run.MarkConnecting();

        Assert.Null(run.Error);
    }

    [Fact]
    public void The_transcript_survives_a_session_but_a_new_start_begins_a_new_one()
    {
        var run = Run();
        run.OnConnected();
        run.OnTranscript("User", "Hallo");
        run.OnClosed();

        Assert.Single(run.Transcript);

        run.MarkConnecting();
        Assert.Empty(run.Transcript);
    }

    [Fact]
    public async Task A_new_session_starts_with_an_empty_trace()
    {
        // The run is reused for every session, so without this the tool calls of an old
        // conversation would still be sitting in the log during the next one.
        var options = new DrylVoiceOptions { ApiKey = "sk-test" };
        options.Tools.Add(AIFunctionFactory.Create(() => "ok", "list_projects"));
        var run = Run(options);
        run.OnConnected();
        await run.OnToolCallAsync("call_1", "list_projects", "{}");

        Assert.Single(run.ToolCalls);

        run.OnClosed();
        Assert.Single(run.ToolCalls);      // nach dem Auflegen noch lesbar

        run.MarkConnecting();
        Assert.Empty(run.ToolCalls);       // die nächste Sitzung räumt sie weg
    }

    [Fact]
    public async Task Stopping_an_idle_run_is_a_no_op()
    {
        var run = Run();
        var beats = 0;
        run.OnChange += () => beats++;

        await run.StopAsync();

        Assert.Equal(0, beats);
        Assert.Equal(VoicePhase.Idle, run.Phase);
    }

    [Fact]
    public void Change_notifications_reach_the_host()
    {
        var run = Run();
        var beats = 0;
        run.OnChange += () => beats++;

        run.MarkConnecting();
        run.OnConnected();
        run.OnActivity(nameof(VoiceActivity.Speaking));

        Assert.Equal(3, beats);
    }
}
