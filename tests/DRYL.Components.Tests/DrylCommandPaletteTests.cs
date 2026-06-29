using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylCommandPaletteTests : BunitContext
{
    public DrylCommandPaletteTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Declarative_commands_render_as_options_when_open()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .AddChildContent<DrylCommand>(c => c.Add(x => x.Title, "Neue Rechnung")));

        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("[role=option]"),
                o => o.TextContent.Contains("Neue Rechnung")),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Legacy_Items_still_render()
    {
        var items = new[] { new CommandItem { Label = "Legacy item" } };
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, items));

        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("[role=option]"),
                o => o.TextContent.Contains("Legacy item")),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Running_argument_less_command_invokes_OnRun_with_empty_context()
    {
        CommandContext? captured = null;
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .AddChildContent<DrylCommand>(c => c
                .Add(x => x.Title, "Run me")
                .Add(x => x.OnRun, (CommandContext ctx) => { captured = ctx; })));

        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("[role=option]"), o => o.TextContent.Contains("Run me")),
            TimeSpan.FromSeconds(2));

        cut.FindAll("[role=option]").First(o => o.TextContent.Contains("Run me")).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(captured);
            Assert.Empty(captured!.Arguments);
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Resolver_result_shows_a_suggestion_row()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Resolver, new EchoResolver())
            .AddChildContent<DrylCommand>(c => c.Add(x => x.Title, "Status setzen")));

        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("[role=option]"), o => o.TextContent.Contains("Status setzen")),
            TimeSpan.FromSeconds(2));

        cut.Find(".cmd-search-input").Input("bezahlt");

        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("[role=option]"),
                o => o.GetAttribute("class")!.Contains("cmd-item--ai")),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Command_with_argument_opens_fill_view_then_runs_with_value()
    {
        CommandContext? captured = null;
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .AddChildContent<DrylCommand>(c => c
                .Add(x => x.Title, "Status setzen")
                .Add(x => x.OnRun, (CommandContext ctx) => { captured = ctx; })
                .AddChildContent<DrylCommandArgument>(a => a
                    .Add(p => p.Name, "note")
                    .Add(p => p.Type, CommandArgType.Text))));

        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("[role=option]"), o => o.TextContent.Contains("Status setzen")),
            TimeSpan.FromSeconds(2));

        cut.FindAll("[role=option]").First(o => o.TextContent.Contains("Status setzen")).Click();

        // Arg-fill view is shown with a text input.
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".cmd-args input")),
            TimeSpan.FromSeconds(2));

        cut.Find(".cmd-args input").Input("Paid");
        cut.Find(".cmd-args-confirm").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(captured);
            Assert.Equal("Paid", captured!.GetArgument<string>("note"));
        }, TimeSpan.FromSeconds(2));
    }

    private sealed class EchoResolver : ICommandResolver
    {
        public Task<CommandResolution?> ResolveAsync(
            string query, IReadOnlyList<DrylCommand> commands, CancellationToken ct)
            => Task.FromResult<CommandResolution?>(
                commands.Count == 0 ? null
                : new CommandResolution(commands[0],
                    new Dictionary<string, object?> { ["status"] = "Paid" }, 0.9));
    }
}
