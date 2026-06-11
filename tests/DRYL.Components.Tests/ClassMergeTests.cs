using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Pins the DRYL class-composition contract for the 1.0 API freeze. A
/// consumer-supplied <c>class</c> must be <em>merged</em> with a component's own
/// identity classes, never override them. The mechanism is a typed <c>Class</c>
/// parameter: Blazor matches attribute names case-insensitively, so a consumer's
/// <c>class="x"</c> binds to <c>Class</c> (not to <c>AdditionalAttributes</c>) and
/// the component folds it into its computed class string. A bare
/// <c>@attributes</c> splat would instead clobber the explicit <c>class</c>, so
/// every consumer-facing component carries a merged <c>Class</c> parameter.
/// </summary>
public class ClassMergeTests : BunitContext
{
    [Fact]
    public void Button_merges_consumer_class_without_clobbering_identity_classes()
    {
        var cut = Render<DrylButton>(ps => ps
            .AddUnmatched("class", "mt-4")
            .AddChildContent("Save"));

        var classes = (cut.Find("button").GetAttribute("class") ?? string.Empty)
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("btn", classes);          // identity class survives
        Assert.Contains("btn-primary", classes);  // variant class survives
        Assert.Contains("mt-4", classes);         // consumer class merged in
    }

    [Fact]
    public void Button_typed_Class_parameter_is_merged()
    {
        var cut = Render<DrylButton>(ps => ps
            .Add(p => p.Class, "danger-zone")
            .AddChildContent("Delete"));

        var classes = (cut.Find("button").GetAttribute("class") ?? string.Empty)
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("btn", classes);
        Assert.Contains("danger-zone", classes);
    }

    [Fact]
    public void SegmentedControl_merges_consumer_class_without_clobbering()
    {
        var cut = Render<DrylSegmentedControl<string>>(ps => ps
            .Add(p => p.AriaLabel, "View")
            .AddUnmatched("class", "my-extra"));

        var classes = (cut.Find("[role=radiogroup]").GetAttribute("class") ?? string.Empty)
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("seg", classes);
        Assert.Contains("seg--md", classes);
        Assert.Contains("my-extra", classes);
    }

    [Fact]
    public void ButtonGroup_merges_consumer_class_without_clobbering()
    {
        var cut = Render<DrylButtonGroup>(ps => ps.AddUnmatched("class", "x"));
        var classes = (cut.Find("[role=group]").GetAttribute("class") ?? string.Empty)
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("btn-group", classes);
        Assert.Contains("x", classes);
    }

    [Fact]
    public void AiIndicator_merges_consumer_class_without_clobbering()
    {
        var cut = Render<DrylAiIndicator>(ps => ps.AddUnmatched("class", "x"));
        var classes = (cut.Find("[role=status]").GetAttribute("class") ?? string.Empty)
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("ai-indicator", classes);
        Assert.Contains("x", classes);
    }

    [Fact]
    public void ToolCall_merges_consumer_class_without_clobbering()
    {
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "get_weather")
            .AddUnmatched("class", "x"));
        var classes = (cut.Find("div.tool-call").GetAttribute("class") ?? string.Empty)
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("tool-call", classes);
        Assert.Contains("x", classes);
    }
}
