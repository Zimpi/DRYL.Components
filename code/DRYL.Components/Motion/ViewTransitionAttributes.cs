namespace DRYL.Components.Motion;

/// <summary>
/// Builds the two attribute values that make an element a shared-element morph
/// target: the inline <c>view-transition-name</c> (plus the
/// <c>view-transition-class</c> of the <see cref="DrylViewTransitionStyle.DepthGlass"/>
/// tier) and the <c>data-vt-depth</c> marker the JS bridge keys on to inject the
/// merge filter lazily.
/// </summary>
/// <remarks>
/// One place, so <c>DrylMorph</c> and <c>DrylCard</c> cannot drift apart on what
/// a morph target looks like. The morph's own duration, easing and filter live
/// in the <c>::view-transition-*</c> rules in <c>dryl.css</c> — nothing here
/// names a value.
/// </remarks>
internal static class ViewTransitionAttributes
{
    /// <summary>True when <paramref name="name"/> claims a transition name and the
    /// <see cref="DrylViewTransitionStyle.DepthGlass"/> tier was asked for.</summary>
    public static bool IsDepth(string? name, DrylViewTransitionStyle style) =>
        !string.IsNullOrWhiteSpace(name) && style == DrylViewTransitionStyle.DepthGlass;

    /// <summary>The inline style claiming <paramref name="name"/>, or null when the
    /// element claims no name and should stay inert.</summary>
    public static string? Style(string? name, DrylViewTransitionStyle style) =>
        string.IsNullOrWhiteSpace(name)
            ? null
            : IsDepth(name, style)
                ? $"view-transition-name: {name}; view-transition-class: dryl-depth"
                : $"view-transition-name: {name}";

    /// <summary>The <c>data-vt-depth</c> marker value — an empty attribute on the
    /// DepthGlass tier, and null (so Blazor omits the attribute) otherwise.</summary>
    public static string? DepthMarker(string? name, DrylViewTransitionStyle style) =>
        IsDepth(name, style) ? "" : null;
}
