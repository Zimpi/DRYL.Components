namespace DRYL.Components.Theming;

/// <summary>
/// Optional overrides for DRYL's semantic status colors. Any member left
/// <c>null</c> falls back to the DRYL default for that token.
/// </summary>
public sealed record DrylSemantic
{
    /// <summary>Healthy / succeeded / online — maps to <c>--success</c>.</summary>
    public string? Success { get; init; }

    /// <summary>Pending / near-limit — maps to <c>--warning</c>.</summary>
    public string? Warning { get; init; }

    /// <summary>Failed / destructive — maps to <c>--danger</c>.</summary>
    public string? Danger { get; init; }

    /// <summary>Informational / neutral — maps to <c>--info</c>.</summary>
    public string? Info { get; init; }
}
