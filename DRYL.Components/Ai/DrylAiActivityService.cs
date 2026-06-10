using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DRYL.Components.Ai;

/// <summary>
/// Default <see cref="IDrylAiActivityService"/> implementation. Registered as scoped
/// (one per Blazor circuit) via <c>AddDrylComponents()</c>. Holds no timers — settling
/// from <see cref="AiState.Generated"/> is the UI layer's job (see <c>DrylAiScope</c> /
/// <c>DrylAiStream</c>) so the one-shot reveal renders before the state relaxes.
/// </summary>
internal sealed class DrylAiActivityService : IDrylAiActivityService
{
    private readonly Dictionary<string, AiState> _states = new();

    public event Action<string>? OnChanged;

    public AiState GetState(string key) =>
        _states.TryGetValue(key, out var state) ? state : AiState.None;

    public void Set(string key, AiState state)
    {
        if (GetState(key) == state) return;

        if (state == AiState.None) _states.Remove(key);
        else _states[key] = state;

        OnChanged?.Invoke(key);
    }

    public void Clear(string key) => Set(key, AiState.None);

    public IDrylAiOperation Begin(string key, AiState initial = AiState.Thinking)
    {
        Set(key, initial);
        return new Operation(this, key);
    }

    public async Task StreamAsync(
        string key,
        IAsyncEnumerable<string> tokens,
        Action<string> onToken,
        CancellationToken ct = default)
    {
        Set(key, AiState.Thinking);
        var sawToken = false;
        try
        {
            await foreach (var token in tokens.WithCancellation(ct))
            {
                if (!sawToken)
                {
                    sawToken = true;
                    Set(key, AiState.Streaming);
                }
                onToken(token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the consumer cancels (e.g. navigation / restart).
            Clear(key);
            return;
        }
        catch
        {
            Clear(key);
            throw;
        }

        // Success: rest at Generated so the reveal can play; the UI layer settles it.
        Set(key, AiState.Generated);
    }

    private sealed class Operation : IDrylAiOperation
    {
        private readonly DrylAiActivityService _service;
        private bool _disposed;

        public string Key { get; }

        public Operation(DrylAiActivityService service, string key)
        {
            _service = service;
            Key = key;
        }

        public void Thinking()  => _service.Set(Key, AiState.Thinking);
        public void Streaming() => _service.Set(Key, AiState.Streaming);
        public void Generated() => _service.Set(Key, AiState.Generated);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _service.Clear(Key);
        }
    }
}
