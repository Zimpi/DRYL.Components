using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DRYL.Components.Dialogs;

/// <summary>
/// Concrete dialog handle. Implements both <see cref="IDrylDialogReference"/>
/// (caller-facing) and <see cref="IDrylDialogInstance"/> (cascaded into the
/// dialog component).
/// </summary>
internal sealed class DrylDialogReference : IDrylDialogReference, IDrylDialogInstance
{
    private readonly TaskCompletionSource<DialogResult> _tcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly DrylDialogService _service;

    public Guid Id { get; } = Guid.NewGuid();
    public string? Title { get; }
    public DialogOptions Options { get; }
    public Type DialogType { get; }
    public IDictionary<string, object> Parameters { get; }

    public AiState Ai { get; private set; }

    public Task<DialogResult> Result => _tcs.Task;

    public DrylDialogReference(
        DrylDialogService service,
        Type dialogType,
        string? title,
        DialogParameters? parameters,
        DialogOptions options)
    {
        _service = service;
        DialogType = dialogType;
        Title = title;
        Options = options;
        Ai = options.Ai;
        Parameters = parameters?.ToDictionary() ?? new Dictionary<string, object>();
    }

    public void Close(DialogResult result)
    {
        if (_tcs.TrySetResult(result))
        {
            _service.NotifyClose(this);
        }
    }

    public void Cancel() => Close(DialogResult.Cancel());

    public void SetAi(AiState state)
    {
        if (Ai == state) return;
        Ai = state;
        _service.NotifyUpdated(this);
    }
}
