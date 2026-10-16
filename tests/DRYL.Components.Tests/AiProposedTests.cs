using Bunit;
using DRYL.Components;
using DRYL.Components.Ai;

namespace DRYL.Components.Tests;

/// <summary>
/// <see cref="AiState.Proposed"/> in the shared aura (I20): the lifecycle keeps it, the class
/// helper names it and the hand-over to <see cref="AiState.Generated"/>, and
/// <see cref="DrylAuraElements"/> renders the description and the accept wave exactly when
/// <c>specs/E3 AI/F7 DrylAuraElements.md</c> says so.
/// </summary>
public class AiProposedTests : BunitContext
{
    private static readonly Func<Task> NoRender = () => Task.CompletedTask;

    private static AuraLifecycle Walk(params AiState[] states)
    {
        var aura = new AuraLifecycle();
        foreach (var s in states) aura.Sync(s, NoRender);
        return aura;
    }

    private static List<string> Classes(AuraLifecycle aura, AiAura variant = AiAura.Comet)
    {
        var classes = new List<string>();
        AiAuraCss.Append(classes, aura, variant);
        return classes;
    }

    // ── The enum ──────────────────────────────────────────────────────────

    [Fact]
    public void Proposed_is_appended_after_Generated_so_older_values_keep_their_numbers()
    {
        Assert.Equal(new[] { "None", "Active", "Thinking", "Streaming", "Generated", "Proposed" },
            Enum.GetNames<AiState>());
        Assert.Equal(5, (int)AiState.Proposed);
    }

    // ── AuraLifecycle ─────────────────────────────────────────────────────

    [Fact]
    public async Task Proposed_rests_and_never_retires_by_itself()
    {
        var aura = new AuraLifecycle(exitMs: 10, generatedHoldMs: 10);
        aura.Sync(AiState.Proposed, NoRender);

        await Task.Delay(80);

        Assert.Equal(AiState.Proposed, aura.RenderState);
        Assert.False(aura.Exiting);
        Assert.True(aura.Present);
    }

    [Fact]
    public void Generated_straight_from_Proposed_is_marked_as_an_accept()
    {
        var aura = Walk(AiState.Proposed, AiState.Generated);

        Assert.Equal(AiState.Generated, aura.RenderState);
        Assert.True(aura.FromProposed);
    }

    [Theory]
    [InlineData(AiState.None)]
    [InlineData(AiState.Active)]
    [InlineData(AiState.Thinking)]
    [InlineData(AiState.Streaming)]
    public void Generated_from_any_other_state_is_not_an_accept(AiState before)
    {
        var aura = Walk(before, AiState.Generated);

        Assert.False(aura.FromProposed);
    }

    [Fact]
    public void A_proposal_dismissed_then_generated_is_not_an_accept()
    {
        var aura = Walk(AiState.Proposed, AiState.None, AiState.Generated);

        Assert.False(aura.FromProposed);
    }

    [Fact]
    public void The_accept_mark_clears_on_the_next_live_state()
    {
        var aura = Walk(AiState.Proposed, AiState.Generated, AiState.Thinking);

        Assert.False(aura.FromProposed);
    }

    [Fact]
    public async Task The_accept_mark_goes_when_the_accepted_Generated_retires()
    {
        var aura = new AuraLifecycle(exitMs: 10, generatedHoldMs: 10);
        aura.Sync(AiState.Proposed, NoRender);
        aura.Sync(AiState.Generated, NoRender);

        for (var i = 0; i < 100 && aura.RenderState != AiState.None; i++) await Task.Delay(10);

        Assert.Equal(AiState.None, aura.RenderState);
        Assert.False(aura.FromProposed);
    }

    [Fact]
    public void A_tool_call_awaiting_a_decision_reads_awaiting_approval()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylToolCall>(ps => ps.Add(p => p.ToolName, "send_email").Add(p => p.Ai, AiState.Proposed));

        Assert.Contains("Awaiting approval", cut.Find(".ai-indicator").TextContent);
    }

    [Fact]
    public void Proposed_dismissed_to_None_dissolves_through_the_exit()
    {
        var aura = Walk(AiState.Proposed, AiState.None);

        Assert.True(aura.Exiting);
        Assert.Equal(AiState.Proposed, aura.RenderState);
    }

    // ── AiAuraCss ─────────────────────────────────────────────────────────

    [Fact]
    public void Proposed_adds_its_state_class()
    {
        Assert.Equal(new[] { "ai-aura", "ai-proposed" }, Classes(Walk(AiState.Proposed)));
    }

    [Fact]
    public void Proposed_keeps_the_variant_class()
    {
        Assert.Contains("ai-aura--aurora", Classes(Walk(AiState.Proposed), AiAura.Aurora));
    }

    [Fact]
    public void An_accept_adds_the_from_proposal_marker_beside_ai_generated()
    {
        var classes = Classes(Walk(AiState.Proposed, AiState.Generated));

        Assert.Contains("ai-generated", classes);
        Assert.Contains("ai-from-proposed", classes);
        Assert.DoesNotContain("ai-proposed", classes);
    }

    [Fact]
    public void A_plain_Generated_carries_no_from_proposal_marker()
    {
        Assert.DoesNotContain("ai-from-proposed", Classes(Walk(AiState.Streaming, AiState.Generated)));
    }

    // ── DrylAuraElements ─────────────────────────────────────────────────

    private IRenderedComponent<DrylAuraElements> Elements(AuraLifecycle aura, int genTick = 1) =>
        Render<DrylAuraElements>(ps => ps.Add(p => p.Aura, aura).Add(p => p.GenTick, genTick));

    [Fact]
    public void Proposed_renders_the_description_for_assistive_technology()
    {
        var cut = Elements(Walk(AiState.Proposed));

        var text = cut.Find(".visually-hidden");
        Assert.Equal("AI suggestion, awaiting your decision", text.TextContent);
        Assert.Null(text.GetAttribute("aria-hidden"));
        Assert.Null(text.GetAttribute("aria-live"));
        Assert.Null(text.GetAttribute("role"));
    }

    [Theory]
    [InlineData(AiState.Active)]
    [InlineData(AiState.Thinking)]
    [InlineData(AiState.Streaming)]
    [InlineData(AiState.Generated)]
    public void No_other_state_renders_the_description(AiState state)
    {
        var cut = Elements(Walk(state));

        Assert.Empty(cut.FindAll(".visually-hidden"));
    }

    [Fact]
    public void A_dismissed_proposal_drops_the_description_while_it_dissolves()
    {
        var cut = Elements(Walk(AiState.Proposed, AiState.None));

        Assert.Empty(cut.FindAll(".visually-hidden"));
        Assert.NotEmpty(cut.FindAll(".ai-aura-ring"));
    }

    [Fact]
    public void Proposed_renders_no_wave_and_no_wash()
    {
        var cut = Elements(Walk(AiState.Proposed));

        Assert.Empty(cut.FindAll(".ai-aura-wave"));
        Assert.Empty(cut.FindAll(".ai-aura-wash"));
    }

    [Fact]
    public void An_accept_renders_the_wave_beside_the_wash()
    {
        var cut = Elements(Walk(AiState.Proposed, AiState.Generated));

        var wave = cut.Find(".ai-aura-wave");
        Assert.Equal("true", wave.GetAttribute("aria-hidden"));
        Assert.Empty(wave.Children);
        Assert.Single(cut.FindAll(".ai-aura-wash"));
    }

    [Fact]
    public void A_plain_Generated_renders_no_wave()
    {
        var cut = Elements(Walk(AiState.Streaming, AiState.Generated));

        Assert.Empty(cut.FindAll(".ai-aura-wave"));
        Assert.Single(cut.FindAll(".ai-aura-wash"));
    }

    [Fact]
    public void Each_accept_replays_its_own_wave()
    {
        var aura = Walk(AiState.Proposed, AiState.Generated);
        var cut = Elements(aura, genTick: 1);
        var first = cut.Find(".ai-aura-wave");

        aura.Sync(AiState.Proposed, NoRender);
        aura.Sync(AiState.Generated, NoRender);
        cut.Render(ps => ps.Add(p => p.Aura, aura).Add(p => p.GenTick, 2));

        Assert.NotSame(first, cut.Find(".ai-aura-wave"));
    }

    // ── DrylAiIndicator ──────────────────────────────────────────────────

    [Fact]
    public void The_indicator_reads_Proposed_for_a_proposal()
    {
        var cut = Render<DrylAiIndicator>(ps => ps.Add(p => p.State, AiState.Proposed));

        Assert.Equal("Proposed", cut.Find(".ai-indicator > span").TextContent);
    }

    [Fact]
    public void The_indicator_carries_the_resting_modifier_for_a_proposal_only()
    {
        var proposed = Render<DrylAiIndicator>(ps => ps.Add(p => p.State, AiState.Proposed));
        var generated = Render<DrylAiIndicator>(ps => ps.Add(p => p.State, AiState.Generated));

        Assert.Contains("is-proposed", proposed.Find(".ai-indicator").ClassList);
        Assert.DoesNotContain("is-proposed", generated.Find(".ai-indicator").ClassList);
    }

    [Fact]
    public void The_indicator_announces_a_host_text_for_proposals()
    {
        var cut = Render<DrylAiIndicator>(ps => ps.Add(p => p.State, AiState.Proposed).AddChildContent("7 suggestions"));

        var pill = cut.Find(".ai-indicator");
        Assert.Equal("polite", pill.GetAttribute("aria-live"));
        Assert.Contains("7 suggestions", pill.TextContent);
    }

    // ── The three that ignore it ─────────────────────────────────────────

    public static TheoryData<Type> Ignorers => new() { typeof(DrylSpinner), typeof(DrylSkeleton), typeof(DrylProgress) };

    [Theory]
    [MemberData(nameof(Ignorers))]
    public void Ignoring_components_render_Proposed_exactly_like_None(Type type)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var proposed = RenderMarkup(type, AiState.Proposed);
        var none = RenderMarkup(type, AiState.None);

        Assert.Equal(none, proposed);
        Assert.DoesNotContain("ai-aura", proposed);
        Assert.DoesNotContain("awaiting your decision", proposed);
    }

    private string RenderMarkup(Type type, AiState state)
    {
        var builder = new Microsoft.AspNetCore.Components.RenderFragment(b =>
        {
            b.OpenComponent(0, type);
            b.AddAttribute(1, "Ai", state);
            b.CloseComponent();
        });
        return Render(builder).Markup;
    }

    // ── Hosts that used to draw their own aura (T4) ──────────────────────

    private const string Description = "AI suggestion, awaiting your decision";

    [Fact]
    public void A_card_shows_the_proposal_and_hands_over_with_the_wave()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylCard>(ps => ps.Add(p => p.Ai, AiState.Proposed).AddChildContent("Plan"));

        Assert.Contains("ai-proposed", cut.Find(".glass-card").ClassList);
        Assert.Equal(Description, cut.Find(".visually-hidden").TextContent);

        cut.Render(ps => ps.Add(p => p.Ai, AiState.Generated));

        Assert.Contains("ai-from-proposed", cut.Find(".glass-card").ClassList);
        Assert.NotNull(cut.Find(".ai-aura-wave"));
    }

    [Fact]
    public void Markdown_shows_the_proposal_and_hands_over_with_the_wave()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylMarkdown>(ps => ps.Add(p => p.Content, "Draft").Add(p => p.Ai, AiState.Proposed));

        Assert.Contains("ai-proposed", cut.Find(".ai-aura").ClassList);
        Assert.Equal(Description, cut.Find(".visually-hidden").TextContent);

        cut.Render(ps => ps.Add(p => p.Ai, AiState.Generated));

        Assert.Contains("ai-from-proposed", cut.Find(".ai-aura").ClassList);
        Assert.NotNull(cut.Find(".ai-aura-wave"));
    }

    [Fact]
    public void An_image_proposal_is_described_and_is_no_live_region()
    {
        var cut = Render<DrylImage>(ps => ps.Add(p => p.Src, "a.png").Add(p => p.Ai, AiState.Proposed));

        var root = cut.Find(".img-wrap");
        Assert.Contains("ai-proposed", root.ClassList);
        Assert.Null(root.GetAttribute("aria-live"));
        Assert.Equal(Description, cut.Find(".visually-hidden").TextContent);
    }

    [Fact]
    public void An_accepted_image_gets_the_wave_but_not_the_scale_in_reveal()
    {
        var cut = Render<DrylImage>(ps => ps.Add(p => p.Src, "a.png").Add(p => p.Ai, AiState.Proposed));

        cut.Render(ps => ps.Add(p => p.Ai, AiState.Generated));

        var root = cut.Find(".img-wrap");
        Assert.Contains("ai-from-proposed", root.ClassList);
        Assert.DoesNotContain("img--generated", root.ClassList);
        Assert.NotNull(cut.Find(".ai-aura-wave"));
    }

    [Fact]
    public void A_plain_generated_image_keeps_its_scale_in_reveal()
    {
        var cut = Render<DrylImage>(ps => ps.Add(p => p.Src, "a.png").Add(p => p.Ai, AiState.Streaming));

        cut.Render(ps => ps.Add(p => p.Ai, AiState.Generated));

        Assert.Contains("img--generated", cut.Find(".img-wrap").ClassList);
        Assert.Empty(cut.FindAll(".ai-aura-wave"));
    }

    [Fact]
    public void A_notification_row_shows_the_proposal_without_a_wave()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var item = new DrylNotification { Title = "Merge 2 contacts?", Timestamp = DateTimeOffset.Now, Ai = AiState.Proposed };
        var cut = Render<DrylNotifications>(ps => ps.Add(p => p.Items, new[] { item }));
        cut.Find(".notif-bell").Click();

        var row = cut.Find(".notif-item");
        Assert.Contains("ai-proposed", row.ClassList);
        Assert.Equal(Description, row.QuerySelector(".visually-hidden")!.TextContent);
        Assert.Empty(cut.FindAll(".ai-aura-wave"));
    }

    [Fact]
    public void A_tab_shows_the_proposal_and_names_it_for_assistive_technology()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylTabs>(ps =>
        {
            ps.AddChildContent<DrylTab>(tp => tp.Add(t => t.Id, "a").Add(t => t.Label, "A"));
            ps.AddChildContent<DrylTab>(tp => tp.Add(t => t.Id, "b").Add(t => t.Label, "B").Add(t => t.Ai, AiState.Proposed));
        });

        var tab = cut.FindAll(".tab")[1];
        Assert.Contains("ai-proposed", tab.ClassList);
        Assert.Contains(Description, tab.TextContent);
        Assert.DoesNotContain(Description, cut.FindAll(".tab")[0].TextContent);
    }

    // ── A host end to end ────────────────────────────────────────────────

    [Fact]
    public void A_shared_aura_host_carries_the_proposal_look_and_description()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylAlert>(ps => ps.Add(p => p.Ai, AiState.Proposed).AddChildContent("Rename 3 files?"));

        Assert.Contains("ai-proposed", cut.Find(".ai-aura").ClassList);
        Assert.Equal("AI suggestion, awaiting your decision", cut.Find(".visually-hidden").TextContent);
    }

    [Fact]
    public void Accepting_on_a_host_hands_over_to_Generated_with_the_wave()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylAlert>(ps => ps.Add(p => p.Ai, AiState.Proposed).AddChildContent("Rename 3 files?"));

        cut.Render(ps => ps.Add(p => p.Ai, AiState.Generated));

        var host = cut.Find(".ai-aura");
        Assert.Contains("ai-generated", host.ClassList);
        Assert.Contains("ai-from-proposed", host.ClassList);
        Assert.NotNull(cut.Find(".ai-aura-wave"));
    }
}
