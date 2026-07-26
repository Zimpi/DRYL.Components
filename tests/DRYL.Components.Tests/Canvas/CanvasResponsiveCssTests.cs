namespace DRYL.Components.Tests.Canvas;

/// <summary>
/// Guards the responsive contract of the canvas surface (Sidequest R): the donut sizes to
/// min(height, available width) instead of a fixed 260px square, and .canvas-body is a NAMED
/// container context so canvas rules react to the canvas width, not the viewport.
/// Layout itself is verified with Playwright; these tests only stop the rules from being
/// silently refactored away.
/// </summary>
public class CanvasResponsiveCssTests
{
    private static string ReadCss(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            dir = dir.Parent;
        }
        throw new FileNotFoundException(string.Join('/', parts) + " not found from " + AppContext.BaseDirectory);
    }

    private static string ReadDrylCss() => ReadCss("DRYL.Components", "wwwroot", "dryl.css");

    private static string ReadCanvasCss() =>
        ReadCss("DRYL.Components", "Components", "AI", "DrylCanvas.razor.css");

    [Fact]
    public void Donut_root_is_a_query_container()
    {
        // The box cannot query itself — the container-type has to sit on the chart root.
        Assert.Contains(".chart-kind-donut { container-type: inline-size; }", ReadDrylCss());
    }

    [Fact]
    public void Donut_box_is_capped_by_its_available_width()
    {
        Assert.Contains("height: min(var(--chart-h, 260px), 100cqw);", ReadDrylCss());
    }
}
