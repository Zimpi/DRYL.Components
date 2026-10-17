using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

/// <summary>
/// Where a <see cref="DrylToolInvocation"/> stands with a human. Set by the runner when the
/// framework asks for approval (a tool wrapped in <c>ApprovalRequiredAIFunction</c>), then by
/// <see cref="DrylRunBase.Approve"/> / <see cref="DrylRunBase.Deny"/>.
/// </summary>
public enum DrylToolApproval
{
    /// <summary>The call needed no approval.</summary>
    None,

    /// <summary>The framework asked for approval; the call has not run.</summary>
    Pending,

    /// <summary>A human approved it; the framework runs it when the run resumes.</summary>
    Approved,

    /// <summary>A human denied it, or the run was cancelled while it waited.</summary>
    Declined,
}

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

    /// <summary>Where the call stands with a human. <see cref="DrylToolApproval.None"/> unless the framework asked for approval.</summary>
    public DrylToolApproval Approval { get; set; } = DrylToolApproval.None;

    /// <summary>The framework's request, answered once every pending call of the response is decided.</summary>
    internal ToolApprovalRequestContent? ApprovalRequest { get; set; }

    /// <summary>
    /// Lifecycle mapped to the shared AI vocabulary: running → <see cref="AiState.Thinking"/>,
    /// completed → <see cref="AiState.Generated"/>, errored → <see cref="AiState.None"/>
    /// (the error is shown via the core component's danger alert). Awaiting approval →
    /// <see cref="AiState.Proposed"/>; approved → <see cref="AiState.Generated"/> straight away,
    /// so the card plays the accept wave; declined → <see cref="AiState.None"/>.
    /// </summary>
    public AiState State =>
        Error is not null ? AiState.None
        : Approval == DrylToolApproval.Declined ? AiState.None
        : Approval == DrylToolApproval.Pending ? AiState.Proposed
        : Approval == DrylToolApproval.Approved || Result is not null ? AiState.Generated
        : AiState.Thinking;

    /// <summary>
    /// The tool is executing: it was not declined or left pending, and neither its result nor
    /// an error is in. Feeds the core <c>DrylToolCall</c>'s <c>Running</c>, so an approved call —
    /// already <see cref="AiState.Generated"/> for its accept wave — reads "Running" until it is done.
    /// </summary>
    public bool Running =>
        Result is null && Error is null
        && Approval is DrylToolApproval.None or DrylToolApproval.Approved;
}
