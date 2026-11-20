using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// The <c>Texts</c> criteria of <c>specs/E8 Inputs/F3 DrylSelect.md</c> (idea I52): the two
/// words the select renders of its own come from <see cref="SelectTexts"/>. JSInterop is
/// Loose because the popover and the key helper wire themselves through dryl.js.
/// </summary>
public class DrylSelectTextsTests : BunitContext
{
    private static readonly SelectItem[] Envs = [new("prod", "Production"), new("dev", "Development")];

    private static readonly SelectTexts German = SelectTexts.Default with
    {
        NoOptions = "Keine Optionen",
        ListLabel = label => string.IsNullOrEmpty(label) ? "Optionen" : $"Optionen für {label}",
    };

    public DrylSelectTextsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private IRenderedComponent<DrylSelect> RenderOpen(
        IEnumerable<SelectItem> items, string? label, SelectTexts? texts = null)
    {
        var cut = Render<DrylSelect>(ps =>
        {
            ps.Add(p => p.Items, items).Add(p => p.Label, label).Add(p => p.AriaLabel, "Environment");
            if (texts is not null) ps.Add(p => p.Texts, texts);
        });
        cut.InvokeAsync(() => cut.Find("[role=combobox]").Click()).GetAwaiter().GetResult();
        return cut;
    }

    [Fact]
    public void The_defaults_are_the_English_words_the_select_always_had()
    {
        Assert.Equal("Environment options",
            RenderOpen(Envs, "Environment").Find("[role=listbox]").GetAttribute("aria-label"));

        var empty = RenderOpen([], null);
        Assert.Equal("Options", empty.Find("[role=listbox]").GetAttribute("aria-label"));
        Assert.Equal("No options", empty.Find(".select-empty").TextContent);
    }

    [Fact]
    public void Texts_translates_the_list_name_and_the_empty_row()
    {
        Assert.Equal("Optionen für Umgebung",
            RenderOpen(Envs, "Umgebung", German).Find("[role=listbox]").GetAttribute("aria-label"));

        var empty = RenderOpen([], null, German);
        Assert.Equal("Optionen", empty.Find("[role=listbox]").GetAttribute("aria-label"));
        Assert.Equal("Keine Optionen", empty.Find(".select-empty").TextContent);
    }
}
