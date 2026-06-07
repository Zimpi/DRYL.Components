using System.Reflection;

namespace DRYL.Components.Demo;

/// <summary>
/// Reads the raw source text of the example components embedded under
/// <c>Components/Examples/&lt;Component&gt;/&lt;Name&gt;.razor</c> so a
/// <c>DemoExample</c> can display exactly the code that produced its live preview.
/// The lookup key is the path under <c>Examples</c> without extension, e.g. "Button/Variants".
/// </summary>
public sealed class ExampleSourceProvider
{
    private const string Marker = ".Examples.";
    private const string Suffix = ".razor";

    private readonly Assembly _assembly = typeof(ExampleSourceProvider).Assembly;
    private readonly Dictionary<string, string> _resourceByKey = new(StringComparer.OrdinalIgnoreCase);

    public ExampleSourceProvider()
    {
        // Map every embedded "...Examples.<Folder>.<Name>.razor" resource to the key "<Folder>/<Name>".
        foreach (var name in _assembly.GetManifestResourceNames())
        {
            var idx = name.IndexOf(Marker, StringComparison.Ordinal);
            if (idx < 0 || !name.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase)) continue;

            var start = idx + Marker.Length;
            var middle = name[start..^Suffix.Length];          // e.g. "Button.Variants"
            var key = middle.Replace('.', '/');                // e.g. "Button/Variants"
            _resourceByKey[key] = name;
        }
    }

    /// <summary>Returns the normalized source text for an example key, or a placeholder comment if missing.</summary>
    public string Get(string key)
    {
        if (!_resourceByKey.TryGetValue(key, out var resourceName))
            return $"@* Example source not found: {key} *@";

        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return $"@* Example source stream missing: {key} *@";

        using var reader = new StreamReader(stream);
        return Normalize(reader.ReadToEnd());
    }

    private static string Normalize(string text)
    {
        if (text.Length > 0 && text[0] == '﻿')
            text = text[1..];
        return text.Replace("\r\n", "\n").Trim('\n');
    }
}
