using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// <see cref="DrylButton.Element"/> — the rendered button a caller hands to
/// <c>ToastOptions.Origin</c> so a toast grows out of it (idea I34).
/// </summary>
public class DrylButtonElementTests : BunitContext
{
    [Fact]
    public void Element_refers_to_the_rendered_button_after_the_first_render()
    {
        var cut = Render<DrylButton>(ps => ps.AddChildContent("Save"));

        Assert.False(string.IsNullOrEmpty(cut.Instance.Element.Id));
    }
}
