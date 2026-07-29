using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.JSInterop;

namespace DRYL.Components.Agents;

/// <summary>
/// The server half of a voice session: it mints the short-lived client secret and owns the JS
/// runtime the browser side is driven through. Registered scoped by <c>AddDrylAgents()</c> — one
/// per Blazor circuit.
/// </summary>
/// <remarks>
/// The API key lives here and only here. The browser receives an <c>ek_…</c> token with the
/// entire session baked into it, so it can neither read the key nor change the instructions, the
/// model or the tool list.
/// </remarks>
public sealed class DrylVoiceRunner
{
    // One client for the lifetime of the app: a fresh HttpClient per session is the classic way
    // to exhaust sockets. Only used when the host does not supply its own.
    private static readonly HttpClient Shared = new();

    private readonly IJSRuntime _js;
    private readonly HttpClient _http;

    /// <summary>Creates the runner. The <paramref name="http"/> parameter is a test seam.</summary>
    /// <param name="js">The circuit's JS runtime — the browser side is driven through it.</param>
    /// <param name="http">Optional HTTP client; a shared one is used when omitted.</param>
    public DrylVoiceRunner(IJSRuntime js, HttpClient? http = null)
    {
        _js = js;
        _http = http ?? Shared;
    }

    internal IJSRuntime Js => _js;

    /// <summary>
    /// Builds a voice session handle. Hold it in a service that outlives the page — a run kept in
    /// a component field dies on the first re-render that replaces the component.
    /// </summary>
    /// <param name="options">The session's configuration.</param>
    public DrylVoiceRun Create(DrylVoiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new DrylVoiceRun(this, options);
    }

    /// <summary>
    /// Exchanges the API key for an ephemeral client secret with the session baked in.
    /// </summary>
    /// <exception cref="InvalidOperationException">No key configured, or the API refused.</exception>
    internal async Task<string> MintTokenAsync(DrylVoiceOptions options, CancellationToken ct)
    {
        if (!options.IsConfigured)
            throw new InvalidOperationException("DrylVoiceOptions.ApiKey is not set.");

        var payload = new JsonObject
        {
            // Long enough to survive a slow SDP exchange, short enough that a leaked token is
            // worthless by the time anyone finds it.
            ["expires_after"] = new JsonObject { ["anchor"] = "created_at", ["seconds"] = 60 },
            ["session"] = options.ToSessionPayload(),
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            options.BaseUrl.TrimEnd('/') + "/realtime/client_secrets")
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        if (!string.IsNullOrWhiteSpace(options.SafetyIdentifier))
            request.Headers.Add("OpenAI-Safety-Identifier", options.SafetyIdentifier);

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ReadApiError(body, response.StatusCode));

        var value = (string?)JsonNode.Parse(body)?["value"];

        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException("The realtime API returned no client secret.")
            : value;
    }

    // The API's own message is far more useful than a status code: a wrong key, an exhausted
    // quota and an unknown model all arrive as 4xx and mean completely different things.
    private static string ReadApiError(string body, HttpStatusCode status)
    {
        try
        {
            if ((string?)JsonNode.Parse(body)?["error"]?["message"] is { Length: > 0 } message)
                return message;
        }
        catch (JsonException)
        {
            // not JSON — fall through to the status line
        }

        return $"The realtime API rejected the session ({(int)status}).";
    }
}
