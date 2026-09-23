using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace DRYL.Components.Tests;

/// <summary>
/// The drop zone's texts are parameters with the old English wording as defaults, so a
/// German host can use the component instead of rebuilding it — and its icons are real ones.
/// </summary>
public class DrylFileUploadTests : BunitContext
{
    public DrylFileUploadTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private static void Upload(IRenderedComponent<DrylFileUpload> cut, params string[] names) =>
        cut.FindComponent<InputFile>().UploadFiles(
            names.Select(n => InputFileContent.CreateFromText("x", n)).ToArray());

    [Fact]
    public void Defaults_keep_the_english_wording()
    {
        var cut = Render<DrylFileUpload>(ps => ps.Add(p => p.Multiple, true));
        Upload(cut, "report.pdf");

        Assert.Equal("Drag & drop or click to browse", cut.Find(".file-drop-title").TextContent);
        Assert.Equal("max 10.0 MB", cut.Find(".file-drop-sub").TextContent);
        Assert.Equal("Remove report.pdf", cut.Find(".file-item-remove").GetAttribute("aria-label"));
    }

    [Fact]
    public void Title_SubText_and_RemoveLabel_replace_the_defaults()
    {
        var cut = Render<DrylFileUpload>(ps => ps
            .Add(p => p.Title, "Dateien hierher ziehen")
            .Add(p => p.SubText, "PDF, höchstens 10 MB")
            .Add(p => p.RemoveLabel, name => $"{name} entfernen"));
        Upload(cut, "bericht.pdf");

        Assert.Equal("Dateien hierher ziehen", cut.Find(".file-drop-title").TextContent);
        Assert.Equal("PDF, höchstens 10 MB", cut.Find(".file-drop-sub").TextContent);
        Assert.Equal("bericht.pdf entfernen", cut.Find(".file-item-remove").GetAttribute("aria-label"));
    }

    [Fact]
    public void An_empty_SubText_hides_the_hint_line()
    {
        var cut = Render<DrylFileUpload>(ps => ps.Add(p => p.SubText, ""));

        Assert.Empty(cut.FindAll(".file-drop-sub"));
    }

    [Fact]
    public void ShowFileList_false_renders_no_list_but_still_reports_the_files()
    {
        IReadOnlyList<IBrowserFile>? reported = null;
        var cut = Render<DrylFileUpload>(ps => ps
            .Add(p => p.ShowFileList, false)
            .Add(p => p.FilesChanged, files => reported = files));
        Upload(cut, "a.txt");

        Assert.Empty(cut.FindAll(".file-list"));
        Assert.Single(reported!);
    }

    /// <summary>
    /// The native input is rendered by InputFile, a child component, so it never carries this
    /// component's scope attribute — a plain scoped selector left it visible as a browser file
    /// button inside the zone. The rule has to go through ::deep. Layout itself was checked in
    /// the browser; this only stops the rule from being refactored back.
    /// </summary>
    [Fact]
    public void The_overlay_rule_reaches_the_child_components_input()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? css = null;
        while (dir is not null && css is null)
        {
            var candidate = Path.Combine(dir.FullName, "code", "DRYL.Components", "Components", "Inputs", "DrylFileUpload.razor.css");
            if (File.Exists(candidate)) css = File.ReadAllText(candidate);
            dir = dir.Parent;
        }

        Assert.NotNull(css);
        Assert.Contains(".file-drop ::deep .file-input-overlay {", css);
    }

    [Fact]
    public void The_drop_zone_and_a_pdf_row_draw_real_icons()
    {
        var cut = Render<DrylFileUpload>();
        Upload(cut, "report.pdf");

        Assert.NotEmpty(cut.Find("svg.file-drop-icon").InnerHtml.Trim());
        Assert.NotEmpty(cut.Find("svg.file-item-icon").InnerHtml.Trim());
    }
}
