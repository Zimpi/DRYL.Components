using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DRYL.Components.Ai;

/// <summary>
/// Coordinates <see cref="AiState"/> across DRYL components keyed by operation, so a
/// single AI flow can light up every surface that belongs to it. Patterned after the
/// other DRYL services (dialog, toast, notification): registered as scoped (one per
/// Blazor circuit) via <c>AddDrylComponents()</c>.
/// </summary>
/// <remarks>
/// <para>
/// Wrap a region of UI in <c>&lt;DrylAiScope Key="..."/&gt;</c>; AI-aware components inside
/// it inherit the state for that key automatically. Drive the state either imperatively
/// (<see cref="Begin(string, AiState)"/>) or by binding a token stream
/// (<see cref="StreamAsync(string, IAsyncEnumerable{string}, Action{string}, CancellationToken)"/>).
/// </para>
/// <para>
/// DRYL stays dependency-free: this service knows nothing about any LLM SDK. You bring the
/// token stream (e.g. from <c>Microsoft.Extensions.AI</c>) and DRYL maps it to the shared
/// AI visual vocabulary.
/// </para>
/// </remarks>
public interface IDrylAiActivityService
{
    /// <summary>
    /// Begin an operation on <paramref name="key"/> and return a disposable handle.
    /// Disposing the handle settles the key back to <see cref="AiState.None"/>.
    /// </summary>
    IDrylAiOperation Begin(string key, AiState initial = AiState.Thinking);

    /// <summary>Current state for <paramref name="key"/> (<see cref="AiState.None"/> if unknown).</summary>
    AiState GetState(string key);

    /// <summary>Force <paramref name="key"/> to <paramref name="state"/>. Raises <see cref="OnChanged"/> if it differs.</summary>
    void Set(string key, AiState state);

    /// <summary>Settle <paramref name="key"/> back to <see cref="AiState.None"/>.</summary>
    void Clear(string key);

    /// <summary>Raised whenever a key's state changes. The argument is the changed key.</summary>
    event Action<string>? OnChanged;

    /// <summary>
    /// Drive a token stream end-to-end: <see cref="AiState.Thinking"/> →
    /// <see cref="AiState.Streaming"/> (on the first token) → <see cref="AiState.Generated"/>
    /// (on completion). <paramref name="onToken"/> is invoked for each chunk. On cancellation
    /// or error the key is settled back to <see cref="AiState.None"/>; on success it rests at
    /// <see cref="AiState.Generated"/> so the reveal can play.
    /// </summary>
    Task StreamAsync(string key, IAsyncEnumerable<string> tokens,
                     Action<string> onToken, CancellationToken ct = default);
}
