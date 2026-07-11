using System.Runtime.CompilerServices;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Agents;

public class DrylAiFieldTests : BunitContext
{
    private readonly BunitJSModuleInterop _module;

    public DrylAiFieldTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;   // tooltip/popover/presence JS is irrelevant here
        Services.AddDrylAgents();
        _module = JSInterop.SetupModule("./_content/DRYL.Components.Agents/js/dryl-aifield.js");
        _module.Mode = JSRuntimeMode.Loose;      // void calls (write/setBusy/focusField) auto-succeed
    }

    private void SetupSnapshot(AiFieldSnapshot snap) =>
        _module.Setup<AiFieldSnapshot>("snapshot", _ => true).SetResult(snap);

    /// <summary>A scripted IChatClient that streams fixed chunks; used through the REAL ChatClientAgent.</summary>
    private sealed class ScriptedChatClient : IChatClient
    {
        private readonly string[] _chunks;
        public string? LastUserMessage { get; private set; }

        /// <summary>
        /// How many times a streaming call actually started. Iterator methods defer execution until
        /// the first MoveNextAsync, but that first step (including this increment) still runs
        /// synchronously before the method's first await — so this reliably counts distinct runs,
        /// not just distinct calls that were never enumerated.
        /// </summary>
        public int Calls { get; private set; }

        public ScriptedChatClient(params string[] chunks) => _chunks = chunks;

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Calls++;
            LastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
            foreach (var chunk in _chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
            }
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

    private static AIAgent Agent(IChatClient client) =>
        new ChatClientAgent(client, instructions: "test", name: "t", description: null, tools: null);

    // ── Task 3: skeleton ────────────────────────────────────────────────────

    [Fact]
    public void Renders_child_content_inside_ai_scope()
    {
        var cut = Render<DrylAiField>(ps => ps
            .Add(p => p.Agent, Agent(new ScriptedChatClient()))
            .Add(p => p.Instruction, "x")
            .AddChildContent("<textarea id=\"target\"></textarea>"));

        Assert.NotNull(cut.Find(".ai-field #target"));
    }

    [Fact]
    public void Trigger_is_icon_only_with_tooltip_and_aria_label()
    {
        var cut = Render<DrylAiField>(ps => ps
            .Add(p => p.Agent, Agent(new ScriptedChatClient()))
            .Add(p => p.Instruction, "x")
            .Add(p => p.TriggerLabel, "Feld ausfüllen")
            .AddChildContent("<textarea></textarea>"));

        var btn = cut.Find(".ai-field-trigger button");
        Assert.Equal("Feld ausfüllen", btn.GetAttribute("aria-label"));
        Assert.Equal("Feld ausfüllen", cut.Find(".ai-field-trigger .tt-wrap").GetAttribute("data-tt"));
    }

    [Fact]
    public void Disabled_hides_the_trigger()
    {
        var cut = Render<DrylAiField>(ps => ps
            .Add(p => p.Agent, Agent(new ScriptedChatClient()))
            .Add(p => p.Instruction, "x")
            .Add(p => p.Disabled, true)
            .AddChildContent("<textarea></textarea>"));

        Assert.Empty(cut.FindAll(".ai-field-trigger"));
    }

    [Fact]
    public void Class_is_merged_onto_the_root()
    {
        var cut = Render<DrylAiField>(ps => ps
            .Add(p => p.Agent, Agent(new ScriptedChatClient()))
            .Add(p => p.Instruction, "x")
            .Add(p => p.Class, "my-extra")
            .AddChildContent("<textarea></textarea>"));

        var cls = cut.Find(".ai-field").GetAttribute("class");
        Assert.Contains("my-extra", cls);
    }

    [Fact]
    public void No_review_chips_and_no_popover_initially()
    {
        var cut = Render<DrylAiField>(ps => ps
            .Add(p => p.Agent, Agent(new ScriptedChatClient()))
            .Add(p => p.Instruction, "x")
            .AddChildContent("<textarea></textarea>"));

        Assert.Empty(cut.FindAll(".ai-field-review"));
        Assert.Empty(cut.FindAll(".ai-field-prompt"));
    }

    // ── Task 4: direct-run streaming ────────────────────────────────────────

    private IRenderedComponent<DrylAiField> RenderField(
        AIAgent agent, string? instruction = "Rewrite professionally",
        Action<ComponentParameterCollectionBuilder<DrylAiField>>? extra = null)
        => Render<DrylAiField>(ps =>
        {
            ps.Add(p => p.Agent, agent)
              .AddChildContent("<textarea></textarea>");
            if (instruction is not null) ps.Add(p => p.Instruction, instruction);
            extra?.Invoke(ps);
        });

    [Fact]
    public void Trigger_click_streams_result_into_field_and_enters_review()
    {
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "yo, report pls", SelStart = 0, SelEnd = 0 });
        var client = new ScriptedChatClient("Dear ", "team, ", "please send the report.");
        var cut = RenderField(Agent(client));

        cut.Find(".ai-field-trigger button").Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".ai-field-review")));

        // the final write carries the full cleaned text
        var writes = _module.Invocations.Where(i => i.Identifier == "write").ToList();
        Assert.NotEmpty(writes);
        Assert.Equal("Dear team, please send the report.", writes[^1].Arguments[1]);

        // prompt was built from instruction + field value
        Assert.Contains("Rewrite professionally", client.LastUserMessage);
        Assert.Contains("yo, report pls", client.LastUserMessage);

        // field was set busy at start
        Assert.Contains(_module.Invocations, i => i.Identifier == "setBusy");
    }

    [Fact]
    public async Task Rapid_double_click_runs_the_agent_only_once()
    {
        // TOCTOU regression: the old guard only flipped `_phase` to Running AFTER two awaits
        // (module load + snapshot). A second dispatch landing inside that window sailed straight
        // past `if (_phase == Running) return` and started a second, independent run. We hold the
        // snapshot call pending so both clicks are forced to land inside that exact window —
        // the first click's synchronous run stalls right there until we release it below.
        var snapshotCall = _module.Setup<AiFieldSnapshot>("snapshot", _ => true);
        var client = new ScriptedChatClient("Dear ", "team, ", "please send the report.");
        var cut = RenderField(Agent(client));

        var click1 = cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());
        await cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());

        // Release the stalled snapshot call(s). Under the old bug, both dispatches would be
        // waiting here and both would proceed; under the fix only the first ever reaches it.
        snapshotCall.SetResult(new AiFieldSnapshot { Found = true, Value = "before", SelStart = 0, SelEnd = 0 });
        await click1;

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".ai-field-review")));

        // The core guarantee: the second click must never start an independent second run.
        Assert.Equal(1, client.Calls);
        Assert.Equal(1, _module.Invocations.Count(i => i.Identifier == "snapshot"));
    }

    [Fact]
    public void Selection_is_transformed_in_place()
    {
        // value "Hallo Welt!", selection [0,10) = "Hallo Welt"
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "Hallo Welt!", SelStart = 0, SelEnd = 10 });
        var client = new ScriptedChatClient("Hello World");
        var cut = RenderField(Agent(client), "Translate to English");

        cut.Find(".ai-field-trigger button").Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".ai-field-review")));

        var writes = _module.Invocations.Where(i => i.Identifier == "write").ToList();
        Assert.Equal("Hello World!", writes[^1].Arguments[1]);          // prefix "" + result + suffix "!"
        Assert.Contains("Selected portion", client.LastUserMessage);
        Assert.Contains("Hallo Welt", client.LastUserMessage);
    }

    [Fact]
    public void Aura_reaches_generated_on_review()
    {
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "", SelStart = 0, SelEnd = 0 });
        var cut = Render<DrylAiField>(ps => ps
            .Add(p => p.Agent, Agent(new ScriptedChatClient("Hi")))
            .Add(p => p.Instruction, "x")
            .AddChildContent<DrylTextarea>(tp => tp.Add(t => t.Value, "")));

        cut.Find(".ai-field-trigger button").Click();

        // DrylTextarea consumes the cascaded scope → wrapper gets ai-aura + ai-generated in review
        cut.WaitForAssertion(() =>
        {
            var wrapper = cut.Find(".textarea-wrapper");
            Assert.Contains("ai-generated", wrapper.GetAttribute("class"));
        });
    }

    [Fact]
    public void No_field_found_does_nothing()
    {
        SetupSnapshot(new AiFieldSnapshot { Found = false });
        var client = new ScriptedChatClient("never");
        var cut = RenderField(Agent(client));

        cut.Find(".ai-field-trigger button").Click();

        Assert.Empty(cut.FindAll(".ai-field-review"));
        Assert.DoesNotContain(_module.Invocations, i => i.Identifier == "write");
    }

    [Fact]
    public void Context_parameter_flows_into_the_prompt()
    {
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "", SelStart = 0, SelEnd = 0 });
        var client = new ScriptedChatClient("Subject");
        var cut = RenderField(Agent(client), "Write a subject",
            ps => ps.Add(p => p.Context, "Mail body: quarterly numbers"));

        cut.Find(".ai-field-trigger button").Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".ai-field-review")));
        Assert.Contains("Mail body: quarterly numbers", client.LastUserMessage);
    }

    // ── Task 5: accept / reject ─────────────────────────────────────────────

    private async Task<IRenderedComponent<DrylAiField>> RenderInReviewAsync(
        Action<ComponentParameterCollectionBuilder<DrylAiField>>? extra = null,
        string fieldValueAtAccept = "Dear team, please send the report.")
    {
        SetupSnapshot(new AiFieldSnapshot
        {
            Found = true, Value = fieldValueAtAccept, SelStart = 0, SelEnd = 0,
        });
        var cut = RenderField(Agent(new ScriptedChatClient("Dear team, please send the report.")), extra: extra);
        await cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".ai-field-review")));
        return cut;
    }

    [Fact]
    public async Task Accept_keeps_value_fires_OnAccepted_and_settles()
    {
        string? accepted = null;
        var cut = await RenderInReviewAsync(ps =>
            ps.Add(p => p.OnAccepted, (string v) => accepted = v));

        await cut.InvokeAsync(() => cut.FindAll(".ai-field-review button")[0].Click());

        // Phase left Review: the root drops ai-field--on and the chips play their
        // exit animation. DrylPresence keeps them mounted with presence-exit here,
        // because Loose JSInterop never fires the exit-finished callback (see
        // DrylPresenceTests.Hiding_keeps_child_mounted_with_exit_class).
        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("ai-field--on", cut.Find(".ai-field").GetAttribute("class"));
            Assert.Contains("presence-exit", cut.Find(".ai-field-review").ParentElement!.GetAttribute("class"));
        });
        Assert.Equal("Dear team, please send the report.", accepted);
        // accepted value comes from a fresh snapshot (user may have edited during review)
        Assert.True(_module.Invocations.Count(i => i.Identifier == "snapshot") >= 2);
    }

    [Fact]
    public async Task Reject_restores_snapshot_and_fires_OnRejected()
    {
        var rejected = false;
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "original", SelStart = 0, SelEnd = 0 });
        var cut = RenderField(Agent(new ScriptedChatClient("replacement")),
            extra: ps => ps.Add(p => p.OnRejected, () => rejected = true));
        await cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".ai-field-review")));

        await cut.InvokeAsync(() => cut.FindAll(".ai-field-review button")[1].Click());

        // Phase left Review (chips stay mounted with presence-exit under Loose JSInterop).
        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("ai-field--on", cut.Find(".ai-field").GetAttribute("class"));
            Assert.Contains("presence-exit", cut.Find(".ai-field-review").ParentElement!.GetAttribute("class"));
        });
        Assert.True(rejected);
        var writes = _module.Invocations.Where(i => i.Identifier == "write").ToList();
        Assert.Equal("original", writes[^1].Arguments[1]);   // last write restored the snapshot
    }

    [Fact]
    public async Task Escape_in_review_rejects()
    {
        var rejected = false;
        var cut = await RenderInReviewAsync(ps =>
            ps.Add(p => p.OnRejected, () => rejected = true));

        await cut.InvokeAsync(() => cut.Find(".ai-field").KeyDown(new KeyboardEventArgs { Key = "Escape" }));

        // Phase left Review (chips stay mounted with presence-exit under Loose JSInterop).
        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("ai-field--on", cut.Find(".ai-field").GetAttribute("class"));
            Assert.Contains("presence-exit", cut.Find(".ai-field-review").ParentElement!.GetAttribute("class"));
        });
        Assert.True(rejected);
    }

    // ── Task 6: cancel + error ──────────────────────────────────────────────

    /// <summary>Streams one chunk, then hangs until cancelled — lets tests cancel mid-stream.</summary>
    private sealed class HangingChatClient : IChatClient
    {
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "partial ");
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }

    /// <summary>Fails after one chunk — exercises the run-error path.</summary>
    private sealed class FailingChatClient : IChatClient
    {
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "par");
            await Task.Yield();
            throw new InvalidOperationException("model unavailable");
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }

    [Fact]
    public async Task Escape_during_run_cancels_and_restores()
    {
        var rejected = false;
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "before", SelStart = 0, SelEnd = 0 });
        var cut = RenderField(Agent(new HangingChatClient()),
            extra: ps => ps.Add(p => p.OnRejected, () => rejected = true));

        await cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());
        // wait until the stream produced at least one write (running for sure)
        cut.WaitForAssertion(() =>
            Assert.Contains(_module.Invocations, i => i.Identifier == "write"));

        await cut.InvokeAsync(() => cut.Find(".ai-field").KeyDown(new KeyboardEventArgs { Key = "Escape" }));

        cut.WaitForAssertion(() =>
        {
            var writes = _module.Invocations.Where(i => i.Identifier == "write").ToList();
            Assert.Equal("before", writes[^1].Arguments[1]);
        });
        Assert.True(rejected);
        // Review chips never mounted in this run — no cancel-only path enters Review.
        Assert.Empty(cut.FindAll(".ai-field-review"));
    }

    [Fact]
    public async Task Run_error_restores_shows_message_and_fires_OnError()
    {
        DrylRunError? error = null;
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "before", SelStart = 0, SelEnd = 0 });
        var cut = RenderField(Agent(new FailingChatClient()),
            extra: ps => ps.Add(p => p.OnError, (DrylRunError e) => error = e));

        await cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());

        // .ai-field-error is DrylPresence-wrapped and Appear, so it's actually mounted when showing.
        // FailAsync restores (which drives the render) BEFORE invoking OnError — same
        // restore-then-notify order as Reject/cancel — so wait for both together rather than
        // assuming OnError has already landed the instant the DOM shows the error.
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".ai-field-error"));
            Assert.NotNull(error);
        });
        Assert.Contains("model unavailable", cut.Find(".ai-field-error").TextContent);
        // Review chips never mounted — the run failed before reaching Review.
        Assert.Empty(cut.FindAll(".ai-field-review"));

        var writes = _module.Invocations.Where(i => i.Identifier == "write").ToList();
        Assert.Equal("before", writes[^1].Arguments[1]);   // restored
    }

    [Fact]
    public async Task New_run_clears_previous_error()
    {
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "", SelStart = 0, SelEnd = 0 });
        var cut = RenderField(Agent(new FailingChatClient()));
        await cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".ai-field-error")));

        // second click starts a new run — StartAsync clears _errorText at the top, so the error's
        // DrylPresence wrapper immediately starts its exit transition (Loose JSInterop never fires
        // the exit-finished callback, so it stays mounted with presence-exit rather than unmounting
        // — see the Task 5 accept/reject tests for the established pattern).
        await cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());
        cut.WaitForAssertion(() =>
            Assert.Contains("presence-exit", cut.Find(".ai-field-error").ParentElement!.GetAttribute("class")));
    }

    // ── Task 7: mini-prompt popover ─────────────────────────────────────────

    [Fact]
    public async Task Without_instruction_trigger_opens_prompt_instead_of_running()
    {
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "", SelStart = 0, SelEnd = 0 });
        var client = new ScriptedChatClient("res");
        var cut = RenderField(Agent(client), instruction: null);

        await cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());

        Assert.Single(cut.FindAll(".ai-field-prompt"));
        Assert.Null(client.LastUserMessage);   // nothing ran yet
    }

    [Fact]
    public async Task Prompt_enter_starts_run_with_typed_instruction()
    {
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "", SelStart = 0, SelEnd = 0 });
        var client = new ScriptedChatClient("Guten Tag");
        var cut = RenderField(Agent(client), instruction: null);
        await cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());

        await cut.InvokeAsync(() => cut.Find(".ai-field-prompt input").Input("Übersetze auf Deutsch"));
        await cut.InvokeAsync(() => cut.Find(".ai-field-prompt")
            .KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".ai-field-review")));
        Assert.StartsWith("Übersetze auf Deutsch", client.LastUserMessage);
        Assert.Empty(cut.FindAll(".ai-field-prompt"));   // popover closed on submit
    }

    [Fact]
    public async Task Empty_prompt_does_not_start()
    {
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "", SelStart = 0, SelEnd = 0 });
        var client = new ScriptedChatClient("res");
        var cut = RenderField(Agent(client), instruction: null);
        await cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());

        await cut.InvokeAsync(() => cut.Find(".ai-field-prompt")
            .KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        Assert.Null(client.LastUserMessage);
        Assert.Single(cut.FindAll(".ai-field-prompt"));   // stays open
    }

    [Fact]
    public async Task ShowPrompt_prefills_the_instruction()
    {
        SetupSnapshot(new AiFieldSnapshot { Found = true, Value = "", SelStart = 0, SelEnd = 0 });
        var cut = RenderField(Agent(new ScriptedChatClient("res")), instruction: "Kürzen",
            extra: ps => ps.Add(p => p.ShowPrompt, true));

        await cut.InvokeAsync(() => cut.Find(".ai-field-trigger button").Click());

        Assert.Equal("Kürzen", cut.Find(".ai-field-prompt input").GetAttribute("value"));
    }
}
