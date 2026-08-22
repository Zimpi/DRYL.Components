namespace DRYL.Components.Motion;

/// <summary>
/// Animates a Blazor state change as a <b>morph</b>: elements that exist before and
/// after the change travel from where they were to where they now are, instead of
/// disappearing and reappearing.
/// </summary>
/// <remarks>
/// <para>The technique is FLIP — <b>F</b>irst (measure), <b>L</b>ast (measure again after
/// the render), <b>I</b>nvert (put each target back on its old geometry), <b>P</b>lay
/// (animate that away). The page stays live throughout and only the elements that
/// actually moved are animated.</para>
/// <para><b>Contract for consuming components:</b> the mutate delegate must end with
/// <c>StateHasChanged()</c>, and the component must report its render back so the
/// engine knows when to measure the new geometry:</para>
/// <code>
/// protected override void OnAfterRender(bool firstRender) => _morph.SignalRendered();
/// </code>
/// <para><see cref="DrylMorph"/> does that reporting for its consumers, so a page built
/// from morph hulls never writes that line. One morph runs at a time per service
/// instance; the service is registered scoped via <c>AddDrylComponents()</c>.</para>
/// <para>Targets announce themselves through the DOM (<c>data-dryl-morph</c>), which is
/// what <see cref="DrylMorph"/> renders — nothing has to be registered here.</para>
/// </remarks>
public interface IDrylMorph
{
    /// <summary>Runs <paramref name="mutate"/> (which must call <c>StateHasChanged()</c>)
    /// and animates every morph target that moved or resized as a result.</summary>
    Task RunAsync(Action mutate);

    /// <summary>Async-mutation overload of <see cref="RunAsync(Action)"/>. Keep the work
    /// short — the morph cannot start until the new state has rendered.</summary>
    Task RunAsync(Func<Task> mutate);

    /// <summary>Reports that a consuming component's <c>OnAfterRender</c> fired, i.e. the
    /// mutated state has reached the DOM. Call this unconditionally from
    /// <c>OnAfterRender</c> — it is a cheap no-op when no morph is in flight.</summary>
    void SignalRendered();

    /// <summary>
    /// Starts a morph that a <b>coming navigation</b> completes, rather than one this
    /// service mutates itself. Used by <c>DrylRouteTransition</c> from a
    /// location-changing handler: the old geometry is measured here, and the morph is
    /// played once the destination page has rendered.
    /// </summary>
    /// <param name="timeout">How long to wait for the destination to report a render
    /// before giving up on the morph. The navigation itself is never delayed or blocked
    /// by this — a page that never reports simply arrives without a morph.</param>
    Task BeginNavigationAsync(TimeSpan timeout);
}
