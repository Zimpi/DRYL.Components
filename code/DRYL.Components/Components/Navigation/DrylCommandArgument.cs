using Microsoft.AspNetCore.Components;

namespace DRYL.Components;

/// <summary>A declarative argument of a <see cref="DrylCommand"/>. Renders no DOM of its own: it
/// contributes (a) a manual input shown in the palette when the command is run with arguments, and
/// (b) one property in the AI tool's JSON schema.</summary>
public sealed class DrylCommandArgument : ComponentBase
{
    /// <summary>The owning command (cascaded). Internal — set by the framework.</summary>
    [CascadingParameter] internal DrylCommand? Parent { get; set; }

    /// <summary>The argument name (the schema property key and <see cref="CommandContext"/> key). Required.</summary>
    [Parameter, EditorRequired] public string Name { get; set; } = string.Empty;

    /// <summary>Human/model-facing description of the argument.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>The input/schema type. Defaults to <see cref="CommandArgType.Text"/>.</summary>
    [Parameter] public CommandArgType Type { get; set; } = CommandArgType.Text;

    /// <summary>Whether the argument must be provided.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Allowed values when <see cref="Type"/> is <see cref="CommandArgType.Choice"/>.</summary>
    [Parameter] public string[]? Options { get; set; }

    protected override void OnInitialized() => Parent?.AddArgument(this);
}
