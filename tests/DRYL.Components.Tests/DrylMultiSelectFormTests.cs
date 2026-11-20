using System.ComponentModel.DataAnnotations;
using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests;

/// <summary>
/// The "Keyboard" additions and the "Validation" criteria of
/// <c>specs/E8 Inputs/F6 DrylMultiSelect.md</c> (idea I52): Home, End and ArrowUp
/// without a highlight, and the field reporting to a surrounding <c>EditContext</c>.
/// The form is a cascaded <see cref="EditContext"/> around the field, which is what
/// <c>EditForm</c> provides. JSInterop is Loose for the same reason as in
/// <see cref="DrylMultiSelectSearchTests"/>.
/// </summary>
public class DrylMultiSelectFormTests : BunitContext
{
    private static readonly SelectItem[] Skills =
    [
        new("blazor", "Blazor"),
        new("css", "CSS"),
        new("redis", "Redis"),
        new("rest", "REST"),
        new("docker", "Docker"),
    ];

    private sealed class Model
    {
        [MinLength(1, ErrorMessage = "Pick at least one skill.")]
        public IReadOnlyList<string> Skills { get; set; } = [];
    }

    public DrylMultiSelectFormTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    // ── Keyboard ───────────────────────────────────────────────────────

    private IRenderedComponent<DrylMultiSelect> RenderField(bool searchable = false) =>
        Render<DrylMultiSelect>(ps => ps
            .Add(p => p.Items, Skills)
            .Add(p => p.Label, "Skills")
            .Add(p => p.Searchable, searchable));

    private static void Press(IRenderedComponent<DrylMultiSelect> cut, string key) =>
        cut.InvokeAsync(() => cut.Find("[role=combobox]").KeyDown(new KeyboardEventArgs { Key = key })).GetAwaiter().GetResult();

    private static string? Highlighted(IRenderedComponent<DrylMultiSelect> cut) =>
        cut.FindAll("[role=option].is-highlighted").SingleOrDefault()?.TextContent.Trim();

    [Fact]
    public void ArrowUp_on_a_closed_control_opens_it_at_the_last_option()
    {
        var cut = RenderField();

        Press(cut, "ArrowUp");

        Assert.Equal("true", cut.Find("[role=combobox]").GetAttribute("aria-expanded"));
        Assert.Equal("Docker", Highlighted(cut));
    }

    [Fact]
    public void ArrowUp_on_an_open_list_without_a_highlight_starts_at_the_last_option()
    {
        var cut = RenderField();
        cut.InvokeAsync(() => cut.Find(".multiselect-trigger").Click()).GetAwaiter().GetResult();
        Assert.Null(Highlighted(cut));

        Press(cut, "ArrowUp");
        Assert.Equal("Docker", Highlighted(cut));

        Press(cut, "ArrowUp");
        Assert.Equal("REST", Highlighted(cut));
    }

    [Fact]
    public void Home_and_End_jump_to_the_first_and_the_last_option()
    {
        var cut = RenderField();
        Press(cut, "ArrowDown");
        Press(cut, "ArrowDown");
        Assert.Equal("CSS", Highlighted(cut));

        Press(cut, "End");
        Assert.Equal("Docker", Highlighted(cut));

        Press(cut, "Home");
        Assert.Equal("Blazor", Highlighted(cut));
    }

    [Fact]
    public void Home_and_End_on_a_closed_control_do_nothing()
    {
        var cut = RenderField();

        Press(cut, "End");
        Press(cut, "Home");

        Assert.Equal("false", cut.Find("[role=combobox]").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void In_search_mode_ArrowUp_starts_at_the_last_listed_option_and_Home_stays_with_the_caret()
    {
        var cut = RenderField(searchable: true);
        cut.InvokeAsync(() => cut.Find("input.multiselect-search").Input("re")).GetAwaiter().GetResult();
        Assert.Equal("Redis", Highlighted(cut));

        Press(cut, "End");
        Press(cut, "Home");
        Assert.Equal("Redis", Highlighted(cut));

        Press(cut, "Escape");
        Press(cut, "Escape");
        Press(cut, "ArrowUp");
        Assert.Equal("Docker", Highlighted(cut));
    }

    // ── Validation ─────────────────────────────────────────────────────

    private IRenderedComponent<DrylMultiSelect> RenderInForm(
        Model model, EditContext context, bool bound = true, bool searchable = false,
        InputState state = InputState.Default, string? helper = null)
    {
        RenderFragment field = builder =>
        {
            builder.OpenComponent<DrylMultiSelect>(0);
            builder.AddComponentParameter(1, nameof(DrylMultiSelect.Items), Skills);
            builder.AddComponentParameter(2, nameof(DrylMultiSelect.Label), "Skills");
            builder.AddComponentParameter(3, nameof(DrylMultiSelect.Searchable), searchable);
            builder.AddComponentParameter(4, nameof(DrylMultiSelect.State), state);
            builder.AddComponentParameter(5, nameof(DrylMultiSelect.HelperText), helper);
            builder.AddComponentParameter(6, nameof(DrylMultiSelect.SelectedValues), model.Skills);
            builder.AddComponentParameter(7, nameof(DrylMultiSelect.SelectedValuesChanged),
                EventCallback.Factory.Create<IReadOnlyList<string>>(this, v => model.Skills = v));
            if (bound)
            {
                builder.AddComponentParameter(8, nameof(DrylMultiSelect.SelectedValuesExpression),
                    (System.Linq.Expressions.Expression<Func<IReadOnlyList<string>>>)(() => model.Skills));
            }
            builder.CloseComponent();
        };

        var host = Render<CascadingValue<EditContext>>(ps => ps
            .Add(p => p.Value, context)
            .Add(p => p.ChildContent, field));
        return host.FindComponent<DrylMultiSelect>();
    }

    private static (Model, EditContext) Form()
    {
        var model = new Model();
        var context = new EditContext(model);
        context.EnableDataAnnotationsValidation(new ServiceCollection().BuildServiceProvider());
        return (model, context);
    }

    private static void Toggle(IRenderedComponent<DrylMultiSelect> cut, string label)
    {
        if (cut.FindAll("[role=listbox]").Count == 0)
            cut.InvokeAsync(() => cut.Find(".multiselect-trigger").Click()).GetAwaiter().GetResult();
        cut.InvokeAsync(() => cut.FindAll("[role=option]").Single(o => o.TextContent.Trim() == label).Click())
           .GetAwaiter().GetResult();
    }

    [Fact]
    public void A_change_marks_the_bound_field_as_modified()
    {
        var (model, context) = Form();
        var cut = RenderInForm(model, context);
        var field = context.Field(nameof(Model.Skills));
        Assert.False(context.IsModified(field));

        Toggle(cut, "Redis");

        Assert.Equal(["redis"], model.Skills);
        Assert.True(context.IsModified(field));
    }

    [Fact]
    public void A_validation_message_shows_the_error_state_and_replaces_the_helper()
    {
        var (model, context) = Form();
        var cut = RenderInForm(model, context, helper: "Pick what fits.");
        Assert.Equal("Pick what fits.", cut.Find(".input-helper").TextContent.Trim());
        Assert.DoesNotContain("input-error", cut.Find(".multiselect-trigger").ClassList);
        Assert.Null(cut.Find("[role=combobox]").GetAttribute("aria-invalid"));

        cut.InvokeAsync(() => context.Validate()).GetAwaiter().GetResult();

        var helper = cut.Find(".input-helper");
        Assert.Equal("Pick at least one skill.", helper.TextContent.Trim());
        Assert.Contains("input-helper-error", helper.ClassList);
        Assert.Contains("input-error", cut.Find(".multiselect-trigger").ClassList);
        var combobox = cut.Find("[role=combobox]");
        Assert.Equal("true", combobox.GetAttribute("aria-invalid"));
        Assert.Equal(helper.Id, combobox.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Picking_a_value_clears_the_message_through_the_field_change()
    {
        var (model, context) = Form();
        var cut = RenderInForm(model, context, helper: "Pick what fits.");
        cut.InvokeAsync(() => context.Validate()).GetAwaiter().GetResult();
        Assert.Contains("input-error", cut.Find(".multiselect-trigger").ClassList);

        Toggle(cut, "Redis");

        Assert.DoesNotContain("input-error", cut.Find(".multiselect-trigger").ClassList);
        Assert.Null(cut.Find("[role=combobox]").GetAttribute("aria-invalid"));
        Assert.Equal("Pick what fits.", cut.Find(".input-helper").TextContent.Trim());
    }

    [Fact]
    public void Removing_the_last_value_by_its_chip_brings_the_message_back()
    {
        var (model, context) = Form();
        model.Skills = ["redis"];
        var cut = RenderInForm(model, context);

        cut.InvokeAsync(() => cut.Find("button[aria-label='Remove Redis']").Click()).GetAwaiter().GetResult();

        Assert.Empty(model.Skills);
        Assert.Equal("Pick at least one skill.", cut.Find(".input-helper").TextContent.Trim());
    }

    [Fact]
    public void A_validation_message_wins_over_State()
    {
        var (model, context) = Form();
        var cut = RenderInForm(model, context, state: InputState.Success);
        Assert.Contains("input-success", cut.Find(".multiselect-trigger").ClassList);

        cut.InvokeAsync(() => context.Validate()).GetAwaiter().GetResult();

        var classes = cut.Find(".multiselect-trigger").ClassList;
        Assert.Contains("input-error", classes);
        Assert.DoesNotContain("input-success", classes);
    }

    [Fact]
    public void Several_messages_are_joined_by_a_space()
    {
        var (model, context) = Form();
        var cut = RenderInForm(model, context);
        var store = new ValidationMessageStore(context);
        store.Add(context.Field(nameof(Model.Skills)), "First.");
        store.Add(context.Field(nameof(Model.Skills)), "Second.");

        cut.InvokeAsync(() => context.NotifyValidationStateChanged()).GetAwaiter().GetResult();

        Assert.Equal("First. Second.", cut.Find(".input-helper").TextContent.Trim());
    }

    [Fact]
    public void In_search_mode_the_text_field_carries_aria_invalid()
    {
        var (model, context) = Form();
        var cut = RenderInForm(model, context, searchable: true);

        cut.InvokeAsync(() => context.Validate()).GetAwaiter().GetResult();

        Assert.Equal("true", cut.Find("input.multiselect-search").GetAttribute("aria-invalid"));
        Assert.Contains("input-error", cut.Find(".multiselect-trigger").ClassList);
    }

    [Fact]
    public void Without_the_expression_the_field_stays_out_of_the_form()
    {
        var (model, context) = Form();
        var cut = RenderInForm(model, context, bound: false);

        Toggle(cut, "Redis");
        cut.InvokeAsync(() => context.Validate()).GetAwaiter().GetResult();

        Assert.False(context.IsModified());
        Assert.Empty(cut.FindAll(".input-helper"));
        Assert.DoesNotContain("input-error", cut.Find(".multiselect-trigger").ClassList);
    }

    [Fact]
    public void Without_a_form_the_field_works_as_before()
    {
        IReadOnlyList<string> values = [];
        var cut = Render<DrylMultiSelect>(ps => ps
            .Add(p => p.Items, Skills)
            .Add(p => p.SelectedValues, values)
            .Add(p => p.SelectedValuesChanged, v => values = v)
            .Add(p => p.SelectedValuesExpression, () => values));

        Toggle(cut, "Redis");

        Assert.Equal(["redis"], values);
        Assert.Null(cut.Find("[role=combobox]").GetAttribute("aria-invalid"));
    }
}
