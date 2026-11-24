namespace DRYL.Components.Tests;

/// <summary>
/// <see cref="DrylMainContent"/> (spec <c>F9</c>): the page padding lives on <c>.main-inner</c>
/// in dryl.css. What it computes to is verified in the browser; here: the cascade contract.
/// </summary>
public class DrylMainContentTests
{
    [Fact]
    public void The_smaller_padding_below_the_desktop_breakpoint_follows_the_base_rule()
    {
        // Equal specificity: the phone padding has to come later, or the base rule's
        // 48px gutter covers it on every phone.
        var css = File.ReadAllText(Path.Combine(RepoRoot(), "code", "DRYL.Components", "wwwroot", "dryl.css")).ReplaceLineEndings("\n");
        var baseRule = css.IndexOf(".main-inner {\n  max-width:", StringComparison.Ordinal);
        var phoneRule = css.IndexOf(".main-inner { padding: var(--sp-5) var(--sp-4) var(--sp-8); }", StringComparison.Ordinal);

        Assert.True(baseRule >= 0, "base .main-inner rule not found");
        Assert.True(phoneRule > baseRule, "the phone padding must follow the base .main-inner rule");
        Assert.Equal(1, CountOf(css, ".main-inner { padding:"));
    }

    private static int CountOf(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + 1, StringComparison.Ordinal)) count++;
        return count;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DRYL.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("DRYL.slnx not found above the test directory.");
    }
}
