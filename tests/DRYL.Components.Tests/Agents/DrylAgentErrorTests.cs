using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

/// <summary>JSInterop is Loose because DrylPresence wires dryl.motion.onExit.</summary>
public class DrylAgentErrorTests : BunitContext
{
    public DrylAgentErrorTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private static DrylAgentRun FailedRun(string message)
    {
        var run = new DrylAgentRun
        {
            Error = new DrylRunError(message, new InvalidOperationException(message)),
            State = AiState.None,
        };
        return run;
    }

    [Fact]
    public void Renders_nothing_while_the_run_is_healthy()
    {
        var run = new DrylAgentRun();
        var cut = Render<DrylAgentError>(p => p.Add(x => x.Run, run));

        Assert.Empty(cut.FindAll(".alert"));
    }

    [Fact]
    public void Shows_danger_alert_with_message_and_exception_type_on_error()
    {
        var cut = Render<DrylAgentError>(p => p.Add(x => x.Run, FailedRun("model exploded")));

        var alert = cut.Find(".alert");
        Assert.Contains("danger", alert.ClassList);
        Assert.Contains("model exploded", cut.Markup);
        Assert.Contains(nameof(InvalidOperationException), cut.Markup);
    }

    [Fact]
    public void Appears_when_the_run_errors_mid_flight()
    {
        var run = new DrylAgentRun();
        var cut = Render<DrylAgentError>(p => p.Add(x => x.Run, run));
        Assert.Empty(cut.FindAll(".alert"));

        run.Error = new DrylRunError("late failure");
        run.State = AiState.None;
        run.Raise();

        cut.WaitForAssertion(() => Assert.Contains("late failure", cut.Markup));
    }

    [Fact]
    public void Retry_button_only_renders_with_a_callback_and_invokes_it()
    {
        var withoutRetry = Render<DrylAgentError>(p => p.Add(x => x.Run, FailedRun("x")));
        Assert.Empty(withoutRetry.FindAll("button"));

        var retried = false;
        var cut = Render<DrylAgentError>(p => p
            .Add(x => x.Run, FailedRun("x"))
            .Add(x => x.OnRetry, () => retried = true));

        cut.Find("button").Click();
        Assert.True(retried);
    }
}
