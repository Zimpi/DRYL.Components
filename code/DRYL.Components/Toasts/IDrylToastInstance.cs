using System;

namespace DRYL.Components.Toasts;

/// <summary>
/// Cascading value exposed by <see cref="DRYL.Components.DrylToastProvider"/> to a
/// custom toast body component. Lets that component close its own toast or update its
/// AI state at runtime.
/// </summary>
/// <example>
/// <code>
/// [CascadingParameter] IDrylToastInstance Instance { get; set; } = default!;
/// void Dismiss() => Instance.Close();
/// </code>
/// </example>
public interface IDrylToastInstance
{
    /// <summary>Stable id of this toast instance.</summary>
    Guid Id { get; }

    /// <summary>The options the toast was opened with.</summary>
    ToastOptions Options { get; }

    /// <summary>The toast's message text (when created via the string overloads).</summary>
    string? Message { get; }

    /// <summary>Current AI state. Mirrors <see cref="ToastOptions.Ai"/> initially.</summary>
    AiState Ai { get; }

    /// <summary>Close the toast (triggers the exit animation, then removes it).</summary>
    void Close();

    /// <summary>Update the AI state and re-render.</summary>
    void SetAi(AiState state);
}
