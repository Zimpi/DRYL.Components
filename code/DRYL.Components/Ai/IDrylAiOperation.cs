using System;

namespace DRYL.Components.Ai;

/// <summary>
/// A handle to an in-flight AI operation tracked by <see cref="IDrylAiActivityService"/>.
/// Returned by <see cref="IDrylAiActivityService.Begin(string, AiState)"/>; advance the
/// state through the call sites of your flow, then <see cref="IDisposable.Dispose"/> it
/// (e.g. with <c>using</c>) to settle the key back to <see cref="AiState.None"/>.
/// </summary>
public interface IDrylAiOperation : IDisposable
{
    /// <summary>The key this operation drives. Components in a matching scope reflect its state.</summary>
    string Key { get; }

    /// <summary>Mark the operation as thinking (e.g. a tool call dispatched).</summary>
    void Thinking();

    /// <summary>Mark the operation as streaming tokens.</summary>
    void Streaming();

    /// <summary>
    /// Mark the operation as generated — the one-shot reveal. The state rests here until
    /// the operation is disposed or a new operation begins, so the reveal can play.
    /// </summary>
    void Generated();

    /// <summary>
    /// Mark the operation as proposed — the AI is done and a human has to accept or dismiss
    /// what it produced. The state rests here until the host resolves it: call
    /// <see cref="Generated"/> on accept, dispose the handle on dismiss. The library never
    /// ends it by itself.
    /// </summary>
    /// <remarks>
    /// The default implementation does nothing, so an implementation written before this
    /// member existed keeps compiling; the operation returned by
    /// <see cref="IDrylAiActivityService.Begin(string, AiState)"/> sets the key to
    /// <see cref="AiState.Proposed"/>.
    /// </remarks>
    void Proposed() { }
}
