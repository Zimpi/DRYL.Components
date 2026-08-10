using Microsoft.AspNetCore.Components;

namespace DRYL.Components;

/// <summary>A single unit of application functionality declared once and usable three ways — click,
/// keyboard <c>Enter</c> on a fuzzy match, and (when a resolver is supplied) an AI tool call — all
/// flowing through the one <see cref="OnRun"/> handler. Place inside a
/// <see cref="DrylCommandPalette"/>; it self-registers and hosts optional
/// <see cref="DrylCommandArgument"/> children.</summary>
public partial class DrylCommand : ComponentBase, IDisposable
{
    private readonly List<DrylCommandArgument> _arguments = new();
    private bool _registered;

    /// <summary>The ambient registry (cascaded by the palette). Internal — set by the framework.</summary>
    [CascadingParameter] internal ICommandRegistry? Registry { get; set; }

    /// <summary>Primary label and the tool's display/selection hint. Required.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>Secondary line in the palette and the tool description sent to the model.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Lucide icon name passed to <c>DrylIcon</c>.</summary>
    [Parameter] public string? Icon { get; set; }

    /// <summary>Section grouping in the palette.</summary>
    [Parameter] public string? Group { get; set; }

    /// <summary>Extra fuzzy-search aliases.</summary>
    [Parameter] public string[]? Keywords { get; set; }

    /// <summary>Display-only shortcut hint, e.g. "⌘N".</summary>
    [Parameter] public string? Shortcut { get; set; }

    /// <summary>De-prioritised in sort, styled as destructive, and forced through human-in-the-loop
    /// confirmation before running.</summary>
    [Parameter] public bool Destructive { get; set; }

    /// <summary>Greyed and unselectable in the palette; excluded from the AI tool list.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The one execution path. Receives a <see cref="CommandContext"/> carrying the resolved
    /// arguments (manual or AI-filled).</summary>
    [Parameter] public EventCallback<CommandContext> OnRun { get; set; }

    /// <summary>Stable id (schema/tool name and de-dup key). Auto-generated from the title if absent.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Hosts the command's <see cref="DrylCommandArgument"/> children.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The resolved id used by the registry and AI tool name.</summary>
    public string ResolvedId { get; private set; } = string.Empty;

    /// <summary>The command's declared arguments.</summary>
    public IReadOnlyList<DrylCommandArgument> Arguments => _arguments;

    /// <summary>Registers an argument (called by <see cref="DrylCommandArgument"/>).</summary>
    public void AddArgument(DrylCommandArgument argument)
    {
        if (!_arguments.Contains(argument)) _arguments.Add(argument);
    }

    /// <summary>Runs the command with the supplied context.</summary>
    public Task RunAsync(CommandContext context) => OnRun.InvokeAsync(context);

    protected override void OnInitialized()
    {
        ResolvedId = string.IsNullOrWhiteSpace(Id) ? Slug(Title) : Id!;
        if (Registry is not null && !_registered)
        {
            Registry.Add(this);
            _registered = true;
        }
    }

    private static string Slug(string title)
    {
        var chars = title.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? $"cmd-{Guid.NewGuid():N}" : slug;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_registered) Registry?.Remove(ResolvedId);
    }
}
