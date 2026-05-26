using System;

namespace DRYL.Components.Toasts;

/// <summary>
/// Handle to a toast that has been shown via <see cref="IDrylToastService"/>.
/// Use to close it programmatically or update its AI state mid-flight.
/// </summary>
public interface IDrylToastReference
{
    /// <summary>Stable id of this toast instance.</summary>
    Guid Id { get; }

    /// <summary>Close the toast (triggers the exit animation, then removes it).</summary>
    void Close();

    /// <summary>Update the toast's AI state and re-render.</summary>
    void SetAi(AiState state);

    /// <summary>Raised once after the toast has finished its exit animation and been removed.</summary>
    event Action<IDrylToastReference>? OnClosed;
}
