using System;
using System.Collections.Generic;

namespace DRYL.Components.Toasts;

/// <summary>
/// Concrete toast handle. Implements both <see cref="IDrylToastReference"/>
/// (caller-facing) and <see cref="IDrylToastInstance"/> (cascaded into a custom toast body).
/// </summary>
internal sealed class DrylToastReference : IDrylToastReference, IDrylToastInstance
{
    private readonly DrylToastService _service;

    public Guid Id { get; } = Guid.NewGuid();
    public ToastOptions Options { get; }
    public string? Message { get; }
    public Type? BodyType { get; }
    public IDictionary<string, object>? BodyParameters { get; }

    public AiState Ai { get; private set; }
    public bool IsClosing { get; private set; }

    public event Action<IDrylToastReference>? OnClosed;

    public DrylToastReference(
        DrylToastService service,
        ToastOptions options,
        string? message,
        Type? bodyType = null,
        ToastParameters? bodyParameters = null)
    {
        _service = service;
        Options = options;
        Message = message;
        BodyType = bodyType;
        BodyParameters = bodyParameters?.ToDictionary();
        Ai = options.Ai;
    }

    public void Close()
    {
        if (IsClosing) return;
        IsClosing = true;
        _service.NotifyClose(this);
    }

    public void SetAi(AiState state)
    {
        if (Ai == state) return;
        Ai = state;
        _service.NotifyUpdated(this);
    }

    internal void RaiseClosed() => OnClosed?.Invoke(this);
}
