using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// The render side of the list ink (idea I38): which element of each menu and option
/// list is the ink's host, and that the menu's flat role pair survives it. The ink
/// itself — placement, glide, hover light — lives in <c>dryl.motion.glideScan</c>,
/// which bUnit never executes; <c>tests/js/glide.test.mjs</c> covers that half.
/// JSInterop is Loose because the popover wires itself through dryl.js.
/// </summary>
public class ListGlideTests : BunitContext
{
    public ListGlideTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private sealed class Model { public string? Value { get; set; } = "b"; }

    private static readonly SelectItem[] Items = [new("a", "Alpha"), new("b", "Beta")];

    private static void Open<T>(IRenderedComponent<T> cut, string trigger = ".popover-trigger") where T : Microsoft.AspNetCore.Components.IComponent
        => cut.InvokeAsync(() => cut.Find(trigger).Click()).GetAwaiter().GetResult();

    [Fact]
    public void Select_marks_its_listbox_as_the_ink_host()
    {
        var model = new Model();
        var cut = Render<DrylSelect>(ps => ps
            .Add(p => p.Value, model.Value)
            .Add(p => p.ValueExpression, () => model.Value)
            .Add(p => p.Items, Items));
        Open(cut, "[role=combobox]");

        var listbox = cut.Find("[role=listbox]");
        Assert.Equal("[role=option]", listbox.GetAttribute("data-dryl-glide"));
        Assert.Equal(2, listbox.QuerySelectorAll("[role=option]").Length);
    }

    [Fact]
    public void MultiSelect_marks_its_listbox_as_the_ink_host()
    {
        var cut = Render<DrylMultiSelect>(ps => ps
            .Add(p => p.Items, Items));
        Open(cut, "[role=combobox]");

        Assert.Equal("[role=option]", cut.Find("[role=listbox]").GetAttribute("data-dryl-glide"));
    }

    [Fact]
    public void Autocomplete_marks_its_listbox_as_the_ink_host()
    {
        var cut = Render<DrylAutocomplete<string>>(ps => ps
            .Add(p => p.Items, ["Alpha", "Beta"])
            .Add(p => p.DisplayText, s => s));
        cut.InvokeAsync(() => cut.Find("input").Input("a")).GetAwaiter().GetResult();

        cut.WaitForAssertion(() => Assert.Equal("[role=option]", cut.Find("[role=listbox]").GetAttribute("data-dryl-glide")));
    }

    [Fact]
    public void TimePicker_marks_each_column_as_its_own_ink_host()
    {
        var cut = Render<DrylTimePicker>();
        Open(cut, "input");

        var columns = cut.FindAll(".time-col");
        Assert.Equal(2, columns.Count);
        Assert.All(columns, c => Assert.Equal(".time-cell", c.GetAttribute("data-dryl-glide")));
    }

    [Fact]
    public void Menu_items_stay_direct_children_of_the_menu()
    {
        // The ink is the panel's ::before, never an element: a node between the
        // menu and its menuitems would break the role pair.
        var cut = Render<DrylMenu>(ps => ps
            .Add(p => p.Trigger, b => b.AddMarkupContent(0, "<button type=\"button\">Open</button>"))
            .Add(p => p.Items, b =>
            {
                b.OpenComponent<DrylMenuItem>(0);
                b.AddAttribute(1, nameof(DrylMenuItem.ChildContent), (Microsoft.AspNetCore.Components.RenderFragment)(c => c.AddContent(0, "Edit")));
                b.CloseComponent();
                b.OpenComponent<DrylMenuItem>(2);
                b.AddAttribute(3, nameof(DrylMenuItem.Variant), DrylMenuItem.MenuItemVariant.Danger);
                b.AddAttribute(4, nameof(DrylMenuItem.ChildContent), (Microsoft.AspNetCore.Components.RenderFragment)(c => c.AddContent(0, "Delete")));
                b.CloseComponent();
            }));
        Open(cut);

        var menu = cut.Find("[role=menu]");
        var children = menu.Children;
        Assert.Equal(2, children.Length);
        Assert.All(children, c => Assert.Equal("menuitem", c.GetAttribute("role")));
        Assert.Contains("menu-item--danger", children[1].ClassName);
    }
}
