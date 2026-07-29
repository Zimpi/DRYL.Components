using Microsoft.JSInterop;

namespace DRYL.Components.Tests.Agents.Voice;

/// <summary>
/// A JS runtime that does nothing. The voice runner needs one to be constructed, but every test
/// in this folder exercises the C# side — the browser half has no test double worth writing,
/// because what it does is hold a WebRTC peer connection.
/// </summary>
internal sealed class NoopJsRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier, CancellationToken cancellationToken, object?[]? args) => default;
}
