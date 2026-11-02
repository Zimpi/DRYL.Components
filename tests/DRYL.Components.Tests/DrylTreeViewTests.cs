using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// The render side of the unfolding tree (idea I41): the fold, the stagger marker, the
/// parameter-versus-toggle rule, and where the focus goes when a folder closes. The ink
/// and the thread live in <c>dryl.motion.treeGlide</c>, which bUnit never executes;
/// <c>tests/js/tree-glide.test.mjs</c> covers that half.
/// </summary>
public class DrylTreeViewTests : BunitContext
{
    public DrylTreeViewTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private IRenderedComponent<DrylTreeView> RenderTree(object? selected = null, bool rootOpen = true, bool childOpen = false) =>
        Render<DrylTreeView>(ps => ps
            .Add(p => p.SelectedValue, selected)
            .Add(p => p.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<DrylTreeNode>(0);
                b.AddAttribute(1, nameof(DrylTreeNode.Text), "root");
                b.AddAttribute(2, nameof(DrylTreeNode.Value), "root");
                b.AddAttribute(3, nameof(DrylTreeNode.Expanded), rootOpen);
                b.AddAttribute(4, nameof(DrylTreeNode.ChildContent), (RenderFragment)(c =>
                {
                    c.OpenComponent<DrylTreeNode>(0);
                    c.AddAttribute(1, nameof(DrylTreeNode.Text), "child");
                    c.AddAttribute(2, nameof(DrylTreeNode.Value), "child");
                    c.AddAttribute(3, nameof(DrylTreeNode.Expanded), childOpen);
                    c.AddAttribute(4, nameof(DrylTreeNode.ChildContent), (RenderFragment)(l =>
                    {
                        l.OpenComponent<DrylTreeNode>(0);
                        l.AddAttribute(1, nameof(DrylTreeNode.Text), "leaf");
                        l.AddAttribute(2, nameof(DrylTreeNode.Value), "leaf");
                        l.CloseComponent();
                    }));
                    c.CloseComponent();
                    c.OpenComponent<DrylTreeNode>(5);
                    c.AddAttribute(6, nameof(DrylTreeNode.Text), "sibling");
                    c.AddAttribute(7, nameof(DrylTreeNode.Value), "sibling");
                    c.CloseComponent();
                }));
                b.CloseComponent();
            })));

    private static AngleSharp.Dom.IElement Row(IRenderedComponent<DrylTreeView> cut, string text) =>
        cut.FindAll(".tree-row").Single(r => r.TextContent.Trim() == text);

    private static AngleSharp.Dom.IElement Fold(IRenderedComponent<DrylTreeView> cut, string text) =>
        Row(cut, text).ParentElement!.Children.Single(c => c.ClassList.Contains("tree-fold"));

    private static void Toggle(IRenderedComponent<DrylTreeView> cut, string text) =>
        Row(cut, text).QuerySelector(".tree-toggle")!.Click();

    [Fact]
    public void A_closed_group_stays_in_the_document_without_the_hidden_attribute()
    {
        var cut = RenderTree(rootOpen: false);

        var fold = Fold(cut, "root");
        Assert.False(fold.ClassList.Contains("is-open"));
        Assert.False(fold.QuerySelector("[role=group]")!.HasAttribute("hidden"));
    }

    [Fact]
    public void A_tree_that_renders_open_does_not_play_its_stagger()
    {
        var cut = RenderTree();

        var fold = Fold(cut, "root");
        Assert.True(fold.ClassList.Contains("is-open"));
        Assert.False(fold.ClassList.Contains("is-entering"));
    }

    [Fact]
    public void Opening_a_folder_marks_its_fold_as_entering_and_closing_clears_it()
    {
        var cut = RenderTree();

        Toggle(cut, "child");
        Assert.True(Fold(cut, "child").ClassList.Contains("is-entering"));

        Toggle(cut, "child");
        Assert.False(Fold(cut, "child").ClassList.Contains("is-open"));
        Assert.False(Fold(cut, "child").ClassList.Contains("is-entering"));
    }

    [Fact]
    public void Rows_publish_their_sibling_index_and_groups_their_parents_depth()
    {
        var cut = RenderTree();

        Assert.Contains("--tree-i: 0", Row(cut, "child").GetAttribute("style"));
        Assert.Contains("--tree-i: 1", Row(cut, "sibling").GetAttribute("style"));
        Assert.Contains("--tree-parent-depth: 1", Fold(cut, "child").QuerySelector("[role=group]")!.GetAttribute("style"));
    }

    [Fact]
    public void The_chevron_turns_inside_its_toggle_so_the_stagger_can_move_the_toggle()
    {
        var cut = RenderTree();

        Assert.False(Row(cut, "root").QuerySelector(".tree-toggle")!.ClassList.Contains("is-open"));
        Assert.True(Row(cut, "root").QuerySelector(".tree-chevron")!.ClassList.Contains("is-open"));
    }

    [Fact]
    public void A_re_render_with_the_same_Expanded_keeps_the_users_toggle()
    {
        var cut = RenderTree();
        Toggle(cut, "child");

        cut.Render();

        Assert.True(Fold(cut, "child").ClassList.Contains("is-open"));
    }

    [Fact]
    public void Closing_the_folder_around_the_focused_node_moves_the_tab_stop_to_the_folder()
    {
        var cut = RenderTree(childOpen: true);
        Row(cut, "leaf").Click();
        Assert.Equal("0", Row(cut, "leaf").GetAttribute("tabindex"));

        Toggle(cut, "child");

        Assert.Equal("0", Row(cut, "child").GetAttribute("tabindex"));
        Assert.Equal("-1", Row(cut, "leaf").GetAttribute("tabindex"));
    }

    [Fact]
    public void A_selection_preset_inside_a_closed_folder_gives_the_tab_stop_to_the_outermost_closed_folder()
    {
        var cut = RenderTree(selected: "leaf", rootOpen: false);

        Assert.Equal("0", Row(cut, "root").GetAttribute("tabindex"));
        Assert.Equal("-1", Row(cut, "leaf").GetAttribute("tabindex"));
        Assert.Equal("true", Row(cut, "leaf").ParentElement!.GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task The_view_attaches_the_ink_once_and_releases_it_on_dispose()
    {
        var cut = RenderTree();
        Toggle(cut, "child");

        Assert.Single(JSInterop.Invocations["dryl.motion.treeGlide"]);

        await DisposeComponentsAsync();

        Assert.Single(JSInterop.Invocations["dryl.motion.disposeTreeGlide"]);
    }
}
