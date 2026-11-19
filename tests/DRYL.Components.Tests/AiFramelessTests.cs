using System.Globalization;
using Bunit;
using DRYL.Components;
using DRYL.Components.Ai;

namespace DRYL.Components.Tests;

/// <summary>
/// AI-08 (I45): a component without a surface of its own draws no aura frame — its marks carry
/// the AI state. These tests guard the markup half of that: the state classes stay on the
/// wrapper, the ring/comet/glow are gone, and the marks that replaced them are rendered.
/// What the CSS does with those classes is checked by eye.
/// </summary>
public class AiFramelessTests : BunitContext
{
    private const string Description = "AI suggestion, awaiting your decision";
    private const string Frame = ".ai-aura-ring, .ai-aura-comet, .ai-aura-glow, .ai-aura-wash";

    public AiFramelessTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    // ── DrylAuraElements.Frameless ───────────────────────────────────────

    [Fact]
    public void Frameless_aura_elements_render_no_layer()
    {
        var aura = new AuraLifecycle();
        aura.Sync(AiState.Thinking, () => Task.CompletedTask);

        var cut = Render<DrylAuraElements>(ps => ps.Add(p => p.Aura, aura).Add(p => p.Frameless, true));

        Assert.Empty(cut.FindAll(Frame));
    }

    [Fact]
    public void Frameless_aura_elements_keep_the_proposal_description()
    {
        var aura = new AuraLifecycle();
        aura.Sync(AiState.Proposed, () => Task.CompletedTask);

        var cut = Render<DrylAuraElements>(ps => ps.Add(p => p.Aura, aura).Add(p => p.Frameless, true));

        Assert.Empty(cut.FindAll(Frame));
        Assert.Contains(Description, cut.Markup);
    }

    // ── Progress, skeleton, spinner ──────────────────────────────────────

    [Theory]
    [InlineData(AiState.Active, "ai-active")]
    [InlineData(AiState.Thinking, "ai-thinking")]
    [InlineData(AiState.Streaming, "ai-streaming")]
    [InlineData(AiState.Generated, "ai-generated")]
    public void Progress_carries_the_state_class_and_no_frame(AiState state, string cls)
    {
        var cut = Render<DrylProgress>(ps => ps.Add(p => p.Value, 42).Add(p => p.Ai, state));

        Assert.Single(cut.FindAll($".progress-wrap.ai-aura.{cls}"));
        Assert.Empty(cut.FindAll(Frame));
    }

    [Fact]
    public void Skeleton_draws_no_frame()
    {
        var cut = Render<DrylSkeleton>(ps => ps.Add(p => p.Ai, AiState.Thinking));

        Assert.Single(cut.FindAll(".skel-wrap.ai-aura.ai-thinking"));
        Assert.Empty(cut.FindAll(Frame));
    }

    [Fact]
    public void Spinner_draws_no_frame()
    {
        var cut = Render<DrylSpinner>(ps => ps.Add(p => p.Ai, AiState.Streaming));

        Assert.Single(cut.FindAll(".spinner-wrap.ai-aura.ai-streaming"));
        Assert.Empty(cut.FindAll(Frame));
    }

    // ── Rating ───────────────────────────────────────────────────────────

    [Fact]
    public void Rating_draws_no_frame()
    {
        var cut = Render<DrylRating>(ps => ps.Add(p => p.Value, 3).Add(p => p.Ai, AiState.Thinking));

        Assert.Single(cut.FindAll(".rating-wrapper.ai-aura.ai-thinking"));
        Assert.Empty(cut.FindAll(Frame));
    }

    [Fact]
    public void Rating_describes_a_proposal()
    {
        var cut = Render<DrylRating>(ps => ps.Add(p => p.Value, 3).Add(p => p.Ai, AiState.Proposed));

        Assert.Single(cut.FindAll(".rating-wrapper.ai-aura.ai-proposed"));
        Assert.Contains(Description, cut.Markup);
    }

    // ── Slider ───────────────────────────────────────────────────────────

    [Fact]
    public void Slider_puts_the_mark_ring_on_the_knob_and_draws_no_frame()
    {
        var cut = Render<DrylSlider>(ps => ps.Add(p => p.Value, 72).Add(p => p.Ai, AiState.Thinking));

        Assert.Single(cut.FindAll(".slider-wrap.ai-aura .slider-ai-ring.ai-mark-ring"));
        Assert.Equal("true", cut.Find(".slider-ai-ring").GetAttribute("aria-hidden"));
        Assert.Empty(cut.FindAll(Frame));
    }

    [Fact]
    public void Slider_renders_no_ring_without_an_AI_state()
    {
        var cut = Render<DrylSlider>(ps => ps.Add(p => p.Value, 72));

        Assert.Empty(cut.FindAll(".ai-mark-ring, .slider-ai-sheen"));
    }

    [Fact]
    public void Slider_writes_the_knob_position_as_a_css_number_under_a_comma_culture()
    {
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var cut = Render<DrylSlider>(ps => ps.Add(p => p.Value, 72.5));

            Assert.Contains("--slider-frac: 0.7250", cut.Find(".slider-wrap").GetAttribute("style"));
        }
        finally { CultureInfo.CurrentCulture = prev; }
    }

    [Fact]
    public void Slider_describes_a_proposal()
    {
        var cut = Render<DrylSlider>(ps => ps.Add(p => p.Value, 72).Add(p => p.Ai, AiState.Proposed));

        Assert.Contains(Description, cut.Markup);
    }

    // ── Radio group ──────────────────────────────────────────────────────

    private IRenderedComponent<DrylRadioGroup<string>> Radios(AiState state) =>
        Render<DrylRadioGroup<string>>(ps => ps
            .Add(p => p.Value, "pro")
            .Add(p => p.Ai, state)
            .AddChildContent<DrylRadio<string>>(r => r.Add(p => p.Value, "free").Add(p => p.Label, "Free"))
            .AddChildContent<DrylRadio<string>>(r => r.Add(p => p.Value, "pro").Add(p => p.Label, "Pro")));

    [Fact]
    public void Radio_group_rings_only_the_selected_option()
    {
        var cut = Radios(AiState.Thinking);

        var ring = Assert.Single(cut.FindAll(".radio-control > .ai-mark-ring"));
        Assert.Equal("pro", ring.ParentElement!.PreviousElementSibling!.GetAttribute("value"));
        Assert.Equal("true", ring.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Radio_group_draws_no_frame()
    {
        var cut = Radios(AiState.Thinking);

        Assert.Single(cut.FindAll(".radio-group-wrapper.ai-aura.ai-thinking"));
        Assert.Empty(cut.FindAll(Frame));
    }

    [Fact]
    public void Radio_group_renders_no_ring_without_an_AI_state()
    {
        var cut = Radios(AiState.None);

        Assert.Empty(cut.FindAll(".ai-mark-ring"));
    }

    [Fact]
    public void Radio_group_describes_a_proposal()
    {
        var cut = Radios(AiState.Proposed);

        Assert.Single(cut.FindAll(".radio-control > .ai-mark-ring"));
        Assert.Contains(Description, cut.Markup);
    }

    // ── Stepper ──────────────────────────────────────────────────────────

    [Fact]
    public void Step_aura_sits_on_the_indicator_and_not_on_the_header()
    {
        var cut = Render<DrylStepper>(ps => ps
            .AddChildContent<DrylStep>(s => s.Add(p => p.Id, "a").Add(p => p.Label, "Load"))
            .AddChildContent<DrylStep>(s => s.Add(p => p.Id, "b").Add(p => p.Label, "Analyse").Add(p => p.Ai, AiState.Thinking)));

        var host = Assert.Single(cut.FindAll(".step-indicator.ai-aura.ai-thinking"));
        Assert.NotNull(host.QuerySelector(".ai-aura-ring"));
        Assert.Empty(cut.FindAll(".step-header.ai-aura"));
    }

    // ── OTP ──────────────────────────────────────────────────────────────

    [Fact]
    public void Otp_boxes_carry_their_index_for_the_walking_light()
    {
        var cut = Render<DrylInputOtp>(ps => ps.Add(p => p.Digits, 4).Add(p => p.Ai, AiState.Thinking));

        var wraps = cut.FindAll(".otp-box-wrap.ai-aura");
        Assert.Equal(4, wraps.Count);
        Assert.Contains("--otp-i: 2", wraps[2].GetAttribute("style"));
        Assert.Contains("--otp-n: 4", wraps[2].GetAttribute("style"));
        Assert.All(wraps, w => Assert.NotNull(w.QuerySelector(".ai-aura-ring")));
    }
}
