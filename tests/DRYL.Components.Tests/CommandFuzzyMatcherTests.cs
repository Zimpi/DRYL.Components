using DRYL.Components;

namespace DRYL.Components.Tests;

public class CommandFuzzyMatcherTests
{
    private static DrylCommand Cmd(string title, bool destructive = false,
        bool disabled = false, string[]? keywords = null)
        => new() { Title = title, Destructive = destructive, Disabled = disabled, Keywords = keywords };

    [Fact]
    public void Empty_query_returns_all_up_to_max()
    {
        var list = new[] { Cmd("Alpha"), Cmd("Beta"), Cmd("Gamma") };
        var r = CommandFuzzyMatcher.Match(list, "", 2);
        Assert.Equal(2, r.Count);
    }

    [Fact]
    public void Subsequence_match_on_title()
    {
        var list = new[] { Cmd("Neue Rechnung"), Cmd("Status setzen") };
        var r = CommandFuzzyMatcher.Match(list, "rech", 8);
        Assert.Single(r);
        Assert.Equal("Neue Rechnung", r[0].Title);
    }

    [Fact]
    public void Matches_keywords()
    {
        var list = new[] { Cmd("Status setzen", keywords: new[] { "bezahlt", "paid" }) };
        var r = CommandFuzzyMatcher.Match(list, "paid", 8);
        Assert.Single(r);
    }

    [Fact]
    public void Destructive_sorts_after_equal_non_destructive()
    {
        var safe = Cmd("Delete safe");
        var danger = Cmd("Delete danger", destructive: true);
        var r = CommandFuzzyMatcher.Match(new[] { danger, safe }, "delete", 8);
        Assert.Equal("Delete safe", r[0].Title);
    }
}
