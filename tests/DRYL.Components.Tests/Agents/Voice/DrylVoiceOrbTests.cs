using Bunit;
using DRYL.Components.Agents;
using Xunit;

namespace DRYL.Components.Tests.Agents.Voice;

/// <summary>The orb is the voice made visible — and it says it in the shared AI language,
/// not in one it invented for itself.</summary>
public class DrylVoiceOrbTests : BunitContext
{
    public DrylVoiceOrbTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static DrylVoiceRun LiveRun(VoiceActivity activity)
    {
        var run = new DrylVoiceRunner(new NoopJsRuntime())
            .Create(new DrylVoiceOptions { ApiKey = "sk-test" });
        run.OnConnected();
        run.OnActivity(activity.ToString());
        return run;
    }

    [Fact]
    public void It_wears_the_shared_aura_primitives()
    {
        var cut = Render<DrylVoiceOrb>(p => p.Add(x => x.Run, LiveRun(VoiceActivity.Listening)));

        var orb = cut.Find(".voice-orb");
        Assert.Contains("ai-aura", orb.ClassList);
        Assert.NotNull(cut.Find(".voice-orb .ai-aura-ring"));
        Assert.NotNull(cut.Find(".voice-orb .ai-aura-comet"));
        Assert.NotNull(cut.Find(".voice-orb .ai-aura-glow"));
    }

    [Fact]
    public void Speaking_streams_and_thinking_thinks()
    {
        Assert.Contains(
            "ai-streaming",
            Render<DrylVoiceOrb>(p => p.Add(x => x.Run, LiveRun(VoiceActivity.Speaking)))
                .Find(".voice-orb").ClassList);

        Assert.Contains(
            "ai-thinking",
            Render<DrylVoiceOrb>(p => p.Add(x => x.Run, LiveRun(VoiceActivity.Thinking)))
                .Find(".voice-orb").ClassList);
    }

    [Fact]
    public void An_idle_run_wears_no_state_class_at_all()
    {
        var idle = new DrylVoiceRunner(new NoopJsRuntime())
            .Create(new DrylVoiceOptions { ApiKey = "sk-test" });

        var classes = Render<DrylVoiceOrb>(p => p.Add(x => x.Run, idle)).Find(".voice-orb").ClassList;

        Assert.DoesNotContain("ai-thinking", classes);
        Assert.DoesNotContain("ai-streaming", classes);
    }

    [Fact]
    public void The_size_arrives_as_an_invariant_css_variable()
    {
        // German locale turns 96 into 96 but a future fractional size into "96,5px", which no
        // browser parses — the invariant formatting is the guard, so it gets a test.
        var cut = Render<DrylVoiceOrb>(p => p
            .Add(x => x.Run, LiveRun(VoiceActivity.Listening))
            .Add(x => x.Size, 128));

        Assert.Contains("--orb-size: 128px", cut.Find(".voice-orb").GetAttribute("style"));
    }

    [Fact]
    public void It_is_decorative_and_stays_out_of_the_accessibility_tree()
    {
        // The spoken state is announced by the dock's aria-live status line. An orb that
        // announced itself as well would say everything twice.
        var cut = Render<DrylVoiceOrb>(p => p.Add(x => x.Run, LiveRun(VoiceActivity.Listening)));

        Assert.Equal("true", cut.Find(".voice-orb").GetAttribute("aria-hidden"));
    }
}
