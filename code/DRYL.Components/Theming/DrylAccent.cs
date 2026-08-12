namespace DRYL.Components.Theming;

/// <summary>
/// A two-stop accent: the endpoints of DRYL's accent gradient. Either stop
/// can be used on its own (e.g. as a solid accent), but together they form
/// <c>--accent-grad</c>.
/// </summary>
/// <param name="A">First gradient stop / primary accent (maps to <c>--accent-a</c>).</param>
/// <param name="B">Second gradient stop / secondary accent (maps to <c>--accent-b</c>).</param>
public readonly record struct DrylAccent(string A, string B);
