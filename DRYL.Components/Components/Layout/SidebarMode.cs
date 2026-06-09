namespace DRYL.Components;

/// <summary>
/// Operating mode of a <see cref="DrylDrawer"/> sidebar. Controls how the drawer
/// occupies space and how the user collapses or dismisses it.
/// </summary>
public enum SidebarMode
{
    /// <summary>
    /// Flyout overlay on small screens, in-flow column on large ones (the historical
    /// default). The grid column appears at desktop widths; below ~1024px it becomes
    /// an off-canvas overlay toggled by <see cref="DrylDrawer.Open"/>.
    /// </summary>
    Auto,

    /// <summary>Always an in-flow column. Never overlays content and is never dismissable.</summary>
    Static,

    /// <summary>
    /// In-flow column that the user can collapse to an icon-only rail via
    /// <see cref="DrylDrawer.Collapsed"/> (<c>@bind-Collapsed</c>).
    /// </summary>
    Collapsible,

    /// <summary>
    /// Like <see cref="Collapsible"/>, but the collapsed state is persisted to
    /// <c>localStorage</c> under <see cref="DrylDrawer.PersistStateKey"/> and restored
    /// on the next visit.
    /// </summary>
    Pinnable,

    /// <summary>
    /// Always an overlay (at every viewport width). Slides in over the content with a
    /// backdrop; closes on backdrop click or <kbd>Escape</kbd> and traps focus while open.
    /// </summary>
    Flyout
}
