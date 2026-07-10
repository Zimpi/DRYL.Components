using System.Runtime.CompilerServices;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
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

        public ScriptedChatClient(params string[] chunks) => _chunks = chunks;

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
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
}
