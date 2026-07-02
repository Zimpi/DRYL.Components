using Bunit;
using DRYL.Components.Agents;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

/// <summary>JSInterop is Loose because DrylPresence wires dryl.motion.onExit.</summary>
public class DrylAgentUsageTests : BunitContext
{
    public DrylAgentUsageTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Renders_nothing_without_usage_data()
    {
        var run = new DrylAgentRun();
        var cut = Render<DrylAgentUsage>(p => p.Add(x => x.Run, run));

        Assert.Empty(cut.FindAll(".badge"));
    }

    [Fact]
    public void Shows_compact_invariant_badges_once_usage_arrives()
    {
        var run = new DrylAgentRun();
        run.AddUsage(new UsageDetails
        {
            InputTokenCount = 1234, OutputTokenCount = 890, TotalTokenCount = 2124,
        });

        var cut = Render<DrylAgentUsage>(p => p.Add(x => x.Run, run));

        Assert.Equal(3, cut.FindAll(".badge").Count);
        Assert.Contains("1.2k", cut.Markup);    // invariant decimal point, German locale or not
        Assert.Contains("890", cut.Markup);
        Assert.Contains("2.1k", cut.Markup);
    }

    [Fact]
    public void Omits_badges_the_provider_never_reported()
    {
        var run = new DrylAgentRun();
        run.AddUsage(new UsageDetails { OutputTokenCount = 42 });

        var cut = Render<DrylAgentUsage>(p => p.Add(x => x.Run, run));

        Assert.Single(cut.FindAll(".badge"));
        Assert.Contains("Completion", cut.Markup);
        Assert.DoesNotContain("Prompt", cut.Markup);
    }

    [Fact]
    public void Updates_when_more_usage_streams_in()
    {
        var run = new DrylAgentRun();
        run.AddUsage(new UsageDetails { TotalTokenCount = 100 });
        var cut = Render<DrylAgentUsage>(p => p.Add(x => x.Run, run));
        Assert.Contains("100", cut.Markup);

        run.AddUsage(new UsageDetails { TotalTokenCount = 1000 });

        cut.WaitForAssertion(() => Assert.Contains("1.1k", cut.Markup));
    }
}
