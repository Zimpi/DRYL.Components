using Microsoft.AspNetCore.Components;

namespace DRYL.Components;

/// <summary>Represents a single entry in a <see cref="DrylCommandPalette"/> result list.</summary>
public sealed class CommandItem
{
    /// <summary>
    /// Stable identity of the entry across searches. A row keeps its place and its element
    /// while a later result set still contains an item with the same <see cref="Id"/>; a new id
    /// enters, a missing one leaves. When <c>null</c>, the palette keys the row by its
    /// <see cref="Category"/> and <see cref="Label"/>.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>Primary display label shown in the result row.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Secondary description text rendered below the label.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Short secondary fact rendered muted at the trailing end of the row, before the arrow —
    /// "12 in stock", "yesterday". Optional.
    /// </summary>
    public string? Meta { get; set; }

    /// <summary>Lucide icon name passed to <see cref="DrylIcon"/>. Optional.</summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Group header label. Items sharing the same Category value are clustered under one header.
    /// Items with a null Category form an implicit group without a header, rendered last under
    /// <see cref="CommandGroupOrder.Alphabetical"/>.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>Controls whether the item navigates, executes an action callback, or triggers an AI intent.</summary>
    public CommandItemType Type { get; set; } = CommandItemType.Action;

    /// <summary>Target URL for <see cref="CommandItemType.Navigate"/> items.</summary>
    public string? Href { get; set; }

    /// <summary>Callback fired for <see cref="CommandItemType.Action"/> items. The palette closes before invocation.</summary>
    public EventCallback Action { get; set; }

    /// <summary>
    /// Callback fired for <see cref="CommandItemType.AiIntent"/> items.
    /// The palette stays open so the AI result panel can display the response;
    /// the consumer is responsible for driving the <see cref="DrylCommandPalette.Ai"/> parameter.
    /// </summary>
    public EventCallback AiAction { get; set; }
}

/// <summary>Determines how a <see cref="CommandItem"/> is executed when selected.</summary>
public enum CommandItemType
{
    /// <summary>Client-side navigation via <see cref="CommandItem.Href"/>. Palette closes on execution.</summary>
    Navigate,

    /// <summary>Fires <see cref="CommandItem.Action"/>. Palette closes on execution.</summary>
    Action,

    /// <summary>Fires <see cref="CommandItem.AiAction"/> and keeps the palette open for the AI result panel.</summary>
    AiIntent
}
