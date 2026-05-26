using System;

namespace DRYL.Components.Dialogs;

/// <summary>
/// Cascading value exposed by <see cref="DRYL.Components.DrylDialogProvider"/> to the
/// dialog component. Lets the dialog close itself or update its AI state at runtime.
/// </summary>
/// <example>
/// <code>
/// [CascadingParameter] IDrylDialogInstance Instance { get; set; } = default!;
/// void Save() => Instance.Close(DialogResult.Ok(_value));
/// </code>
/// </example>
public interface IDrylDialogInstance
{
    /// <summary>Stable id of this dialog instance.</summary>
    Guid Id { get; }

    /// <summary>Title supplied at <c>ShowAsync</c> time. May be overridden by the dialog's own header content.</summary>
    string? Title { get; }

    /// <summary>The options the dialog was opened with.</summary>
    DialogOptions Options { get; }

    /// <summary>Current AI state of the dialog frame. Mirrors <see cref="DialogOptions.Ai"/> initially.</summary>
    AiState Ai { get; }

    /// <summary>Close the dialog with the given result.</summary>
    void Close(DialogResult result);

    /// <summary>Cancel the dialog (equivalent to <c>Close(DialogResult.Cancel())</c>).</summary>
    void Cancel();

    /// <summary>Update the AI state of the dialog frame and re-render.</summary>
    void SetAi(AiState state);
}
