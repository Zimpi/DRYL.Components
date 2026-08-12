namespace DRYL.Components;

/// <summary>The outcome of an <see cref="ICommandResolver"/>: the chosen command, the model-filled
/// arguments, and a 0–1 confidence. Surfaced by the palette as a confirmable top suggestion.</summary>
public sealed record CommandResolution(
    DrylCommand Command,
    IReadOnlyDictionary<string, object?> Arguments,
    double Confidence);
