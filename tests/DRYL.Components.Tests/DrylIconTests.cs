using System.Text.RegularExpressions;
using Xunit;

namespace DRYL.Components.Tests;

/// <summary>The icon set is a public contract — a name a component uses has to be in it.</summary>
public class DrylIconTests
{
    [Theory]
    [InlineData("Undo")]
    [InlineData("Redo")]
    [InlineData("History")]
    public void The_history_icons_are_in_the_set(string name)
    {
        Assert.True(DrylIcon.Icons.ContainsKey(name));
        Assert.NotEmpty(DrylIcon.Icons[name]);
    }

    [Theory]
    [InlineData("Map")]
    [InlineData("MapPin")]
    [InlineData("Route")]
    [InlineData("Navigation")]
    [InlineData("Paperclip")]
    [InlineData("FileText")]
    [InlineData("UploadCloud")]
    public void The_map_route_attachment_and_upload_icons_are_in_the_set(string name)
    {
        Assert.True(DrylIcon.Icons.ContainsKey(name));
        Assert.NotEmpty(DrylIcon.Icons[name]);
    }

    /// <summary>
    /// An unknown name renders an empty <c>svg</c> and nothing reports it — that is how
    /// <c>DrylFileUpload</c>'s <c>UploadCloud</c> went unnoticed. So every literal
    /// <c>&lt;DrylIcon Name="…"&gt;</c> in the library's source is checked against the set.
    /// </summary>
    [Fact]
    public void Every_literal_icon_name_in_the_library_is_in_the_set()
    {
        var code = FindCodeDirectory();
        var pattern = new Regex("<DrylIcon[^>]*\\bName=\"(?<name>[A-Za-z]+)\"");

        var missing = Directory.EnumerateFiles(code.FullName, "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(f => pattern.Matches(File.ReadAllText(f))
                .Select(m => (File: Path.GetFileName(f), Name: m.Groups["name"].Value)))
            .Where(u => !DrylIcon.Icons.ContainsKey(u.Name))
            .Select(u => $"{u.File}: {u.Name}")
            .Distinct()
            .ToList();

        Assert.Empty(missing);
    }

    private static DirectoryInfo FindCodeDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = new DirectoryInfo(Path.Combine(dir.FullName, "code", "DRYL.Components"));
            if (candidate.Exists) return candidate.Parent!;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("code/ not found from " + AppContext.BaseDirectory);
    }
}
