using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DRYL.Components.Canvas;

/// <summary>
/// Resolves the argument/parameter objects of a canvas binding: literals stay as they are,
/// <c>{ "$field": "&lt;name&gt;" }</c> becomes the interactive node's current value.
///
/// <para>Shared by <see cref="CanvasDataBinder"/> (a data binding's <c>params</c>) and
/// <see cref="CanvasActionRunner"/> (an action binding's <c>args</c>) on purpose: the model is
/// told the reference syntax is the same in both places, so there must not be two
/// implementations of it that can drift apart.</para>
/// </summary>
internal static class CanvasArgs
{
    /// <summary>
    /// Resolves <paramref name="raw"/> against <paramref name="form"/>. Returns <c>null</c> when
    /// there is no object to resolve; <paramref name="fields"/> collects every referenced field
    /// name so a caller can react to exactly those changing.
    /// </summary>
    public static JsonElement? Resolve(JsonElement? raw, CanvasFormState form, out HashSet<string> fields)
    {
        fields = new HashSet<string>(StringComparer.Ordinal);
        if (raw is not { ValueKind: JsonValueKind.Object } obj) return null;

        var result = new JsonObject();
        foreach (var p in obj.EnumerateObject())
        {
            if (FieldReference(p.Value) is { } field)
            {
                fields.Add(field);
                result[p.Name] = ToNode(form.Get(field));
            }
            else
            {
                result[p.Name] = JsonNode.Parse(p.Value.GetRawText());
            }
        }
        return JsonSerializer.SerializeToElement(result);
    }

    /// <summary>The field name of a <c>{ "$field": "…" }</c> reference, or <c>null</c> for a literal.</summary>
    public static string? FieldReference(JsonElement value) =>
        value.ValueKind == JsonValueKind.Object &&
        value.TryGetProperty("$field", out var f) &&
        f.ValueKind == JsonValueKind.String
            ? f.GetString()
            : null;

    /// <summary>True when at least one value of the object is a field reference.</summary>
    public static bool HasFieldReference(JsonElement? args) =>
        args is { ValueKind: JsonValueKind.Object } p &&
        p.EnumerateObject().Any(x => FieldReference(x.Value) is not null);

    private static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        string s => JsonValue.Create(s),
        bool b => JsonValue.Create(b),
        double d => JsonValue.Create(d),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        decimal m => JsonValue.Create(m),
        _ => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture)),
    };
}
