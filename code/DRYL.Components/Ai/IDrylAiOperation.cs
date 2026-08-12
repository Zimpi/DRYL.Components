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
}
