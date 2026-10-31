namespace DRYL.Components.Agents;

/// <summary>
/// Every human word a <see cref="DrylAiField"/> renders or announces of its own. The defaults are
/// English; replace what you translate with <c>AiFieldTexts.Default with { … }</c>.
/// The field's <see cref="DrylAiField.TriggerLabel"/>, <see cref="DrylAiField.CancelLabel"/>,
/// <see cref="DrylAiField.SendLabel"/>, <see cref="DrylAiField.PromptPlaceholder"/>,
/// <see cref="DrylAiField.AcceptLabel"/> and <see cref="DrylAiField.RejectLabel"/> win over the
/// matching members when set.
/// </summary>
public sealed record AiFieldTexts
{
    /// <summary>The English defaults.</summary>
    public static AiFieldTexts Default { get; } = new();

    /// <summary>Tooltip and accessible name of the ✨ trigger.</summary>
    public string Trigger { get; init; } = "Write with AI";

    /// <summary>Tooltip and accessible name of the trigger while a run can be cancelled.</summary>
    public string Cancel { get; init; } = "Cancel";

    /// <summary>Tooltip and accessible name of the mini-prompt's send button.</summary>
    public string Send { get; init; } = "Send";

    /// <summary>Placeholder and accessible name of the mini-prompt input.</summary>
    public string PromptPlaceholder { get; init; } = "What should the AI do?";

    /// <summary>Label of the accept chip.</summary>
    public string Accept { get; init; } = "Accept";

    /// <summary>Label of the reject chip.</summary>
    public string Reject { get; init; } = "Discard";

    /// <summary>Announced when a run starts.</summary>
    public string Writing { get; init; } = "The AI is writing…";

    /// <summary>Announced when a suggestion is ready and waits for a decision.</summary>
    public string Ready { get; init; } = "AI suggestion ready — accept or discard it.";

    /// <summary>Announced when the user accepts the suggestion.</summary>
    public string Accepted { get; init; } = "Suggestion accepted.";

    /// <summary>Announced when the user rejects the suggestion or cancels a run.</summary>
    public string Rejected { get; init; } = "Suggestion discarded.";

    /// <summary>Announced when the run fails and the field is restored.</summary>
    public string Failed { get; init; } = "The AI request failed.";
}
