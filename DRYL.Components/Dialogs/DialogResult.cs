namespace DRYL.Components.Dialogs;

/// <summary>
/// Outcome of a dialog. Either the user confirmed (with optional <see cref="Data"/>)
/// or canceled. Awaiting <see cref="IDrylDialogReference.Result"/> yields one of these.
/// </summary>
public sealed class DialogResult
{
    /// <summary>True if the dialog was dismissed without a confirmed outcome.</summary>
    public bool Canceled { get; }

    /// <summary>Payload returned by the dialog on confirmation. Null when canceled.</summary>
    public object? Data { get; }

    private DialogResult(bool canceled, object? data)
    {
        Canceled = canceled;
        Data = data;
    }

    /// <summary>Confirmed result with no payload.</summary>
    public static DialogResult Ok() => new(false, null);

    /// <summary>Confirmed result with a payload.</summary>
    public static DialogResult Ok(object? data) => new(false, data);

    /// <summary>Confirmed, strongly-typed result.</summary>
    public static DialogResult Ok<T>(T data) => new(false, data);

    /// <summary>Canceled result.</summary>
    public static DialogResult Cancel() => new(true, null);

    /// <summary>Try to read <see cref="Data"/> as <typeparamref name="T"/>.</summary>
    public T? DataAs<T>() => Data is T t ? t : default;
}
