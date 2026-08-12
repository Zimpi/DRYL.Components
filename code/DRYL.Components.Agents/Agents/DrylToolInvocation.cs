namespace DRYL.Components.Agents;

/// <summary>
/// A single agent tool / function call captured from an agent run. Its fields feed the
/// core <c>DrylToolCall</c> presentational component — name for name, except that
/// <see cref="State"/> is passed to that component's <c>Ai</c> parameter (renamed from
/// <c>State</c> in 2.22.0). <see cref="State"/> is derived from the call's lifecycle so
/// the UI shows the right AI vocabulary.
/// </summary>
public sealed class DrylToolInvocation
{
    /// <summary>The framework call id, used to match a result back to its call.</summary>
    public string CallId { get; set; } = string.Empty;

    /// <summary>The tool / function name the model invoked.</summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>The call arguments as JSON (shown in the collapsible body).</summary>
    public string? Arguments { get; set; }

    /// <summary>The call result as JSON; <c>null</c> until the result arrives.</summary>
    public string? Result { get; set; }

    /// <summary>An error message; when set, the call is rendered as failed.</summary>
    public string? Error { get; set; }

    /// <summary>
    /// Lifecycle mapped to the shared AI vocabulary: running → <see cref="AiState.Thinking"/>,
    /// completed → <see cref="AiState.Generated"/>, errored → <see cref="AiState.None"/>
    /// (the error is shown via the core component's danger alert).
    /// </summary>
    public AiState State =>
        Error is not null ? AiState.None
        : Result is not null ? AiState.Generated
        : AiState.Thinking;
}
