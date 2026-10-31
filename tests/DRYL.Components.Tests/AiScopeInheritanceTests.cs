using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// I32: <c>DrylAiScope</c> promises its descendants the scope's <em>state</em>, not only its
/// aura variant. Many components resolved <c>Aura</c> through the scope and handed the raw
/// <c>Ai</c> to the aura — inside a scope they stayed dark. A source scan keeps the door
/// shut for new components, and render probes prove the state arrives.
/// </summary>
public class AiScopeInheritanceTests : BunitContext
{
    // Every file that resolves the aura variant through the scope but not the state, and why.
    // A new entry needs a reason a reviewer accepts — the default is to inherit both.
    private static readonly Dictionary<string, string> StateNotInherited = new()
    {
        ["DrylToolCall.razor"] = "a tool call's status is a fact about that call (spec)",
        ["DrylToolCallGroup.razor"] = "the group's status is a fact about its calls (spec)",
        ["DrylCommandPalette.razor"] = "Ai mounts the AI result panel; inheriting would open it empty (spec)",
        ["DrylDialog.razor"] = "state comes from DialogOptions.Ai; dialogs render in the provider at layout root, outside page scopes",
        ["DrylNotifications.razor"] = "the host sits at layout root; a page-wide scope must not light the toast stack (spec)",
        // I32 follow-up: no spec yet, so SPEC-01 forbids the behaviour change until one is written.
        ["DrylChipInput.razor"] = "I32 follow-up — spec first",
        ["DrylDatePicker.razor"] = "I32 follow-up — spec first",
        ["DrylTimePicker.razor"] = "I32 follow-up — spec first",
        ["DrylInputMask.razor"] = "I32 follow-up — spec first",
        ["DrylInputNumber.razor"] = "I32 follow-up — spec first",
        ["DrylInputOtp.razor"] = "I32 follow-up — spec first",
        ["DrylInputPassword.razor"] = "I32 follow-up — spec first",
        ["DrylMultiSelect.razor"] = "I32 follow-up — spec first",
        ["DrylRadioGroup.razor"] = "I32 follow-up — spec first",
        ["DrylRating.razor"] = "I32 follow-up — spec first",
        ["DrylSlider.razor"] = "I32 follow-up — spec first",
        ["DrylExpansion.razor"] = "I32 follow-up — spec first",
        ["DrylChatComposer.razor"] = "I32 follow-up — spec first",
    };

    private static string CoreDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DRYL.slnx"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "code", "DRYL.Components");
    }

    [Fact]
    public void Every_component_that_inherits_the_aura_inherits_the_state_or_is_accounted_for()
    {
        var unaccounted = new List<string>();
        var stale = new List<string>();
        var scanned = 0;
        foreach (var file in Directory.EnumerateFiles(CoreDir(), "*.razor", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))
        {
            var src = File.ReadAllText(file);
            if (!src.Contains("AiScope.ResolveAura(")) continue;
            scanned++;

            var name = Path.GetFileName(file);
            var inheritsState = src.Contains("AiScope.Resolve(Ai,");
            if (!inheritsState && !StateNotInherited.ContainsKey(name)) unaccounted.Add(name);
            if (inheritsState && StateNotInherited.ContainsKey(name)) stale.Add(name);
        }

        Assert.True(scanned >= 30, $"only {scanned} scope-aware files found");
        Assert.Empty(unaccounted);
        Assert.Empty(stale);   // fixed — take it off the list
    }

    // ── Render probes ────────────────────────────────────────────────

    public static TheoryData<Type> Inheritors => new()
    {
        typeof(DrylStat), typeof(DrylCodeBlock), typeof(DrylTimelineItem), typeof(DrylAlert),
        typeof(DrylEmptyState), typeof(DrylProgress), typeof(DrylSkeleton), typeof(DrylSpinner),
        typeof(DrylFileUpload), typeof(DrylImage), typeof(DrylTable<string>),
    };

    [Theory]
    [MemberData(nameof(Inheritors))]
    public void A_component_with_no_Ai_shows_the_scopes_state(Type type)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render(InScope(AiState.Streaming, Host(type, null)));

        Assert.NotEmpty(cut.FindAll(".ai-streaming"));
    }

    [Theory]
    [MemberData(nameof(Inheritors))]
    public void An_explicit_Ai_wins_over_the_scope(Type type)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render(InScope(AiState.Streaming, Host(type, AiState.Thinking)));

        // Only the host's own surface: a nested DrylButton (a copy button, say) still
        // inherits the scope, as it should — it set no Ai of its own.
        var hosts = cut.FindAll(".ai-thinking");
        Assert.NotEmpty(hosts);
        Assert.All(hosts, h => Assert.DoesNotContain("ai-streaming", h.ClassList));
    }

    [Theory]
    [MemberData(nameof(Inheritors))]
    public void A_component_outside_a_scope_stays_plain(Type type)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render(Host(type, null));

        Assert.Empty(cut.FindAll(".ai-aura"));
    }

    [Fact]
    public void An_inherited_move_to_Generated_plays_the_bloom()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<ScopeProbe>(ps => ps.Add(p => p.State, AiState.Streaming));
        Assert.Empty(cut.FindAll(".ai-generated"));

        cut.Render(ps => ps.Add(p => p.State, AiState.Generated));

        Assert.NotEmpty(cut.FindAll(".ai-generated"));
    }

    [Fact]
    public void A_step_inside_a_scope_lights_its_header()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylAiScope>(ps => ps
            .Add(p => p.State, AiState.Thinking)
            .AddChildContent<DrylStepper>(sp => sp
                .AddChildContent<DrylStep>(st => st.Add(p => p.Label, "Draft"))));

        Assert.NotEmpty(cut.FindAll(".ai-thinking"));
    }

    [Fact]
    public void A_tool_call_keeps_its_own_state_inside_a_scope()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render(InScope(AiState.Streaming, b =>
        {
            b.OpenComponent<DrylToolCall>(0);
            b.AddAttribute(1, "ToolName", "send_email");
            b.CloseComponent();
        }));

        Assert.Empty(cut.FindAll(".ai-streaming"));
    }

    [Fact]
    public void A_command_palette_does_not_open_its_AI_panel_from_a_scope()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render(InScope(AiState.Streaming, b =>
        {
            b.OpenComponent<DrylCommandPalette>(0);
            b.AddAttribute(1, "Open", true);
            b.CloseComponent();
        }));

        Assert.Empty(cut.FindAll(".cmd-ai-wrap"));
    }

    private sealed class ScopeProbe : ComponentBase
    {
        [Parameter] public AiState State { get; set; }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b) =>
            InScope(State, Host(typeof(DrylStat), null))(b);
    }

    private static RenderFragment InScope(AiState state, RenderFragment child) => b =>
    {
        b.OpenComponent<DrylAiScope>(0);
        b.AddAttribute(1, "State", (AiState?)state);
        b.AddAttribute(2, "ChildContent", child);
        b.CloseComponent();
    };

    private static RenderFragment Host(Type type, AiState? ai) => b =>
    {
        b.OpenComponent(0, type);
        if (ai is not null) b.AddAttribute(1, "Ai", ai.Value);
        if (type == typeof(DrylCodeBlock)) b.AddAttribute(2, "Code", "var x = 1;");
        if (type == typeof(DrylStat)) { b.AddAttribute(2, "Label", "Revenue"); b.AddAttribute(3, "Value", "42"); }
        if (type == typeof(DrylTimelineItem)) b.AddAttribute(2, "Title", "Step");
        if (type == typeof(DrylEmptyState)) b.AddAttribute(2, "Title", "Nothing yet");
        if (type == typeof(DrylProgress)) b.AddAttribute(2, "Value", 40d);
        if (type == typeof(DrylImage)) b.AddAttribute(2, "Src", "a.png");
        if (type == typeof(DrylTable<string>)) b.AddAttribute(2, "Items", new[] { "a", "b" });
        b.CloseComponent();
    };
}
