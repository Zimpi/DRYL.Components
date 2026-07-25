using System.Text.Json;
using Bunit;
using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>Undo, redo and "back to version N" as the workspace bar offers them.</summary>
public class DrylCanvasWorkspaceHistoryTests : BunitContext
{
    public DrylCanvasWorkspaceHistoryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Spec(string title) =>
        JsonSerializer.Deserialize<CanvasSpec>(
            $$"""{ "title": "{{title}}", "root": { "id": "r", "type": "markdown", "props": { "content": "{{title}}" } } }""",
            CanvasJson.Options)!;

    private static CanvasWorkspace OneView(params string[] versions)
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        foreach (var v in versions)
        {
            view.Spec = Spec(v);
            ws.Commit(v);
        }
        return ws;
    }

    [Fact]
    public void Without_ShowHistory_the_bar_has_no_tools()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, OneView("v1", "v2")));

        Assert.Empty(cut.FindAll(".ws-tools"));
    }

    [Fact]
    public void The_bar_shows_for_a_single_view_once_history_is_on()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, OneView("v1"))
            .Add(x => x.ShowHistory, true));

        Assert.Single(cut.FindAll(".ws-tools"));
        Assert.Single(cut.FindAll(".ws-chip"));
    }

    [Fact]
    public void Undo_is_disabled_with_one_version_and_enabled_with_two()
    {
        var one = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, OneView("v1"))
            .Add(x => x.ShowHistory, true));
        Assert.True(one.Find(".ws-tools button[aria-label='Undo']").HasAttribute("disabled"));

        var two = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, OneView("v1", "v2"))
            .Add(x => x.ShowHistory, true));
        Assert.False(two.Find(".ws-tools button[aria-label='Undo']").HasAttribute("disabled"));
    }

    [Fact]
    public void Clicking_undo_puts_the_previous_spec_back()
    {
        var ws = OneView("v1", "v2");
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.ShowHistory, true));

        cut.Find(".ws-tools button[aria-label='Undo']").Click();

        Assert.Equal("v1", ws.Active!.Spec!.Title);
        Assert.False(cut.Find(".ws-tools button[aria-label='Redo']").HasAttribute("disabled"));
    }

    [Fact]
    public void A_changed_Revision_commits_the_active_view_exactly_once()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        view.Spec = Spec("v1");

        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.ShowHistory, true)
            .Add(x => x.Revision, 1)
            .Add(x => x.RevisionLabel, "first prompt"));

        Assert.Single(view.History.Entries);
        Assert.Equal("first prompt", view.History.Entries[0].Label);

        cut.Render(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.ShowHistory, true)
            .Add(x => x.Revision, 1)
            .Add(x => x.RevisionLabel, "first prompt"));

        Assert.Single(view.History.Entries);

        view.Spec = Spec("v2");
        cut.Render(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.ShowHistory, true)
            .Add(x => x.Revision, 2)
            .Add(x => x.RevisionLabel, "second prompt"));

        Assert.Equal(2, view.History.Entries.Count);
        Assert.Equal("second prompt", view.History.Entries[1].Label);
    }

    [Fact]
    public void The_version_list_shows_the_newest_first_and_marks_the_current_one()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, OneView("v1", "v2", "v3"))
            .Add(x => x.ShowHistory, true));

        cut.Find(".ws-tools button[aria-label='Version history']").Click();

        var items = cut.FindAll(".ws-version");
        Assert.Equal(3, items.Count);
        Assert.Contains("v3", items[0].TextContent);
        Assert.Contains("is-current", items[0].ClassName);
    }

    [Fact]
    public void Picking_a_version_restores_it()
    {
        var ws = OneView("v1", "v2", "v3");
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.ShowHistory, true));

        cut.Find(".ws-tools button[aria-label='Version history']").Click();
        cut.FindAll(".ws-version")[2].Click();      // oldest

        Assert.Equal("v1", ws.Active!.Spec!.Title);
    }

    [Fact]
    public async Task AutoSave_writes_the_workspace_to_the_registered_store()
    {
        var store = new InMemoryCanvasDocumentStore();
        Services.AddSingleton<ICanvasDocumentStore>(store);

        var ws = OneView("v1");
        string? savedId = null;

        Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.ShowHistory, true)
            .Add(x => x.AutoSave, true)
            .Add(x => x.AutoSaveDelayMs, 0)
            .Add(x => x.DocumentTitle, "My dashboard")
            .Add(x => x.DocumentIdChanged, (string id) => savedId = id)
            .Add(x => x.Revision, 1));

        for (var i = 0; i < 50 && savedId is null; i++) await Task.Delay(20);

        Assert.NotNull(savedId);
        var list = await store.ListAsync();
        Assert.Single(list);
        Assert.Equal("My dashboard", list[0].Title);
    }

    [Fact]
    public void AutoSave_without_a_registered_store_is_a_no_op()
    {
        var ws = OneView("v1");

        var ex = Record.Exception(() => Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.AutoSave, true)
            .Add(x => x.AutoSaveDelayMs, 0)
            .Add(x => x.Revision, 1)));

        Assert.Null(ex);
    }
}
