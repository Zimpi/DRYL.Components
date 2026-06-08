using System;

namespace DRYL.Components;

/// <summary>
/// A single entry in the <see cref="DrylNotifications"/> inbox. Mutable so the read state can be
/// toggled in place; identity is the <see cref="Id"/>.
/// </summary>
public sealed class DrylNotification
{
    /// <summary>Stable identifier. Defaults to a new GUID; set it yourself to de-duplicate.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Headline shown in bold.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional supporting line under the title.</summary>
    public string? Message { get; set; }

    /// <summary>Optional <see cref="DrylIcon"/> name shown in the leading chip.</summary>
    public string? Icon { get; set; }

    /// <summary>When the notification occurred. Drives the relative "x ago" label.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    /// <summary>Read state. Unread entries show an accent dot and count toward the bell badge.</summary>
    public bool Read { get; set; }

    /// <summary>
    /// AI provenance. When not <see cref="AiState.None"/> the entry carries the shared AI aura —
    /// ideal for asynchronous AI results ("Your report was generated", "Agent task finished").
    /// </summary>
    public AiState Ai { get; set; } = AiState.None;
}
