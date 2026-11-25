using System.ComponentModel.DataAnnotations;
using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylMarkdownEditor"/> — <c>specs/E8 Inputs/F24 DrylMarkdownEditor.md</c>.
/// The text edits themselves live in <c>dryl.mdeditor</c> and are covered by
/// <c>tests/js/mdeditor.test.mjs</c>; here the markup, the binding, the view and the
/// interop calls are checked. JSInterop is Loose because the composed tooltip and
/// presence components make calls of their own.
/// </summary>
public class DrylMarkdownEditorTests : BunitContext
{
    private const string Attach = "dryl.mdeditor.attach";
    private const string Sync = "dryl.mdeditor.sync";
    private const string Detach = "dryl.mdeditor.detach";

    public DrylMarkdownEditorTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private IRenderedComponent<DrylMarkdownEditor> RenderEditor(
        Action<ComponentParameterCollectionBuilder<DrylMarkdownEditor>>? extra = null)
        => Render<DrylMarkdownEditor>(ps => extra?.Invoke(ps));

    private static void Switch(IRenderedComponent<DrylMarkdownEditor> cut, string label) =>
        cut.FindAll("[role=radio]").Single(r => r.TextContent.Trim() == label).Click();

    private int Calls(string identifier) => JSInterop.Invocations.Count(i => i.Identifier == identifier);

    // ── Structure ──────────────────────────────────────────────────────

    [Fact]
    public void The_root_is_a_field_that_holds_one_frame_with_a_header_and_a_body()
    {
        var cut = RenderEditor();

        var root = cut.Find(".field.mde-field");
        Assert.Single(root.QuerySelectorAll(".mde"));
        Assert.Single(cut.FindAll(".mde > .mde-header"));
        Assert.Single(cut.FindAll(".mde > .mde-body"));
    }

    [Fact]
    public void Class_is_merged_onto_the_root()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.Class, "mine"));

        var root = cut.Find(".mde-field");
        Assert.Contains("field", root.ClassList);
        Assert.Contains("mine", root.ClassList);
    }

    [Fact]
    public void The_source_is_one_textarea_that_shows_the_value_and_carries_the_field_markers()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.Value, "# Title"));

        var source = Assert.Single(cut.FindAll("textarea"));
        Assert.Equal("# Title", source.GetAttribute("value"));
        Assert.True(source.HasAttribute("data-dryl-input"));
        Assert.True(source.HasAttribute("data-mde-source"));
        Assert.Equal("8", source.GetAttribute("rows"));
    }

    [Fact]
    public void Rows_Placeholder_and_additional_attributes_reach_the_source()
    {
        var cut = RenderEditor(ps => ps
            .Add(p => p.Rows, 3)
            .Add(p => p.Placeholder, "Write…")
            .AddUnmatched("maxlength", "200"));

        var source = cut.Find("textarea");
        Assert.Equal("3", source.GetAttribute("rows"));
        Assert.Equal("Write…", source.GetAttribute("placeholder"));
        Assert.Equal("200", source.GetAttribute("maxlength"));
    }

    [Fact]
    public void A_label_is_tied_to_the_source_and_replaces_the_aria_label()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.Label, "Body").Add(p => p.AriaLabel, "ignored"));

        var source = cut.Find("textarea");
        Assert.Equal(source.Id, cut.Find("label.field-label").GetAttribute("for"));
        Assert.Null(source.GetAttribute("aria-label"));
    }

    [Fact]
    public void Without_a_label_the_aria_label_names_the_source()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.AriaLabel, "Comment"));

        Assert.Empty(cut.FindAll("label"));
        Assert.Equal("Comment", cut.Find("textarea").GetAttribute("aria-label"));
    }

    // ── Value ──────────────────────────────────────────────────────────

    [Fact]
    public void Typing_updates_the_bound_value()
    {
        string? bound = null;
        var cut = RenderEditor(ps => ps
            .Add(p => p.Value, "")
            .Add(p => p.ValueChanged, v => bound = v));

        cut.Find("textarea").Input("**bold**");

        Assert.Equal("**bold**", bound);
    }

    // ── View ───────────────────────────────────────────────────────────

    [Fact]
    public void The_switch_is_a_radio_group_of_two_with_the_editor_selected()
    {
        var cut = RenderEditor();

        Assert.Equal("View", cut.Find("[role=radiogroup]").GetAttribute("aria-label"));
        var radios = cut.FindAll("[role=radio]");
        Assert.Equal(["Editor", "Preview"], radios.Select(r => r.TextContent.Trim()));
        Assert.Equal(["true", "false"], radios.Select(r => r.GetAttribute("aria-checked")));
    }

    [Fact]
    public void In_the_editor_view_the_frame_says_so_and_no_preview_is_rendered()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.Value, "text"));

        var frame = cut.Find(".mde");
        Assert.Contains("mde--editor", frame.ClassList);
        Assert.Equal("editor", frame.GetAttribute("data-mde-view"));
        Assert.Empty(cut.FindAll(".mde-preview"));
        Assert.Empty(cut.FindAll(".md"));
    }

    [Fact]
    public void Choosing_Preview_renders_the_value_as_Markdown_and_reports_the_view()
    {
        MarkdownEditorView? reported = null;
        var cut = RenderEditor(ps => ps
            .Add(p => p.Value, "Some **bold** text")
            .Add(p => p.ViewChanged, v => reported = v));

        Switch(cut, "Preview");

        Assert.Equal(MarkdownEditorView.Preview, reported);
        var frame = cut.Find(".mde");
        Assert.Contains("mde--preview", frame.ClassList);
        Assert.Equal("preview", frame.GetAttribute("data-mde-view"));
        Assert.Equal("bold", cut.Find(".mde-preview .md-content strong").TextContent);
    }

    [Fact]
    public void The_preview_is_a_named_region_in_the_tab_order()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.View, MarkdownEditorView.Preview));

        var preview = cut.Find(".mde-preview");
        Assert.Equal("region", preview.GetAttribute("role"));
        Assert.Equal("Preview", preview.GetAttribute("aria-label"));
        Assert.Equal("0", preview.GetAttribute("tabindex"));
    }

    [Fact]
    public void The_source_stays_rendered_and_enabled_in_the_preview()
    {
        var cut = RenderEditor(ps => ps
            .Add(p => p.Value, "text")
            .Add(p => p.View, MarkdownEditorView.Preview));

        var source = Assert.Single(cut.FindAll("textarea"));
        Assert.False(source.HasAttribute("disabled"));
        Assert.Equal("text", source.GetAttribute("value"));
    }

    [Fact]
    public void The_preview_follows_a_value_that_changes_while_it_is_shown()
    {
        var cut = RenderEditor(ps => ps
            .Add(p => p.Value, "one")
            .Add(p => p.View, MarkdownEditorView.Preview));

        cut.Find("textarea").Input("# two");

        Assert.Equal("two", cut.Find(".mde-preview h1").TextContent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n ")]
    public void An_empty_value_previews_as_the_empty_text(string? value)
    {
        var cut = RenderEditor(ps => ps
            .Add(p => p.Value, value!)
            .Add(p => p.View, MarkdownEditorView.Preview));

        Assert.Equal("Nothing to preview", cut.Find(".mde-preview-empty").TextContent);
        Assert.Empty(cut.FindAll(".md"));
    }

    [Fact]
    public void Raw_html_in_the_value_is_escaped_in_the_preview()
    {
        var cut = RenderEditor(ps => ps
            .Add(p => p.Value, "<script>alert(1)</script>")
            .Add(p => p.View, MarkdownEditorView.Preview));

        Assert.Empty(cut.FindAll(".mde-preview script"));
        Assert.Contains("alert(1)", cut.Find(".mde-preview").TextContent);
    }

    [Fact]
    public void The_View_parameter_opens_the_preview_and_a_changed_parameter_moves_the_view()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.View, MarkdownEditorView.Preview));
        Assert.Contains("mde--preview", cut.Find(".mde").ClassList);

        cut.Render(ps => ps.Add(p => p.View, MarkdownEditorView.Editor));

        Assert.Contains("mde--editor", cut.Find(".mde").ClassList);
    }

    [Fact]
    public void A_view_the_user_chose_survives_a_rerender_with_an_unchanged_parameter()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.Value, "a"));
        Switch(cut, "Preview");

        cut.Render(ps => ps.Add(p => p.Value, "b"));

        Assert.Contains("mde--preview", cut.Find(".mde").ClassList);
    }

    [Fact]
    public void Choosing_the_view_that_is_already_shown_reports_nothing()
    {
        var reports = 0;
        var cut = RenderEditor(ps => ps.Add(p => p.ViewChanged, _ => reports++));

        Switch(cut, "Editor");

        Assert.Equal(0, reports);
    }

    // ── Toolbar ────────────────────────────────────────────────────────

    [Fact]
    public void The_toolbar_is_named_and_points_at_the_source()
    {
        var cut = RenderEditor();

        var toolbar = cut.Find("[role=toolbar]");
        Assert.Equal("Formatting", toolbar.GetAttribute("aria-label"));
        Assert.Equal(cut.Find("textarea").Id, toolbar.GetAttribute("aria-controls"));
        Assert.True(toolbar.HasAttribute("data-mde-toolbar"));
    }

    [Fact]
    public void The_toolbar_holds_the_ten_commands_in_order()
    {
        var cut = RenderEditor();

        Assert.Equal(
            ["bold", "italic", "strike", "heading", "bullet", "ordered", "task", "quote", "code", "link"],
            cut.FindAll("[role=toolbar] button").Select(b => b.GetAttribute("data-mde-cmd")));
    }

    [Fact]
    public void Every_command_is_an_icon_only_button_whose_tooltip_and_name_say_the_same()
    {
        var cut = RenderEditor();
        string[] labels = ["Bold", "Italic", "Strikethrough", "Heading", "Bullet list", "Numbered list",
                           "Task list", "Quote", "Code", "Link"];

        var buttons = cut.FindAll("[role=toolbar] button");
        Assert.Equal(labels, buttons.Select(b => b.GetAttribute("aria-label")));
        Assert.All(buttons, b => Assert.Contains("btn-icon", b.ClassList));
        Assert.All(buttons, b => Assert.Equal("button", b.GetAttribute("type")));
        Assert.Equal(labels, cut.FindAll("[role=toolbar] .tt-wrap").Select(t => t.GetAttribute("data-tt")));
    }

    [Fact]
    public void Exactly_the_first_command_is_the_toolbars_tab_stop()
    {
        var cut = RenderEditor();

        Assert.Equal(
            ["0", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1"],
            cut.FindAll("[role=toolbar] button").Select(b => b.GetAttribute("tabindex")));
    }

    [Theory]
    [InlineData("bold", "Control+B Meta+B")]
    [InlineData("italic", "Control+I Meta+I")]
    [InlineData("code", "Control+E Meta+E")]
    [InlineData("link", "Control+K Meta+K")]
    [InlineData("heading", null)]
    public void A_command_with_a_shortcut_declares_it(string command, string? shortcut)
    {
        var cut = RenderEditor();

        Assert.Equal(shortcut, cut.Find($"button[data-mde-cmd={command}]").GetAttribute("aria-keyshortcuts"));
    }

    [Fact]
    public void The_commands_are_enabled_in_the_editor_and_disabled_in_the_preview()
    {
        var cut = RenderEditor();
        Assert.All(cut.FindAll("[role=toolbar] button"), b => Assert.False(b.HasAttribute("disabled")));

        Switch(cut, "Preview");

        Assert.All(cut.FindAll("[role=toolbar] button"), b => Assert.True(b.HasAttribute("disabled")));
    }

    // ── Disabled ───────────────────────────────────────────────────────

    [Fact]
    public void Disabled_disables_the_source_and_the_commands_but_not_the_switch()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.Value, "**x**").Add(p => p.Disabled, true));

        Assert.True(cut.Find("textarea").HasAttribute("disabled"));
        Assert.Contains("mde--disabled", cut.Find(".mde").ClassList);
        Assert.All(cut.FindAll("[role=toolbar] button"), b => Assert.True(b.HasAttribute("disabled")));
        Assert.All(cut.FindAll("[role=radio]"), r => Assert.False(r.HasAttribute("disabled")));

        Switch(cut, "Preview");
        Assert.Equal("x", cut.Find(".mde-preview strong").TextContent);
    }

    // ── Validation and state ───────────────────────────────────────────

    private sealed class Model
    {
        [Required(ErrorMessage = "A body is required.")]
        public string Body { get; set; } = "";
    }

    private IRenderedComponent<DrylMarkdownEditor> RenderInForm(Model model, EditContext context, string? helper = null) =>
        Render<CascadingValue<EditContext>>(ps => ps
            .Add(p => p.Value, context)
            .AddChildContent<DrylMarkdownEditor>(e => e
                .Add(p => p.Value, model.Body)
                .Add(p => p.ValueExpression, () => model.Body)
                .Add(p => p.HelperText, helper)))
            .FindComponent<DrylMarkdownEditor>();

    [Fact]
    public void A_validation_message_marks_the_frame_and_the_source_and_replaces_the_helper()
    {
        var model = new Model();
        var context = new EditContext(model);
        context.EnableDataAnnotationsValidation(Services);
        var cut = RenderInForm(model, context, helper: "Markdown is supported.");
        Assert.Equal("Markdown is supported.", cut.Find(".input-helper").TextContent);

        cut.InvokeAsync(() => context.Validate());

        var helper = cut.Find(".input-helper.input-helper-error");
        Assert.Equal("A body is required.", helper.TextContent.Trim());
        Assert.Contains("mde--error", cut.Find(".mde").ClassList);
        var source = cut.Find("textarea");
        Assert.Equal("true", source.GetAttribute("aria-invalid"));
        Assert.Equal(helper.Id, source.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Typing_notifies_the_edit_context()
    {
        var model = new Model();
        var context = new EditContext(model);
        var cut = RenderInForm(model, context);

        cut.Find("textarea").Input("text");

        Assert.True(context.IsModified(context.Field(nameof(Model.Body))));
    }

    [Theory]
    [InlineData(InputState.Error, "mde--error")]
    [InlineData(InputState.Success, "mde--success")]
    public void State_colors_the_frame_without_declaring_the_source_invalid(InputState state, string css)
    {
        var cut = RenderEditor(ps => ps.Add(p => p.State, state));

        Assert.Contains(css, cut.Find(".mde").ClassList);
        Assert.Null(cut.Find("textarea").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Helper_text_is_shown_and_describes_the_source()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.HelperText, "Markdown is supported."));

        var helper = cut.Find(".input-helper");
        Assert.Equal("Markdown is supported.", helper.TextContent);
        Assert.Equal(helper.Id, cut.Find("textarea").GetAttribute("aria-describedby"));
    }

    // ── Texts ──────────────────────────────────────────────────────────

    [Fact]
    public void Texts_replace_every_word_of_the_editor()
    {
        var texts = MarkdownEditorTexts.Default with
        {
            View = "Ansicht", Editor = "Quelltext", Preview = "Vorschau", PreviewEmpty = "Nichts anzuzeigen",
            Toolbar = "Formatierung", Bold = "Fett", Link = "Verweis",
        };
        var cut = RenderEditor(ps => ps.Add(p => p.Texts, texts).Add(p => p.View, MarkdownEditorView.Preview));

        Assert.Equal("Ansicht", cut.Find("[role=radiogroup]").GetAttribute("aria-label"));
        Assert.Equal(["Quelltext", "Vorschau"], cut.FindAll("[role=radio]").Select(r => r.TextContent.Trim()));
        Assert.Equal("Formatierung", cut.Find("[role=toolbar]").GetAttribute("aria-label"));
        Assert.Equal("Fett", cut.Find("button[data-mde-cmd=bold]").GetAttribute("aria-label"));
        Assert.Equal("Verweis", cut.Find("button[data-mde-cmd=link]").GetAttribute("aria-label"));
        Assert.Equal("Vorschau", cut.Find(".mde-preview").GetAttribute("aria-label"));
        Assert.Equal("Nichts anzuzeigen", cut.Find(".mde-preview-empty").TextContent);
    }

    // ── AI ─────────────────────────────────────────────────────────────

    [Fact]
    public void Without_an_AI_state_the_editor_draws_no_aura()
    {
        var cut = RenderEditor();

        Assert.Empty(cut.FindAll(".ai-aura"));
        Assert.Empty(cut.FindAll(".ai-aura-ring"));
    }

    [Fact]
    public void An_AI_state_puts_one_aura_on_the_frame()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.Ai, AiState.Streaming));

        var frame = cut.Find(".mde");
        Assert.Contains("ai-aura", frame.ClassList);
        Assert.Contains("ai-streaming", frame.ClassList);
        Assert.Single(cut.FindAll(".ai-aura"));
    }

    [Fact]
    public void The_Aura_parameter_picks_the_variant()
    {
        var cut = RenderEditor(ps => ps.Add(p => p.Ai, AiState.Active).Add(p => p.Aura, AiAura.Aurora));

        Assert.Contains("ai-aura--aurora", cut.Find(".mde").ClassList);
    }

    [Fact]
    public void A_surrounding_scope_lights_the_frame_and_nothing_inside_it()
    {
        var cut = Render<DrylAiScope>(ps => ps
            .Add(p => p.State, AiState.Streaming)
            .AddChildContent<DrylMarkdownEditor>(e => e
                .Add(p => p.Value, "Some text")
                .Add(p => p.View, MarkdownEditorView.Preview)));

        var aura = Assert.Single(cut.FindAll(".ai-aura"));
        Assert.Contains("mde", aura.ClassList);
        Assert.Contains("ai-streaming", aura.ClassList);
        Assert.DoesNotContain("ai-aura", cut.Find(".mde-preview .md").ClassList);
    }

    [Fact]
    public void An_explicit_Ai_state_wins_over_the_scope()
    {
        var cut = Render<DrylAiScope>(ps => ps
            .Add(p => p.State, AiState.Streaming)
            .AddChildContent<DrylMarkdownEditor>(e => e.Add(p => p.Ai, AiState.Thinking)));

        var frame = cut.Find(".mde");
        Assert.Contains("ai-thinking", frame.ClassList);
        Assert.DoesNotContain("ai-streaming", frame.ClassList);
    }

    // ── Interop ────────────────────────────────────────────────────────

    [Fact]
    public async Task The_editing_script_is_attached_once_and_detached_once_on_disposal()
    {
        var cut = RenderEditor();
        cut.Render();

        Assert.Equal(1, Calls(Attach));
        Assert.Equal(0, Calls(Detach));

        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());

        Assert.Equal(1, Calls(Detach));
    }

    [Fact]
    public void The_toolbars_tab_stop_is_synced_when_the_commands_change_availability()
    {
        var cut = RenderEditor();
        cut.Render();
        Assert.Equal(0, Calls(Sync));

        Switch(cut, "Preview");
        Assert.Equal(1, Calls(Sync));

        Switch(cut, "Editor");
        Assert.Equal(2, Calls(Sync));
    }
}
