using System.Text;
using System.Text.Json;

namespace DRYL.Components.Canvas;

/// <summary>
/// Turns a source name plus its resolved parameters into the binder's dedupe key.
/// Property order in the JSON the model wrote is arbitrary, so the key is built from a
/// canonical rendering: object keys sorted ordinal, arrays kept in order, numbers written
/// invariantly. Three stat nodes on <c>orders.open</c> then share one key — and one call.
/// </summary>
internal static class CanvasDataKey
{
    public static string Of(string source, JsonElement? parameters) =>
        source + "|" + Canonicalize(parameters);

    public static string Canonicalize(JsonElement? element)
    {
        if (element is not { } e || e.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return "{}";
        var sb = new StringBuilder();
        Write(sb, e);
        return sb.ToString();
    }

    private static void Write(StringBuilder sb, JsonElement e)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.Object:
                sb.Append('{');
                var first = true;
                foreach (var p in e.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(JsonSerializer.Serialize(p.Name)).Append(':');
                    Write(sb, p.Value);
                }
                sb.Append('}');
                break;

            case JsonValueKind.Array:
                sb.Append('[');
                var firstItem = true;
                foreach (var item in e.EnumerateArray())
                {
                    if (!firstItem) sb.Append(',');
                    firstItem = false;
                    Write(sb, item);
                }
                sb.Append(']');
                break;

            case JsonValueKind.String:
                sb.Append(JsonSerializer.Serialize(e.GetString()));
                break;

            case JsonValueKind.Number:
                // GetRawText keeps the invariant textual form the parser saw — no culture in play.
                sb.Append(e.GetRawText());
                break;

            default:
                sb.Append(e.GetRawText());
                break;
        }
    }
}
