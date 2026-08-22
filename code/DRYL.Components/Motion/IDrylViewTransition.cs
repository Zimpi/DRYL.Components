namespace DRYL.Components.Motion;

/// <summary>
/// Runs a Blazor state change inside a same-document
/// <see href="https://developer.mozilla.org/docs/Web/API/View_Transition_API">View Transition</see>,
/// so elements carrying a <c>view-transition-name</c> morph (position, size, opacity)
/// to their new state instead of snapping. Falls back to applying the change
/// directly — no snapshot, no morph — in browsers without the API, during
/// prerender, and when the user prefers reduced motion.
/// </summary>
/// <remarks>
/// <para><b>Contract for consuming components:</b> the mutate delegate must end with
/// <c>StateHasChanged()</c>, and the component must report its render back so the
/// browser knows when to take the "new" snapshot:</para>
/// <code>
/// protected override void OnAfterRender(bool firstRender) => _viewTransition.SignalRendered();
/// </code>
/// <para>One transition runs at a time per service instance; the service is
/// registered scoped via <c>AddDrylComponents()</c>.</para>
/// </remarks>
public interface IDrylViewTransition
{
    /// <summary>Runs <paramref name="mutate"/> (which must call <c>StateHasChanged()</c>)
    /// inside a view transition and completes when the morph has finished.</summary>
    Task RunAsync(Action mutate);

    /// <summary>Async-mutation overload of <see cref="RunAsync(Action)"/>. Keep the work
    /// synchronous where possible — the browser holds rendering while it waits.</summary>
    Task RunAsync(Func<Task> mutate);

    /// <summary>Reports that the consuming component's <c>OnAfterRender</c> fired, i.e.
    /// the mutated state has reached the DOM. Call this unconditionally from
    /// <c>OnAfterRender</c> — it is a cheap no-op when no transition is in flight.</summary>
    void SignalRendered();

    /// <summary>
    /// Starts a transition that a <b>coming navigation</b> completes, rather than one
    /// this service mutates itself. Used by <c>DrylRouteTransition</c> from a
    /// location-changing handler: the old snapshot is taken here, and the transition
    /// is held open until a <c>DrylMorph</c> on the destination page reports its
    /// render through <see cref="SignalRendered"/>.
    /// </summary>
    /// <param name="timeout">How long the old frame may be held before the transition
    /// completes without a morph. A destination page that carries no <c>DrylMorph</c>,
    /// or never finishes rendering, must not leave the user looking at a held frame.</param>
    /// <remarks>
    /// <para>Unlike <see cref="RunAsync(Action)"/> this does not await the morph — the
    /// navigation must not be delayed by it, and awaiting here would deadlock: the
    /// transition waits for the new page's render, which cannot happen until the
    /// handler returns.</para>
    /// <para>Ships with a do-nothing default implementation, so an existing implementer
    /// of this interface keeps compiling and simply never morphs a navigation.</para>
    /// </remarks>
    void BeginNavigation(TimeSpan timeout) { }
}
