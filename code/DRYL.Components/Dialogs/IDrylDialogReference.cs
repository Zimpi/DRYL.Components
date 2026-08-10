using System;
using System.Threading.Tasks;

namespace DRYL.Components.Dialogs;

/// <summary>
/// Handle to a dialog that has been shown via <see cref="IDrylDialogService"/>.
/// Await <see cref="Result"/> to receive the user's response.
/// </summary>
public interface IDrylDialogReference
{
    /// <summary>Stable id of this dialog instance.</summary>
    Guid Id { get; }

    /// <summary>Completes when the dialog closes (confirmed or canceled).</summary>
    Task<DialogResult> Result { get; }

    /// <summary>Programmatically close the dialog with the given result.</summary>
    void Close(DialogResult result);

    /// <summary>Programmatically cancel the dialog.</summary>
    void Cancel();
}
