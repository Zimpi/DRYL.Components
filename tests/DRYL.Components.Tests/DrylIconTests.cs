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
}
