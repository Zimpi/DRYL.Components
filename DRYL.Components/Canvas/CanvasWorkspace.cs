using System.Text;

namespace DRYL.Components.Canvas;

/// <summary>
/// One named artifact in a <see cref="CanvasWorkspace"/> — the unit the user comes back to.
/// A view owns its title and its spec; who authored that spec (code, a store, an AI run) is
/// none of its business.
/// </summary>
public sealed class CanvasView
{
    /// <summary>Stable key — the slug of the title the view was opened with.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Label shown on the view's chip.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional <c>DrylIcon</c> name shown left of the title.</summary>
    public string? Icon { get; set; }

    /// <summary>The artifact this view shows, or null while it is still empty.</summary>
    public CanvasSpec? Spec { get; set; }

    /// <summary>True while the chip plays its exit animation; the bar calls
    /// <see cref="CanvasWorkspace.Remove"/> when that animation ends.</summary>
    public bool Removing { get; internal set; }
}

/// <summary>
/// The named views of one canvas surface, exactly one of them active. A line-of-business page is
/// not one artifact; it is "the overview", "order 4711", "last month's report" — and the user has
/// to be able to get back to what they had.
/// </summary>
/// <remarks>
/// Plain observable state: <c>DrylCanvasWorkspace</c> renders it, an AI run writes into its active
/// view, and a host may pre-fill it from code. Renderer-thread state like <c>DrylRunBase</c> — no
/// locking, no <c>INotifyPropertyChanged</c>. Every mutation that actually changed something raises
/// <see cref="OnChange"/> exactly once.
/// </remarks>
public sealed class CanvasWorkspace
{
    private readonly List<CanvasView> _views = new();

    /// <summary>The views, in the order they were opened.</summary>
    public IReadOnlyList<CanvasView> Views => _views;

    /// <summary>Id of the view currently shown, or null while the workspace is empty.</summary>
    public string? ActiveId { get; private set; }

    /// <summary>The view currently shown, or null while the workspace is empty.</summary>
    public CanvasView? Active =>
        ActiveId is null ? null : _views.FirstOrDefault(v => v.Id == ActiveId);

    /// <summary>Raised after every mutation that changed the workspace.</summary>
    public event Action? OnChange;

    /// <summary>
    /// Opens the view with this title and activates it. A title whose slug is already present
    /// re-activates that view and keeps its spec — that is what makes "back to the overview" work
    /// for the user and for the model alike.
    /// </summary>
    /// <param name="title">Display title; its slug becomes the view's id.</param>
    /// <param name="icon">Optional <c>DrylIcon</c> name for the chip.</param>
    public CanvasView Open(string title, string? icon = null)
    {
        var slug = Slug(title);
        if (_views.FirstOrDefault(v => v.Id == slug) is { } existing)
        {
            existing.Removing = false;   // re-opening a closing view keeps it alive
            if (icon is not null) existing.Icon = icon;
            ActiveId = existing.Id;
            OnChange?.Invoke();
            return existing;
        }

        var view = new CanvasView { Id = slug, Title = title.Trim(), Icon = icon };
        _views.Add(view);
        ActiveId = view.Id;
        OnChange?.Invoke();
        return view;
    }

    /// <summary>Shows the view with this id. Returns false when it is unknown or already active.</summary>
    public bool Activate(string id)
    {
        if (ActiveId == id) return false;
        if (_views.All(v => v.Id != id)) return false;

        ActiveId = id;
        OnChange?.Invoke();
        return true;
    }

    /// <summary>
    /// Starts closing a view: it is flagged for its exit animation and, if it was active, hands the
    /// active slot to a neighbour right away — the body must not keep showing something that is on
    /// its way out.
    /// </summary>
    public void Close(string id)
    {
        var index = _views.FindIndex(v => v.Id == id);
        if (index < 0 || _views[index].Removing) return;

        _views[index].Removing = true;
        if (ActiveId == id) ActiveId = Neighbour(index);
        OnChange?.Invoke();
    }

    /// <summary>Drops the view — called by the bar once the exit animation has finished.</summary>
    public void Remove(string id)
    {
        var index = _views.FindIndex(v => v.Id == id);
        if (index < 0) return;

        var wasActive = ActiveId == id;
        _views.RemoveAt(index);
        if (wasActive) ActiveId = Neighbour(index);
        OnChange?.Invoke();
    }

    /// <summary>Empties the workspace.</summary>
    public void Clear()
    {
        if (_views.Count == 0 && ActiveId is null) return;

        _views.Clear();
        ActiveId = null;
        OnChange?.Invoke();
    }

    // The nearest view that is not itself on its way out: right first, then left.
    private string? Neighbour(int index)
    {
        for (var i = index; i < _views.Count; i++)
            if (!_views[i].Removing) return _views[i].Id;
        for (var i = Math.Min(index, _views.Count) - 1; i >= 0; i--)
            if (!_views[i].Removing) return _views[i].Id;
        return null;
    }

    // Lower-cased, letters and digits kept, everything else collapsed into a single dash.
    // Culture-invariant on purpose: the id is a key, not display text.
    private static string Slug(string? title)
    {
        var sb = new StringBuilder();
        foreach (var ch in (title ?? string.Empty).Trim())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }

        var slug = sb.ToString().Trim('-');
        return slug.Length == 0 ? "view" : slug;
    }
}
