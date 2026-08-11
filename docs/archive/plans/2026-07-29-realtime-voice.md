# Sprachmodus fürs Canvas-Dock — Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Das `DrylCanvasDock` bekommt einen Sprachmodus über die OpenAI-Realtime-API (WebRTC), der dieselben Werkzeuge und denselben Verlauf nutzt wie der Textmodus.

**Architecture:** Der Browser hält den Medienpfad (WebRTC direkt zu OpenAI), der Server prägt das kurzlebige Zugangstoken und führt jeden Werkzeugaufruf aus. Ein `DrylVoiceRun` im Besitz des Hosts trägt den beobachtbaren Zustand; das Dock rendert ihn als Übernahme mit einem Orb. Lautstärkepegel bleiben im Browser und fassen den Circuit nie an.

**Tech Stack:** .NET 8/9/10, Blazor (Server + WASM), `Microsoft.Agents.AI` 1.13.0 / `Microsoft.Extensions.AI`, ES-Modul ohne Abhängigkeiten, xUnit + bUnit 2.7.2.

**Spec:** `docs/superpowers/specs/2026-07-29-realtime-voice-design.md`

## Global Constraints

- **Keine neuen Laufzeitabhängigkeiten** (CLAUDE.md §2.8). Kein npm, kein neues NuGet. `System.Text.Json` und `HttpClient` sind bereits da.
- **Nur Tokens, nie Literale** (§2.1). Jede Farbe, jedes Maß, jeder Radius, jede Dauer aus `dryl.css`.
- **`dryl.css` wird nicht angefasst.** Der Orb baut ausschließlich auf den vorhandenen `.ai-aura*`-Primitiven auf. Falls sich das als unmöglich erweist: anhalten und melden, nicht heimlich eine Farbe erfinden.
- **Kein neues AI-Vokabular** (§2.10). Nur `AiState.None/Active/Thinking/Streaming/Generated`.
- **Motion ist Pflicht** (§2.12). Jeder Zustandswechsel im Dock läuft über `DrylPresence`; `prefers-reduced-motion: reduce` wird respektiert.
- **Icon-only-Buttons brauchen `DrylTooltip` + `AriaLabel`** (§2.11).
- **JS-Interop braucht ein `_attached`-Flag im Dispose** (Prerender-Guard, bekannter Fallstrick des Repos).
- **Zahlen in JSON/CSS über `FormattableString.Invariant`** — deutsche Locale macht sonst `0,5` aus `0.5`.
- Zielversion nach Abschluss: `DRYL.Components.Agents` **0.16.0**. Kern bleibt bei 2.19.0.
- Alle Commits auf Branch `feat/realtime-voice` (DRYL.Components) bzw. `feat/assistant-voice` (DRYL.Portfolio).

## Dateien

**Neu in `DRYL.Components.Agents/Voice/`:**

| Datei | Verantwortung |
|---|---|
| `DrylVoiceMessage.cs` | Ein Transkript-Turn (Rolle + Text) |
| `DrylVoiceOptions.cs` | Alles Einstellbare + `ToSessionPayload()` |
| `DrylVoiceRunner.cs` | Token prägen, `DrylVoiceRun` bauen, `IJSRuntime` halten |
| `DrylVoiceRun.cs` | Beobachtbarer Zustand, JS-Brücke, Werkzeugausführung |
| `DrylVoiceOrb.razor` (+ `.css`) | Die sichtbare Stimme |
| `wwwroot/js/dryl-voice.js` | WebRTC, Datenkanal, Pegel |

**Geändert:**

| Datei | Änderung |
|---|---|
| `Canvas/DrylCanvasDock.razor` (+ `.css`) | `Voice`-Parameter, Mikro-Knopf, Übernahme |
| `Extensions/ServiceCollectionExtensions.cs` | `DrylVoiceRunner` registrieren |
| `DRYL.Components.Agents.csproj` | Version 0.16.0 |
| `CHANGELOG.md` | Eintrag unter `Added` |

**Tests:** `tests/DRYL.Components.Tests/Agents/Voice/` — `DrylVoiceOptionsTests.cs`, `DrylVoiceRunTests.cs`, `DrylVoiceOrbTests.cs`; dazu neue Fälle in `Agents/Canvas/DrylCanvasDockTests.cs`.

---

### Task 1: Optionen und Transkript-Turn

**Files:**
- Create: `DRYL.Components.Agents/Voice/DrylVoiceMessage.cs`
- Create: `DRYL.Components.Agents/Voice/DrylVoiceOptions.cs`
- Test: `tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceOptionsTests.cs`

**Interfaces:**
- Produces: `DRYL.Components.Agents.DrylVoiceMessage` (record: `VoiceRole Role`, `string Text`), `VoiceRole { User, Assistant }`, `DrylVoiceOptions` mit `JsonNode ToSessionPayload()`, `VoiceTurnDetection { SemanticVad, ServerVad }`, `VoiceNoiseReduction { NearField, FarField, Off }`.

- [ ] **Step 1: Den fehlschlagenden Test schreiben**

`tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceOptionsTests.cs`:

```csharp
using System.ComponentModel;
using System.Text.Json.Nodes;
using DRYL.Components.Agents;
using Microsoft.Extensions.AI;
using Xunit;

namespace DRYL.Components.Tests.Agents.Voice;

/// <summary>The options are the whole developer-facing API — the payload has to be exactly
/// what /v1/realtime/client_secrets expects, because a typo there fails at runtime only.</summary>
public class DrylVoiceOptionsTests
{
    [Fact]
    public void The_payload_carries_model_voice_and_speed()
    {
        var options = new DrylVoiceOptions
        {
            ApiKey = "sk-test",
            Model = "gpt-realtime-2.1",
            Voice = "cedar",
            Speed = 1.25,
            Instructions = "Sei knapp.",
        };

        var session = options.ToSessionPayload();

        Assert.Equal("realtime", (string?)session["type"]);
        Assert.Equal("gpt-realtime-2.1", (string?)session["model"]);
        Assert.Equal("Sei knapp.", (string?)session["instructions"]);
        Assert.Equal("cedar", (string?)session["audio"]!["output"]!["voice"]);
        Assert.Equal(1.25, (double?)session["audio"]!["output"]!["speed"]);
    }

    [Fact]
    public void Audio_format_is_left_to_the_peer_connection()
    {
        // WebRTC negotiates Opus itself. Pinning audio/pcm here is how you get a session that
        // connects and then stays silent.
        var session = new DrylVoiceOptions { ApiKey = "sk-test" }.ToSessionPayload();

        Assert.Null(session["audio"]!["input"]!["format"]);
        Assert.Null(session["audio"]!["output"]!["format"]);
    }

    [Fact]
    public void Semantic_vad_is_the_default_turn_detection()
    {
        var session = new DrylVoiceOptions { ApiKey = "sk-test" }.ToSessionPayload();

        Assert.Equal("semantic_vad", (string?)session["audio"]!["input"]!["turn_detection"]!["type"]);
    }

    [Fact]
    public void Transcription_is_on_so_the_user_half_of_the_conversation_survives()
    {
        var session = new DrylVoiceOptions { ApiKey = "sk-test", Language = "de" }.ToSessionPayload();

        var transcription = session["audio"]!["input"]!["transcription"]!;
        Assert.Equal("gpt-4o-transcribe", (string?)transcription["model"]);
        Assert.Equal("de", (string?)transcription["language"]);
    }

    [Fact]
    public void Transcription_can_be_switched_off_entirely()
    {
        var session = new DrylVoiceOptions { ApiKey = "sk-test", TranscriptionModel = null }
            .ToSessionPayload();

        Assert.Null(session["audio"]!["input"]!["transcription"]);
    }

    [Fact]
    public void Tools_are_emitted_as_realtime_function_schemas()
    {
        var options = new DrylVoiceOptions { ApiKey = "sk-test" };
        options.Tools.Add(AIFunctionFactory.Create(
            ([Description("the city")] string city) => "sunny",
            "get_weather",
            "Looks up the weather"));

        var tools = (JsonArray)new DrylVoiceOptions
        {
            ApiKey = "sk-test",
            Tools = options.Tools,
        }.ToSessionPayload()["tools"]!;

        var tool = tools[0]!;
        Assert.Equal("function", (string?)tool["type"]);
        Assert.Equal("get_weather", (string?)tool["name"]);
        Assert.Equal("Looks up the weather", (string?)tool["description"]);
        Assert.NotNull(tool["parameters"]);
    }

    [Fact]
    public void Reasoning_is_omitted_unless_asked_for()
    {
        Assert.Null(new DrylVoiceOptions { ApiKey = "sk-test" }.ToSessionPayload()["reasoning"]);

        var session = new DrylVoiceOptions { ApiKey = "sk-test", ReasoningEffort = "low" }
            .ToSessionPayload();
        Assert.Equal("low", (string?)session["reasoning"]!["effort"]);
    }
}
```

- [ ] **Step 2: Test laufen lassen, Fehlschlag bestätigen**

Run: `dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~DrylVoiceOptionsTests"`
Expected: Compile-Fehler — `DrylVoiceOptions` existiert nicht.

- [ ] **Step 3: `DrylVoiceMessage.cs` schreiben**

```csharp
namespace DRYL.Components.Agents;

/// <summary>Who said a line in a voice conversation.</summary>
public enum VoiceRole
{
    /// <summary>The person at the microphone.</summary>
    User,
    /// <summary>The assistant.</summary>
    Assistant,
}

/// <summary>
/// One spoken turn, transcribed. The transcript is what makes a voice conversation survive
/// the session: it seeds the next one and hands back to the text conversation.
/// </summary>
/// <param name="Role">Who spoke.</param>
/// <param name="Text">What was said, as text.</param>
public sealed record DrylVoiceMessage(VoiceRole Role, string Text);
```

- [ ] **Step 4: `DrylVoiceOptions.cs` schreiben**

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

/// <summary>How the model decides that the user has finished a turn.</summary>
public enum VoiceTurnDetection
{
    /// <summary>Waits for a complete thought rather than for silence — the natural choice for
    /// conversation, because it stops the model from cutting into a thinking pause.</summary>
    SemanticVad,
    /// <summary>Plain silence detection. Snappier, but interrupts hesitant speakers.</summary>
    ServerVad,
}

/// <summary>Server-side noise handling on the input stream.</summary>
public enum VoiceNoiseReduction
{
    /// <summary>Headset or handset — the microphone is close to the mouth.</summary>
    NearField,
    /// <summary>Laptop or room microphone.</summary>
    FarField,
    /// <summary>No server-side reduction.</summary>
    Off,
}

/// <summary>
/// Everything a developer configures about a voice session. There is deliberately no settings
/// UI anywhere in DRYL: voice, persona and model are code, not preferences.
/// </summary>
/// <remarks>
/// The whole object is baked into the ephemeral client secret, so the browser cannot change
/// any of it. Note that <see cref="Voice"/> is locked by the API once a session has emitted
/// audio — switching voices means starting a new session, not updating this object.
/// </remarks>
public sealed class DrylVoiceOptions
{
    /// <summary>OpenAI API key. Stays on the server — only the minted <c>ek_…</c> token
    /// reaches the browser.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Realtime model. Also <c>gpt-realtime-2</c> and <c>gpt-realtime-2.1-mini</c>.</summary>
    public string Model { get; set; } = "gpt-realtime-2.1";

    /// <summary>The system prompt — role, personality, tone, language. Hand it the same prompt
    /// the text assistant uses, plus whatever is specific to being spoken aloud.</summary>
    public string? Instructions { get; set; }

    /// <summary>One of alloy, ash, ballad, coral, echo, sage, shimmer, verse, marin, cedar.
    /// <c>marin</c> and <c>cedar</c> are the highest quality.</summary>
    public string Voice { get; set; } = "marin";

    /// <summary>Speaking rate, 0.25–1.5.</summary>
    public double Speed { get; set; } = 1.0;

    /// <summary>How the end of a user turn is detected.</summary>
    public VoiceTurnDetection TurnDetection { get; set; } = VoiceTurnDetection.SemanticVad;

    /// <summary>Server-side input noise reduction.</summary>
    public VoiceNoiseReduction NoiseReduction { get; set; } = VoiceNoiseReduction.NearField;

    /// <summary>Reasoning effort — <c>low</c>, <c>medium</c> or <c>high</c>. Null leaves it to
    /// the model's default. Only the 2.1 family supports it.</summary>
    public string? ReasoningEffort { get; set; }

    /// <summary>Model that transcribes the user's audio. Null switches transcription off — and
    /// with it every trace of what the user said.</summary>
    public string? TranscriptionModel { get; set; } = "gpt-4o-transcribe";

    /// <summary>ISO language code for the transcription, e.g. <c>de</c>. Null lets it detect.</summary>
    public string? Language { get; set; }

    /// <summary>The tools the voice may call — hand it the same list the text agent has.</summary>
    public IList<AITool> Tools { get; set; } = new List<AITool>();

    /// <summary>Silence after which the session closes itself. An open session bills per minute
    /// whether or not anyone is talking.</summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Hard cap on session length. The API's own limit is 60 minutes.</summary>
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>API base URL — override for Azure or a proxy.</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>A stable, privacy-preserving user identifier (e.g. a hashed internal id), sent
    /// as the <c>OpenAI-Safety-Identifier</c> header.</summary>
    public string? SafetyIdentifier { get; set; }

    /// <summary>True when a key is configured — hosts use this to decide whether to offer voice
    /// at all.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// Builds the <c>session</c> block for <c>POST /v1/realtime/client_secrets</c>.
    /// </summary>
    /// <remarks>Audio formats are deliberately absent: over WebRTC the peer connection
    /// negotiates the codec, and pinning <c>audio/pcm</c> here produces a session that connects
    /// and then stays silent.</remarks>
    public JsonNode ToSessionPayload()
    {
        var input = new JsonObject
        {
            ["turn_detection"] = new JsonObject
            {
                ["type"] = TurnDetection == VoiceTurnDetection.ServerVad ? "server_vad" : "semantic_vad",
            },
        };

        if (NoiseReduction != VoiceNoiseReduction.Off)
        {
            input["noise_reduction"] = new JsonObject
            {
                ["type"] = NoiseReduction == VoiceNoiseReduction.FarField ? "far_field" : "near_field",
            };
        }

        if (!string.IsNullOrWhiteSpace(TranscriptionModel))
        {
            var transcription = new JsonObject { ["model"] = TranscriptionModel };
            if (!string.IsNullOrWhiteSpace(Language)) transcription["language"] = Language;
            input["transcription"] = transcription;
        }

        var session = new JsonObject
        {
            ["type"] = "realtime",
            ["model"] = Model,
            ["output_modalities"] = new JsonArray("audio"),
            ["audio"] = new JsonObject
            {
                ["input"] = input,
                ["output"] = new JsonObject
                {
                    ["voice"] = Voice,
                    ["speed"] = Speed,
                },
            },
        };

        if (!string.IsNullOrWhiteSpace(Instructions)) session["instructions"] = Instructions;
        if (!string.IsNullOrWhiteSpace(ReasoningEffort))
            session["reasoning"] = new JsonObject { ["effort"] = ReasoningEffort };

        var functions = ToolSchemas();
        if (functions.Count > 0)
        {
            session["tools"] = functions;
            session["tool_choice"] = "auto";
        }

        return session;
    }

    private JsonArray ToolSchemas()
    {
        var array = new JsonArray();
        foreach (var tool in Tools)
        {
            if (tool is not AIFunction fn) continue;
            array.Add(new JsonObject
            {
                ["type"] = "function",
                ["name"] = fn.Name,
                ["description"] = fn.Description,
                ["parameters"] = JsonNode.Parse(fn.JsonSchema.GetRawText()),
            });
        }
        return array;
    }

    /// <summary>Finds a tool by the name the model used, or null if it invented one.</summary>
    internal AIFunction? FindTool(string name) =>
        Tools.OfType<AIFunction>().FirstOrDefault(f =>
            string.Equals(f.Name, name, StringComparison.Ordinal));
}
```

- [ ] **Step 5: Tests laufen lassen**

Run: `dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~DrylVoiceOptionsTests"`
Expected: PASS (7 Tests). Schlägt `AIFunction.JsonSchema` fehl, in der installierten `Microsoft.Extensions.AI.Abstractions` nachsehen, welchen Namen die Schema-Eigenschaft dort trägt, und den Aufruf anpassen — nicht den Test aufweichen.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents/Voice tests/DRYL.Components.Tests/Agents/Voice
git commit -m "feat(voice): die Optionen einer Sprachsession"
```

---

### Task 2: Runner — Token prägen

**Files:**
- Create: `DRYL.Components.Agents/Voice/DrylVoiceRunner.cs`
- Modify: `DRYL.Components.Agents/Extensions/ServiceCollectionExtensions.cs`
- Test: `tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceRunnerTests.cs`
- Test: `tests/DRYL.Components.Tests/Agents/AddDrylAgentsTests.cs` (ergänzen)

**Interfaces:**
- Consumes: `DrylVoiceOptions` aus Task 1.
- Produces: `DrylVoiceRunner` mit `DrylVoiceRun Create(DrylVoiceOptions options)` und intern `Task<string> MintTokenAsync(DrylVoiceOptions, CancellationToken)`; Konstruktor `DrylVoiceRunner(IJSRuntime js, HttpClient? http = null)`.

> `Create` liefert einen `DrylVoiceRun` — der entsteht erst in Task 3. Bis dahin ist der Compiler unzufrieden. Deshalb legt **dieser** Task nur `MintTokenAsync` und die DI-Registrierung an; `Create` kommt am Ende von Task 3 dazu. Der Test hier testet ausschließlich das Prägen.

- [ ] **Step 1: Den fehlschlagenden Test schreiben**

`tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceRunnerTests.cs`:

```csharp
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using DRYL.Components.Agents;
using Microsoft.JSInterop;
using Xunit;

namespace DRYL.Components.Tests.Agents.Voice;

/// <summary>The runner is the only place the API key exists. What leaves it is a token.</summary>
public class DrylVoiceRunnerTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string Response { get; set; } = """{"value":"ek_abc","expires_at":1}""";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Status)
            {
                Content = new StringContent(Response, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static DrylVoiceRunner Runner(CapturingHandler handler) =>
        new(new NoopJsRuntime(), new HttpClient(handler));

    private sealed class NoopJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken ct, object?[]? args) => default;
    }

    [Fact]
    public async Task It_posts_the_session_to_the_client_secrets_endpoint()
    {
        var handler = new CapturingHandler();

        var token = await Runner(handler).MintTokenAsync(
            new DrylVoiceOptions { ApiKey = "sk-test", Voice = "cedar" }, CancellationToken.None);

        Assert.Equal("ek_abc", token);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://api.openai.com/v1/realtime/client_secrets",
            handler.Request.RequestUri!.ToString());
        Assert.Equal("sk-test", handler.Request.Headers.Authorization!.Parameter);

        var body = JsonNode.Parse(handler.Body!)!;
        Assert.Equal("cedar", (string?)body["session"]!["audio"]!["output"]!["voice"]);
        Assert.NotNull(body["expires_after"]);
    }

    [Fact]
    public async Task The_safety_identifier_travels_as_a_header_when_set()
    {
        var handler = new CapturingHandler();

        await Runner(handler).MintTokenAsync(
            new DrylVoiceOptions { ApiKey = "sk-test", SafetyIdentifier = "hashed-jan" },
            CancellationToken.None);

        Assert.Equal("hashed-jan",
            handler.Request!.Headers.GetValues("OpenAI-Safety-Identifier").Single());
    }

    [Fact]
    public async Task A_rejected_key_surfaces_as_a_readable_error()
    {
        var handler = new CapturingHandler
        {
            Status = HttpStatusCode.Unauthorized,
            Response = """{"error":{"message":"Incorrect API key provided."}}""",
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Runner(handler).MintTokenAsync(new DrylVoiceOptions { ApiKey = "sk-bad" },
                CancellationToken.None));

        Assert.Contains("Incorrect API key", ex.Message);
    }

    [Fact]
    public async Task A_missing_key_never_reaches_the_network()
    {
        var handler = new CapturingHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Runner(handler).MintTokenAsync(new DrylVoiceOptions(), CancellationToken.None));

        Assert.Null(handler.Request);
    }
}
```

- [ ] **Step 2: Test laufen lassen, Fehlschlag bestätigen**

Run: `dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~DrylVoiceRunnerTests"`
Expected: Compile-Fehler — `DrylVoiceRunner` existiert nicht.

- [ ] **Step 3: `DrylVoiceRunner.cs` schreiben** (ohne `Create` — das kommt in Task 3)

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.JSInterop;

namespace DRYL.Components.Agents;

/// <summary>
/// The server half of a voice session: it mints the short-lived client secret and owns the
/// JS runtime the browser side is driven through. Registered scoped by
/// <c>AddDrylAgents()</c> — one per Blazor circuit.
/// </summary>
/// <remarks>
/// The API key lives here and only here. The browser receives an <c>ek_…</c> token with the
/// entire session baked in, so it can neither read the key nor change the instructions, the
/// model or the tool list.
/// </remarks>
public sealed class DrylVoiceRunner
{
    // One client for the lifetime of the app: a new HttpClient per session is the classic way
    // to exhaust sockets. Only used when the host does not supply its own.
    private static readonly HttpClient Shared = new();

    private readonly IJSRuntime _js;
    private readonly HttpClient _http;

    /// <summary>Creates the runner. The <paramref name="http"/> parameter exists for tests.</summary>
    public DrylVoiceRunner(IJSRuntime js, HttpClient? http = null)
    {
        _js = js;
        _http = http ?? Shared;
    }

    internal IJSRuntime Js => _js;

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
            HttpMethod.Post, options.BaseUrl.TrimEnd('/') + "/realtime/client_secrets")
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

    // The API's own message is far more useful than a status code — a wrong key, an exhausted
    // quota and an unknown model all arrive as 4xx and mean completely different things.
    private static string ReadApiError(string body, System.Net.HttpStatusCode status)
    {
        try
        {
            if ((string?)JsonNode.Parse(body)?["error"]?["message"] is { Length: > 0 } message)
                return message;
        }
        catch (System.Text.Json.JsonException) { /* not JSON — fall through */ }

        return $"The realtime API rejected the session ({(int)status}).";
    }
}
```

- [ ] **Step 4: In `AddDrylAgents()` registrieren**

In `Extensions/ServiceCollectionExtensions.cs`, in `AddDrylAgents`, unter die bestehende Zeile:

```csharp
        services.AddScoped<DrylAgentRunner>();
        // Explicit factory rather than AddScoped<DrylVoiceRunner>(): the optional HttpClient
        // parameter is a test seam, and nothing should try to resolve it from the container.
        services.AddScoped(sp => new DrylVoiceRunner(
            sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
```

- [ ] **Step 5: Registrierungstest ergänzen**

An `tests/DRYL.Components.Tests/Agents/AddDrylAgentsTests.cs` anhängen (im vorhandenen Stil der Datei — vorher lesen und die dort genutzte Aufbaumethode wiederverwenden):

```csharp
    [Fact]
    public void It_registers_the_voice_runner_as_scoped()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(new Microsoft.JSInterop.Infrastructure.NoopJsRuntimeStub());
        services.AddDrylAgents();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(DrylVoiceRunner));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
```

> Falls die Datei bereits einen JS-Runtime-Stub oder ein `ServiceCollection`-Hilfsmittel hat, dieses nehmen statt eines neuen. `Microsoft.JSInterop.Infrastructure.NoopJsRuntimeStub` gibt es **nicht** — den lokalen `NoopJsRuntime` aus `DrylVoiceRunnerTests` in eine gemeinsame Testhilfe `tests/DRYL.Components.Tests/Agents/Voice/NoopJsRuntime.cs` (internal, namespace `DRYL.Components.Tests.Agents.Voice`) ziehen und hier verwenden.

- [ ] **Step 6: Tests laufen lassen**

Run: `dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~DrylVoiceRunnerTests|FullyQualifiedName~AddDrylAgentsTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add DRYL.Components.Agents tests/DRYL.Components.Tests
git commit -m "feat(voice): der Server prägt das Token, der Schlüssel bleibt liegen"
```

---

### Task 3: Der Run — Zustand, Brücke, Werkzeuge

**Files:**
- Create: `DRYL.Components.Agents/Voice/DrylVoiceRun.cs`
- Modify: `DRYL.Components.Agents/Voice/DrylVoiceRunner.cs` (`Create` ergänzen)
- Test: `tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceRunTests.cs`

**Interfaces:**
- Consumes: `DrylVoiceOptions`, `DrylVoiceMessage`, `DrylVoiceRunner` aus Tasks 1–2; `DrylRunBase` (`State`, `Error`, `ToolCalls`, `AddToolCall`, `Raise`, `OnChange`).
- Produces: `VoicePhase { Idle, Connecting, Live, Closing }`, `VoiceActivity { Listening, UserSpeaking, Thinking, Speaking }`, `DrylVoiceRun` mit `Phase`, `Activity`, `Transcript`, `StartAsync(IEnumerable<DrylVoiceMessage>?, CancellationToken)`, `StopAsync()`, und den `[JSInvokable]`-Rückrufen `OnConnected`, `OnActivity(string)`, `OnTranscript(string role, string text)`, `OnToolCallAsync(string, string, string)`, `OnFailed(string)`, `OnClosed()`.

- [ ] **Step 1: Den fehlschlagenden Test schreiben**

`tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceRunTests.cs`:

```csharp
using System.ComponentModel;
using System.Text.Json;
using DRYL.Components.Agents;
using Microsoft.Extensions.AI;
using Xunit;

namespace DRYL.Components.Tests.Agents.Voice;

/// <summary>The run is what the dock watches and what the host owns. The browser only reports
/// into it — every decision that matters (which tool exists, what it returns) stays here.</summary>
public class DrylVoiceRunTests
{
    private static DrylVoiceRun Run(DrylVoiceOptions? options = null) =>
        new DrylVoiceRunner(new NoopJsRuntime()).Create(options ?? new DrylVoiceOptions { ApiKey = "sk-test" });

    [Fact]
    public void A_fresh_run_is_idle_and_silent()
    {
        var run = Run();

        Assert.Equal(VoicePhase.Idle, run.Phase);
        Assert.Equal(AiState.None, run.State);
        Assert.Empty(run.Transcript);
    }

    [Theory]
    [InlineData(VoiceActivity.Listening, AiState.Active)]
    [InlineData(VoiceActivity.UserSpeaking, AiState.Active)]
    [InlineData(VoiceActivity.Thinking, AiState.Thinking)]
    [InlineData(VoiceActivity.Speaking, AiState.Streaming)]
    public void Live_activity_maps_onto_the_shared_ai_vocabulary(VoiceActivity activity, AiState expected)
    {
        var run = Run();
        run.OnConnected();
        run.OnActivity(activity.ToString());

        Assert.Equal(expected, run.State);
    }

    [Fact]
    public void Connecting_reads_as_thinking_not_as_idle()
    {
        // The dock breathes while the peer connection is being set up — an idle-looking dock
        // during a two-second handshake reads as "the button did nothing".
        var run = Run();
        run.MarkConnecting();

        Assert.Equal(AiState.Thinking, run.State);
    }

    [Fact]
    public void Transcript_lines_arrive_in_order_and_keep_their_speaker()
    {
        var run = Run();
        run.OnConnected();
        run.OnTranscript("User", "Zeig mir die Projekte");
        run.OnTranscript("Assistant", "Ist gebaut.");

        Assert.Collection(run.Transcript,
            m => { Assert.Equal(VoiceRole.User, m.Role); Assert.Equal("Zeig mir die Projekte", m.Text); },
            m => { Assert.Equal(VoiceRole.Assistant, m.Role); Assert.Equal("Ist gebaut.", m.Text); });
    }

    [Fact]
    public void Empty_transcript_lines_are_dropped()
    {
        // A cough gets transcribed as "" and would otherwise become a blank bubble in the log.
        var run = Run();
        run.OnConnected();
        run.OnTranscript("User", "   ");

        Assert.Empty(run.Transcript);
    }

    [Fact]
    public async Task A_tool_call_runs_the_real_function_and_returns_its_json()
    {
        var options = new DrylVoiceOptions { ApiKey = "sk-test" };
        options.Tools.Add(AIFunctionFactory.Create(
            ([Description("the city")] string city) => $"sunny in {city}", "get_weather"));
        var run = Run(options);
        run.OnConnected();

        var result = await run.OnToolCallAsync("call_1", "get_weather", """{"city":"Bonn"}""");

        Assert.Contains("sunny in Bonn", result);
        var call = Assert.Single(run.ToolCalls);
        Assert.Equal("get_weather", call.ToolName);
        Assert.Equal("call_1", call.CallId);
        Assert.NotNull(call.Result);
    }

    [Fact]
    public async Task An_invented_tool_name_comes_back_as_an_error_result_not_an_exception()
    {
        // Returning nothing would leave the model waiting for a result that never arrives, and
        // the conversation dies mid-sentence. An error it can read keeps it talking.
        var run = Run();
        run.OnConnected();

        var result = await run.OnToolCallAsync("call_1", "no_such_tool", "{}");

        using var json = JsonDocument.Parse(result);
        Assert.Contains("no_such_tool", json.RootElement.GetProperty("error").GetString());
        Assert.Equal("no_such_tool", Assert.Single(run.ToolCalls).ToolName);
        Assert.NotNull(Assert.Single(run.ToolCalls).Error);
    }

    [Fact]
    public async Task A_throwing_tool_comes_back_as_an_error_result()
    {
        var options = new DrylVoiceOptions { ApiKey = "sk-test" };
        options.Tools.Add(AIFunctionFactory.Create(
            () => throw new InvalidOperationException("Datenbank weg"), "list_projects"));
        var run = Run(options);
        run.OnConnected();

        var result = await run.OnToolCallAsync("call_1", "list_projects", "{}");

        Assert.Contains("Datenbank weg", result);
        Assert.NotNull(Assert.Single(run.ToolCalls).Error);
    }

    [Fact]
    public void A_failure_settles_the_run_instead_of_leaving_it_breathing()
    {
        var run = Run();
        run.MarkConnecting();
        run.OnFailed("Kein Zugriff aufs Mikrofon.");

        Assert.Equal(VoicePhase.Idle, run.Phase);
        Assert.Equal(AiState.None, run.State);
        Assert.Equal("Kein Zugriff aufs Mikrofon.", run.Error!.Message);
    }

    [Fact]
    public void Starting_clears_the_error_of_the_previous_attempt()
    {
        var run = Run();
        run.OnFailed("Kein Zugriff aufs Mikrofon.");
        run.MarkConnecting();

        Assert.Null(run.Error);
    }

    [Fact]
    public void The_transcript_survives_a_session_but_a_new_start_begins_a_new_one()
    {
        var run = Run();
        run.OnConnected();
        run.OnTranscript("User", "Hallo");
        run.OnClosed();

        Assert.Single(run.Transcript);

        run.MarkConnecting();
        Assert.Empty(run.Transcript);
    }

    [Fact]
    public void Change_notifications_reach_the_host()
    {
        var run = Run();
        var beats = 0;
        run.OnChange += () => beats++;

        run.MarkConnecting();
        run.OnConnected();
        run.OnActivity("Speaking");

        Assert.Equal(3, beats);
    }
}
```

- [ ] **Step 2: Test laufen lassen, Fehlschlag bestätigen**

Run: `dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~DrylVoiceRunTests"`
Expected: Compile-Fehler — `DrylVoiceRun` existiert nicht.

- [ ] **Step 3: `DrylVoiceRun.cs` schreiben**

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace DRYL.Components.Agents;

/// <summary>Where a voice session is in its life.</summary>
public enum VoicePhase
{
    /// <summary>Nothing running.</summary>
    Idle,
    /// <summary>Microphone granted, peer connection being negotiated.</summary>
    Connecting,
    /// <summary>Connected — the conversation is happening.</summary>
    Live,
    /// <summary>Tearing down.</summary>
    Closing,
}

/// <summary>What is happening inside a live session, moment to moment.</summary>
public enum VoiceActivity
{
    /// <summary>Waiting for the user to say something.</summary>
    Listening,
    /// <summary>The user is talking.</summary>
    UserSpeaking,
    /// <summary>The model is working — reasoning or running a tool.</summary>
    Thinking,
    /// <summary>The model is talking.</summary>
    Speaking,
}

/// <summary>
/// An observable handle on a spoken conversation with a realtime model, rendered by
/// <c>DrylCanvasDock Voice="…"</c>. Create it through <see cref="DrylVoiceRunner.Create"/> and
/// hold it in a service, not in a component: it should outlive a re-render and a navigation,
/// exactly like <c>DrylCanvasRun</c>.
/// </summary>
/// <remarks>
/// <para>The audio never touches .NET. The browser holds a WebRTC peer connection straight to
/// the API; this object receives state changes and tool calls over a data channel and answers
/// them. That is deliberate — routing audio through a Blazor Server circuit adds a few hundred
/// milliseconds in each direction, which is the difference between a conversation and a
/// walkie-talkie.</para>
/// <para>Input and output levels are deliberately absent. A level that updates 30 times a
/// second and raises <see cref="DrylRunBase.OnChange"/> is 30 renders a second for a decoration.
/// The level stays in the browser and drives a CSS variable on the orb.</para>
/// </remarks>
public sealed class DrylVoiceRun : DrylRunBase
{
    private const string ModulePath = "./_content/DRYL.Components.Agents/js/dryl-voice.js";

    private readonly DrylVoiceRunner _runner;
    private readonly List<DrylVoiceMessage> _transcript = new();
    private DotNetObjectReference<DrylVoiceRun>? _self;
    private IJSObjectReference? _module;
    private bool _attached;      // the JS module actually loaded (prerender guard)
    private bool _starting;

    internal DrylVoiceRun(DrylVoiceRunner runner, DrylVoiceOptions options)
    {
        _runner = runner;
        Options = options;
        State = AiState.None;    // a voice run is a possibility, not an errand — see DrylCanvasRun
    }

    /// <summary>The session's configuration. Read-only to the browser: it is baked into the token.</summary>
    public DrylVoiceOptions Options { get; }

    /// <summary>Where the session is in its life.</summary>
    public VoicePhase Phase { get; private set; } = VoicePhase.Idle;

    /// <summary>What is happening right now inside a live session.</summary>
    public VoiceActivity Activity { get; private set; } = VoiceActivity.Listening;

    /// <summary>Everything said in the current session, in order — both sides.</summary>
    public IReadOnlyList<DrylVoiceMessage> Transcript => _transcript;

    /// <summary>True while a session is connecting or running.</summary>
    public bool IsActive => Phase is not VoicePhase.Idle;

    /// <summary>
    /// Opens a session. The browser asks for the microphone, the server mints the token, and the
    /// two sides negotiate a peer connection.
    /// </summary>
    /// <param name="history">Earlier turns — typically the text conversation so far — replayed
    /// into the session so the voice knows what has already been discussed.</param>
    /// <param name="ct">Cancels the token request.</param>
    public async Task StartAsync(
        IEnumerable<DrylVoiceMessage>? history = null, CancellationToken ct = default)
    {
        if (IsActive || _starting) return;
        _starting = true;
        MarkConnecting();

        try
        {
            var token = await _runner.MintTokenAsync(Options, ct).ConfigureAwait(false);

            _module ??= await _runner.Js
                .InvokeAsync<IJSObjectReference>("import", ct, ModulePath).ConfigureAwait(false);
            _attached = true;
            _self ??= DotNetObjectReference.Create(this);

            await _module.InvokeVoidAsync("start", ct, token, new
            {
                baseUrl = Options.BaseUrl.TrimEnd('/'),
                idleMs = (int)Options.IdleTimeout.TotalMilliseconds,
                maxMs = (int)Options.MaxDuration.TotalMilliseconds,
                history = (history ?? Array.Empty<DrylVoiceMessage>())
                    .Select(m => new { role = m.Role.ToString(), text = m.Text })
                    .ToArray(),
            }, _self).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            OnClosed();
        }
        catch (JSDisconnectedException)
        {
            OnClosed();                                   // circuit gone; nothing to report to
        }
        catch (Exception ex)
        {
            OnFailed(ex.Message);
        }
        finally
        {
            _starting = false;
        }
    }

    /// <summary>Ends the session and releases the microphone.</summary>
    public async Task StopAsync()
    {
        if (Phase is VoicePhase.Idle) return;
        Phase = VoicePhase.Closing;
        Raise();

        if (_attached && _module is not null)
        {
            try { await _module.InvokeVoidAsync("stop").ConfigureAwait(false); }
            catch (JSDisconnectedException) { /* circuit gone */ }
            catch (JSException) { /* already torn down */ }
        }

        OnClosed();
    }

    // ── Reported by the browser ──────────────────────────────────────────────

    /// <summary>Internal: the run enters the connecting phase. Public surface is <see cref="StartAsync"/>.</summary>
    internal void MarkConnecting()
    {
        Phase = VoicePhase.Connecting;
        Activity = VoiceActivity.Listening;
        Error = null;                 // a new attempt is not still carrying the old failure
        _transcript.Clear();          // a session is a conversation; a new one starts empty
        State = AiState.Thinking;
        Raise();
    }

    /// <summary>The data channel is open — the conversation has started.</summary>
    [JSInvokable]
    public void OnConnected()
    {
        Phase = VoicePhase.Live;
        Activity = VoiceActivity.Listening;
        Sync();
    }

    /// <summary>Who is doing what right now. Values are <see cref="VoiceActivity"/> names.</summary>
    [JSInvokable]
    public void OnActivity(string activity)
    {
        if (Phase is not VoicePhase.Live) return;
        if (Enum.TryParse<VoiceActivity>(activity, ignoreCase: true, out var parsed))
            Activity = parsed;
        Sync();
    }

    /// <summary>A finished transcript line. <paramref name="role"/> is a <see cref="VoiceRole"/> name.</summary>
    [JSInvokable]
    public void OnTranscript(string role, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;      // a cough transcribes to ""
        var parsed = Enum.TryParse<VoiceRole>(role, ignoreCase: true, out var r) ? r : VoiceRole.Assistant;
        _transcript.Add(new DrylVoiceMessage(parsed, text.Trim()));
        Raise();
    }

    /// <summary>
    /// Runs a tool the model asked for and returns the result as JSON for the data channel.
    /// </summary>
    /// <remarks>Never throws. A model waiting for a result that never arrives stops
    /// mid-conversation with no way back; an error it can read keeps it talking.</remarks>
    [JSInvokable]
    public async Task<string> OnToolCallAsync(string callId, string name, string argumentsJson)
    {
        var invocation = new DrylToolInvocation
        {
            CallId = callId,
            ToolName = name,
            Arguments = argumentsJson,
        };
        AddToolCall(invocation);

        var tool = Options.FindTool(name);
        if (tool is null)
        {
            invocation.Error = $"Unknown tool \"{name}\".";
            Raise();
            return Fail(invocation.Error);
        }

        try
        {
            var result = await tool.InvokeAsync(ParseArguments(argumentsJson)).ConfigureAwait(false);
            var json = result as string ?? JsonSerializer.Serialize(result);
            invocation.Result = json;
            Raise();
            return json;
        }
        catch (Exception ex)
        {
            invocation.Error = ex.Message;
            Raise();
            return Fail(ex.Message);
        }

        static string Fail(string message) =>
            new JsonObject { ["error"] = message }.ToJsonString();
    }

    /// <summary>Something went wrong; the session is over.</summary>
    [JSInvokable]
    public void OnFailed(string message)
    {
        Error = new DrylRunError(message);
        Phase = VoicePhase.Idle;
        State = AiState.None;
        Raise();
    }

    /// <summary>The session ended — by the user, by a timeout, or by the network.</summary>
    [JSInvokable]
    public void OnClosed()
    {
        Phase = VoicePhase.Idle;
        Activity = VoiceActivity.Listening;
        State = AiState.None;
        Raise();
    }

    // The one place phase and activity turn into the shared AI vocabulary. No new states —
    // a voice session breathes with exactly the same language as every other AI surface.
    private void Sync()
    {
        State = Phase switch
        {
            VoicePhase.Idle => AiState.None,
            VoicePhase.Connecting => AiState.Thinking,
            VoicePhase.Closing => AiState.None,
            _ => Activity switch
            {
                VoiceActivity.Thinking => AiState.Thinking,
                VoiceActivity.Speaking => AiState.Streaming,
                _ => AiState.Active,
            },
        };
        Raise();
    }

    private static AIFunctionArguments ParseArguments(string json)
    {
        var arguments = new AIFunctionArguments();
        if (string.IsNullOrWhiteSpace(json)) return arguments;

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind is not JsonValueKind.Object) return arguments;

        foreach (var property in document.RootElement.EnumerateObject())
            arguments[property.Name] = property.Value.Clone();

        return arguments;
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        // _attached guards prerender: without a loaded module there is nothing to dispose, and
        // calling JS from a static render throws.
        if (_attached && _module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("stop").ConfigureAwait(false);
                await _module.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException) { /* circuit gone */ }
            catch (JSException) { /* already torn down */ }
        }

        _self?.Dispose();
        _self = null;
        _module = null;
        await base.DisposeAsync().ConfigureAwait(false);
    }
}
```

- [ ] **Step 4: `Create` an den Runner hängen**

In `DrylVoiceRunner.cs`, unter `internal IJSRuntime Js => _js;`:

```csharp
    /// <summary>
    /// Builds a voice session handle. Hold it in a service that outlives the page — a run in a
    /// component field dies on the first re-render that replaces the component.
    /// </summary>
    public DrylVoiceRun Create(DrylVoiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new DrylVoiceRun(this, options);
    }
```

- [ ] **Step 5: Tests laufen lassen**

Run: `dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~DrylVoiceRunTests"`
Expected: PASS (12 Tests). Beim Fehlschlag von `AIFunctionArguments`/`InvokeAsync` die tatsächliche Signatur in der installierten `Microsoft.Extensions.AI.Abstractions` prüfen und den Aufruf anpassen.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents tests/DRYL.Components.Tests
git commit -m "feat(voice): der Run trägt den Zustand und führt die Werkzeuge aus"
```

---

### Task 4: Der Browserteil

**Files:**
- Create: `DRYL.Components.Agents/wwwroot/js/dryl-voice.js`

**Interfaces:**
- Consumes: Die `[JSInvokable]`-Namen aus Task 3 — `OnConnected`, `OnActivity`, `OnTranscript`, `OnToolCallAsync`, `OnFailed`, `OnClosed`.
- Produces: Exportierte Funktionen `start(token, config, dotNet)`, `stop()`, `attachOrb(element)`.

Dieser Task hat keinen automatisierten Test — WebRTC lässt sich in bUnit nicht fahren. Die Prüfung ist die Laufzeitprüfung in Task 8. Deshalb ist der Code hier vollständig ausgeschrieben; nichts daran darf improvisiert werden.

- [ ] **Step 1: `dryl-voice.js` schreiben**

```javascript
// dryl-voice.js — the browser half of a DRYL voice session.
//
// Holds a WebRTC peer connection straight to the realtime API: the microphone track goes out,
// the model's voice comes back as a remote track, and a data channel called "oai-events"
// carries the JSON both ways. .NET never sees a byte of audio — it only learns who is talking
// and which tool was asked for.
//
// The levels are the reason this file exists at all. Measuring them here and writing a CSS
// variable straight onto the orb keeps a 60-per-second signal out of the Blazor circuit; the
// same value shipped over interop would be sixty renders a second for a decoration.

let session = null;   // only one voice session per page — a second microphone is not a feature
let orb = null;

/** Points the level meter at the orb element. Safe to call before or after start(). */
export function attachOrb(element) {
    orb = element || null;
    if (!orb) return;
    orb.style.setProperty('--voice-level', '0');
}

/** Opens a session. `token` is the ephemeral ek_… secret; the session config rides inside it. */
export async function start(token, config, dotNet) {
    if (session) return;

    const state = {
        dotNet,
        pc: null,
        channel: null,
        mic: null,
        audio: null,
        ctx: null,
        raf: 0,
        idleTimer: 0,
        maxTimer: 0,
        closed: false,
        speaking: false,
        // Assistant text arrives as deltas keyed by item; a turn is only worth a transcript
        // line once it is finished.
        answers: new Map(),
    };
    session = state;

    try {
        state.mic = await navigator.mediaDevices.getUserMedia({
            audio: { echoCancellation: true, noiseSuppression: true, autoGainControl: true },
        });
    } catch (err) {
        session = null;
        // A denied microphone is the single most likely failure, and the browser's own message
        // ("Permission denied") tells the user nothing about what to do.
        await report(dotNet, 'OnFailed', err && err.name === 'NotAllowedError'
            ? 'Kein Zugriff auf das Mikrofon. Erlaube ihn in den Browser-Einstellungen und starte neu.'
            : `Das Mikrofon ließ sich nicht öffnen: ${err?.message ?? err}`);
        return;
    }

    try {
        const pc = new RTCPeerConnection();
        state.pc = pc;

        // The model's voice. Kept out of the document flow — it is audio, it has nothing to show.
        const audio = document.createElement('audio');
        audio.autoplay = true;
        audio.style.display = 'none';
        document.body.appendChild(audio);
        state.audio = audio;

        pc.ontrack = (event) => {
            audio.srcObject = event.streams[0];
            meter(state, event.streams[0], 'out');
        };

        pc.addTrack(state.mic.getAudioTracks()[0], state.mic);
        meter(state, state.mic, 'in');

        pc.oniceconnectionstatechange = () => {
            if (pc.iceConnectionState === 'failed' || pc.iceConnectionState === 'closed') {
                teardown(state, 'OnClosed');
            }
        };

        const channel = pc.createDataChannel('oai-events');
        state.channel = channel;
        channel.onmessage = (event) => handle(state, event.data);
        channel.onopen = async () => {
            seed(state, config.history);
            await report(state.dotNet, 'OnConnected');
            idle(state, config.idleMs);
        };

        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);

        const response = await fetch(`${config.baseUrl}/realtime/calls`, {
            method: 'POST',
            body: offer.sdp,
            headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/sdp' },
        });

        if (!response.ok) {
            throw new Error(`Die Verbindung wurde abgelehnt (${response.status}).`);
        }

        await pc.setRemoteDescription({ type: 'answer', sdp: await response.text() });

        if (config.maxMs > 0) {
            state.maxTimer = setTimeout(() => teardown(state, 'OnClosed'), config.maxMs);
        }
    } catch (err) {
        const message = err?.message ?? String(err);
        teardown(state, null);
        await report(dotNet, 'OnFailed', message);
    }
}

/** Ends the session and releases the microphone. */
export function stop() {
    if (session) teardown(session, null);
}

// ── events ───────────────────────────────────────────────────────────────────

function handle(state, raw) {
    let event;
    try { event = JSON.parse(raw); } catch { return; }

    switch (event.type) {
        case 'input_audio_buffer.speech_started':
            touch(state);
            report(state.dotNet, 'OnActivity', 'UserSpeaking');
            break;

        case 'input_audio_buffer.speech_stopped':
            report(state.dotNet, 'OnActivity', 'Thinking');
            break;

        case 'response.output_audio.delta':
            // Only the first delta of a turn is a state change; the rest are just audio.
            if (!state.speaking) {
                state.speaking = true;
                report(state.dotNet, 'OnActivity', 'Speaking');
            }
            break;

        case 'response.output_audio_transcript.delta':
            state.answers.set(event.item_id, (state.answers.get(event.item_id) ?? '') + (event.delta ?? ''));
            break;

        case 'response.output_audio_transcript.done':
            if (event.transcript) state.answers.set(event.item_id, event.transcript);
            break;

        case 'conversation.item.input_audio_transcription.completed':
            report(state.dotNet, 'OnTranscript', 'User', event.transcript ?? '');
            break;

        case 'response.done':
            state.speaking = false;
            flush(state, event);
            calls(state, event);
            touch(state);
            report(state.dotNet, 'OnActivity', 'Listening');
            break;

        case 'error':
            report(state.dotNet, 'OnFailed', event.error?.message ?? 'Die Sprachsitzung meldete einen Fehler.');
            teardown(state, null);
            break;
    }
}

// Hand the finished spoken answer to .NET as one line. Prefers what the server said the
// transcript was; falls back to the deltas we accumulated.
function flush(state, event) {
    for (const item of event.response?.output ?? []) {
        if (item.type !== 'message') continue;
        const spoken = (item.content ?? [])
            .map((part) => part.transcript ?? part.text ?? '')
            .join('')
            .trim();
        const text = spoken || (state.answers.get(item.id) ?? '').trim();
        if (text) report(state.dotNet, 'OnTranscript', 'Assistant', text);
        state.answers.delete(item.id);
    }
}

// Every function call in a finished response, executed server-side and answered on the channel.
function calls(state, event) {
    const pending = (event.response?.output ?? []).filter((item) => item.type === 'function_call');
    if (pending.length === 0) return;

    report(state.dotNet, 'OnActivity', 'Thinking');

    Promise.all(pending.map(async (call) => {
        let output;
        try {
            output = await state.dotNet.invokeMethodAsync(
                'OnToolCallAsync', call.call_id, call.name, call.arguments ?? '{}');
        } catch (err) {
            // .NET itself fell over (circuit gone, serialisation). The model still needs an
            // answer or the conversation stops dead.
            output = JSON.stringify({ error: err?.message ?? 'Der Werkzeugaufruf schlug fehl.' });
        }
        send(state, {
            type: 'conversation.item.create',
            item: { type: 'function_call_output', call_id: call.call_id, output },
        });
    })).then(() => send(state, { type: 'response.create' }));
}

// Replays the text conversation into the session so the voice knows what was written.
function seed(state, history) {
    for (const turn of history ?? []) {
        if (!turn.text) continue;
        send(state, {
            type: 'conversation.item.create',
            item: {
                type: 'message',
                role: turn.role === 'User' ? 'user' : 'assistant',
                content: [{ type: turn.role === 'User' ? 'input_text' : 'output_text', text: turn.text }],
            },
        });
    }
}

function send(state, payload) {
    if (state.channel?.readyState === 'open') state.channel.send(JSON.stringify(payload));
}

async function report(dotNet, method, ...args) {
    try { await dotNet.invokeMethodAsync(method, ...args); }
    catch { /* circuit gone — the page is on its way out anyway */ }
}

// ── levels ───────────────────────────────────────────────────────────────────

// One AnalyserNode per direction, both feeding the same CSS variable on the orb: the loudest
// of the two wins, because at any moment only one side is really talking.
function meter(state, stream, direction) {
    try {
        state.ctx ??= new (window.AudioContext || window.webkitAudioContext)();
        const analyser = state.ctx.createAnalyser();
        analyser.fftSize = 256;
        state.ctx.createMediaStreamSource(stream).connect(analyser);
        state[direction] = { analyser, buffer: new Uint8Array(analyser.frequencyBinCount), level: 0 };
        if (!state.raf) state.raf = requestAnimationFrame(() => tick(state));
    } catch { /* no Web Audio — the orb simply breathes without a level */ }
}

function tick(state) {
    if (state.closed) return;

    for (const direction of ['in', 'out']) {
        const meterState = state[direction];
        if (!meterState) continue;
        meterState.analyser.getByteTimeDomainData(meterState.buffer);
        let peak = 0;
        for (const sample of meterState.buffer) peak = Math.max(peak, Math.abs(sample - 128));
        // Smoothed: a raw peak flickers, and a flickering orb reads as broken rather than alive.
        meterState.level += (Math.min(1, peak / 48) - meterState.level) * 0.25;
    }

    if (orb) {
        const level = Math.max(state.in?.level ?? 0, state.out?.level ?? 0);
        orb.style.setProperty('--voice-level', level.toFixed(3));
    }

    state.raf = requestAnimationFrame(() => tick(state));
}

// ── lifetime ─────────────────────────────────────────────────────────────────

function idle(state, ms) {
    if (!ms || ms <= 0) return;
    state.idleMs = ms;
    touch(state);
}

function touch(state) {
    if (!state.idleMs) return;
    clearTimeout(state.idleTimer);
    state.idleTimer = setTimeout(() => teardown(state, 'OnClosed'), state.idleMs);
}

function teardown(state, notify) {
    if (state.closed) return;
    state.closed = true;

    clearTimeout(state.idleTimer);
    clearTimeout(state.maxTimer);
    if (state.raf) cancelAnimationFrame(state.raf);

    try { state.channel?.close(); } catch { /* already gone */ }
    try { state.pc?.close(); } catch { /* already gone */ }
    for (const track of state.mic?.getTracks() ?? []) track.stop();
    try { state.ctx?.close(); } catch { /* already gone */ }
    state.audio?.remove();
    orb?.style.setProperty('--voice-level', '0');

    if (session === state) session = null;
    if (notify) report(state.dotNet, notify);
}
```

- [ ] **Step 2: Prüfen, dass das Modul syntaktisch trägt**

Run: `node --check DRYL.Components.Agents/wwwroot/js/dryl-voice.js`
Expected: keine Ausgabe (Node kann ESM-Syntax mit `--check` prüfen; meldet er „Cannot use import statement", die Datei stattdessen mit `node --input-type=module --check < datei` prüfen oder den Schritt überspringen und den Fehler in Task 8 im Browser sehen).

- [ ] **Step 3: Commit**

```bash
git add DRYL.Components.Agents/wwwroot/js/dryl-voice.js
git commit -m "feat(voice): der Browser hält den Medienpfad"
```

---

### Task 5: Der Orb

**Files:**
- Create: `DRYL.Components.Agents/Voice/DrylVoiceOrb.razor`
- Create: `DRYL.Components.Agents/Voice/DrylVoiceOrb.razor.css`
- Test: `tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceOrbTests.cs`

**Interfaces:**
- Consumes: `DrylVoiceRun` (Task 3), Kern-Primitive `.ai-aura`, `.ai-aura-ring`, `.ai-aura-comet`, `.ai-aura-glow`.
- Produces: `DrylVoiceOrb` mit `[Parameter] DrylVoiceRun? Run` und `[Parameter] int Size` (Vorgabe 96).

- [ ] **Step 1: Den fehlschlagenden Test schreiben**

`tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceOrbTests.cs`:

```csharp
using Bunit;
using DRYL.Components.Agents;
using Xunit;

namespace DRYL.Components.Tests.Agents.Voice;

/// <summary>The orb is the voice made visible — and it says it in the shared AI language,
/// not in one it invented for itself.</summary>
public class DrylVoiceOrbTests : BunitContext
{
    public DrylVoiceOrbTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static DrylVoiceRun LiveRun(VoiceActivity activity)
    {
        var run = new DrylVoiceRunner(new NoopJsRuntime())
            .Create(new DrylVoiceOptions { ApiKey = "sk-test" });
        run.OnConnected();
        run.OnActivity(activity.ToString());
        return run;
    }

    [Fact]
    public void It_wears_the_shared_aura_primitives()
    {
        var cut = Render<DrylVoiceOrb>(p => p.Add(x => x.Run, LiveRun(VoiceActivity.Listening)));

        var orb = cut.Find(".voice-orb");
        Assert.Contains("ai-aura", orb.ClassList);
        Assert.NotNull(cut.Find(".voice-orb .ai-aura-ring"));
        Assert.NotNull(cut.Find(".voice-orb .ai-aura-comet"));
        Assert.NotNull(cut.Find(".voice-orb .ai-aura-glow"));
    }

    [Fact]
    public void Speaking_streams_and_thinking_thinks()
    {
        Assert.Contains("ai-streaming",
            Render<DrylVoiceOrb>(p => p.Add(x => x.Run, LiveRun(VoiceActivity.Speaking)))
                .Find(".voice-orb").ClassList);

        Assert.Contains("ai-thinking",
            Render<DrylVoiceOrb>(p => p.Add(x => x.Run, LiveRun(VoiceActivity.Thinking)))
                .Find(".voice-orb").ClassList);
    }

    [Fact]
    public void It_is_decorative_and_stays_out_of_the_accessibility_tree()
    {
        // The spoken state is announced by the dock's aria-live status line. An orb that
        // announced itself as well would say everything twice.
        var cut = Render<DrylVoiceOrb>(p => p.Add(x => x.Run, LiveRun(VoiceActivity.Listening)));

        Assert.Equal("true", cut.Find(".voice-orb").GetAttribute("aria-hidden"));
    }
}
```

- [ ] **Step 2: Test laufen lassen, Fehlschlag bestätigen**

Run: `dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~DrylVoiceOrbTests"`
Expected: Compile-Fehler — `DrylVoiceOrb` existiert nicht.

- [ ] **Step 3: `DrylVoiceOrb.razor` schreiben**

```razor
@namespace DRYL.Components.Agents
@using DRYL.Components.Ai
@inject IJSRuntime JS
@implements IAsyncDisposable

@*  ─────────────────────────────────────────────────────────
    DrylVoiceOrb — a voice, made visible.

    Nothing here is new: it is the shared .ai-aura vocabulary bent into a
    circle. The state classes are the same ones every other AI surface wears,
    so a listening assistant reads the same here as anywhere else.

    The level is the one thing the orb owns, and it never touches .NET: the
    JS module writes --voice-level straight onto this element, and the CSS
    turns it into a scale. A level shipped over interop would re-render the
    circuit sixty times a second for a decoration.
    ───────────────────────────────────────────────────────── *@

<div class="voice-orb @StateCss" @ref="_el" aria-hidden="true">
    <div class="ai-aura-ring"></div>
    <div class="ai-aura-comet"></div>
    <div class="ai-aura-glow"></div>
    <div class="voice-orb-core"></div>
</div>

@code {
    /// <summary>The session the orb reflects. Without it the orb sits still.</summary>
    [Parameter] public DrylVoiceRun? Run { get; set; }

    /// <summary>Diameter in pixels. Default 96.</summary>
    [Parameter] public int Size { get; set; } = 96;

    private const string ModulePath = "./_content/DRYL.Components.Agents/js/dryl-voice.js";

    private ElementReference _el;
    private IJSObjectReference? _module;
    private bool _attached;

    private string StateCss
    {
        get
        {
            var state = Run?.State ?? AiState.None;
            var css = "ai-aura" + state switch
            {
                AiState.Thinking => " ai-thinking",
                AiState.Streaming => " ai-streaming",
                AiState.Generated => " ai-generated",
                _ => string.Empty,
            };
            return css;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        // Pointing the meter at the orb is a one-time wiring, not a per-frame call.
        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _attached = true;
            await _module.InvokeVoidAsync("attachOrb", _el);
        }
        catch (JSDisconnectedException) { /* circuit gone */ }
        catch (InvalidOperationException) { /* prerender — no JS */ }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_attached || _module is null) return;

        try
        {
            await _module.InvokeVoidAsync("attachOrb", (ElementReference?)null);
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { /* circuit gone */ }
        catch (JSException) { /* already torn down */ }
    }
}
```

- [ ] **Step 4: `DrylVoiceOrb.razor.css` schreiben**

```css
/* DrylVoiceOrb — the shared aura, bent into a circle. Tokens only; no new colour, no new
   timing. Everything that moves here either comes from .ai-aura* or from --voice-level, which
   the JS module writes directly onto the element. */

.voice-orb {
    position: relative;
    isolation: isolate;
    width: var(--orb-size, 96px);
    height: var(--orb-size, 96px);
    border-radius: var(--r-pill);
    display: grid;
    place-items: center;
    /* The level is a compositor property and nothing else: a scale, never a width or a
       box-shadow, so a talking orb costs a transform and not a layout pass. */
    scale: calc(1 + var(--voice-level, 0) * 0.16);
    transition: scale var(--dur-fast) var(--ease-out);
}

/* The body of the orb: a glass sphere, not an accent fill (rule 2.4). The accent shows up in
   the aura ring and the glow that the primitives already draw around it. */
.voice-orb-core {
    position: absolute;
    inset: 0;
    border-radius: inherit;
    background:
        radial-gradient(120% 120% at 30% 25%, var(--glass-2), transparent 70%),
        var(--glass-1);
    border: 1px solid var(--line);
    backdrop-filter: var(--glass-fx-float);
    z-index: 1;
}

@media (prefers-reduced-motion: reduce) {
    .voice-orb {
        scale: 1;
        transition: none;
    }
}
```

> `--orb-size` wird vom `Size`-Parameter gesetzt — dafür bekommt das Wurzel-Element im Razor ein Inline-`style`. Ergänze in `DrylVoiceOrb.razor` am Wurzel-`div`:
> `style="@FormattableString.Invariant($"--orb-size: {Size}px")"`.
> Das ist eine Layout-Einzelfallgröße, kein Farbwert — nach CLAUDE.md §6 erlaubt. `FormattableString.Invariant` ist Pflicht, sonst macht die deutsche Locale aus einer Zahl irgendwann ein Komma.

- [ ] **Step 5: Tests laufen lassen**

Run: `dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~DrylVoiceOrbTests"`
Expected: PASS (3 Tests).

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents tests/DRYL.Components.Tests
git commit -m "feat(voice): der Orb macht die Stimme sichtbar"
```

---

### Task 6: Die Dock-Übernahme

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasDock.razor`
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasDock.razor.css`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasDockTests.cs` (ergänzen)

**Interfaces:**
- Consumes: `DrylVoiceRun`, `VoicePhase`, `VoiceActivity` (Task 3), `DrylVoiceOrb` (Task 5).
- Produces: `DrylCanvasDock.Voice` (`DrylVoiceRun?`), `DrylCanvasDock.VoiceLabel` (`string`).

- [ ] **Step 1: Die fehlschlagenden Tests schreiben**

An `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasDockTests.cs` anhängen:

```csharp
    private static DrylVoiceRun VoiceRun()
    {
        var runner = new DrylVoiceRunner(new DRYL.Components.Tests.Agents.Voice.NoopJsRuntime());
        return runner.Create(new DrylVoiceOptions { ApiKey = "sk-test" });
    }

    [Fact]
    public void Without_a_voice_run_the_dock_is_exactly_what_it_was()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, new DrylCanvasRun()));

        Assert.Empty(cut.FindAll(".dock-voice-toggle"));
        Assert.NotNull(cut.Find(".composer"));
    }

    [Fact]
    public void An_idle_voice_run_offers_a_microphone()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, VoiceRun())
            .Add(x => x.VoiceLabel, "Mit dem Assistenten sprechen"));

        var button = cut.Find(".dock-voice-toggle");
        Assert.Equal("Mit dem Assistenten sprechen", button.GetAttribute("aria-label"));
        Assert.NotNull(cut.Find(".composer"));
    }

    [Fact]
    public void A_live_session_takes_the_dock_over()
    {
        var voice = VoiceRun();
        voice.OnConnected();

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, voice));

        Assert.NotNull(cut.Find(".dock-voice .voice-orb"));
        Assert.NotNull(cut.Find(".dock-voice-stop"));
        // The composer is gone: you are talking, not typing.
        Assert.Empty(cut.FindAll(".composer"));
    }

    [Fact]
    public void The_status_line_says_what_the_voice_is_doing()
    {
        var voice = VoiceRun();
        voice.OnConnected();
        voice.OnActivity(nameof(VoiceActivity.Speaking));

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, voice));

        Assert.Contains("Speaking", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void The_last_spoken_line_is_shown_under_the_orb()
    {
        var voice = VoiceRun();
        voice.OnConnected();
        voice.OnTranscript(nameof(VoiceRole.User), "Zeig mir die Projekte");
        voice.OnTranscript(nameof(VoiceRole.Assistant), "Ist gebaut.");

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, voice));

        Assert.Contains("Ist gebaut.", cut.Find(".dock-voice-line").TextContent);
    }

    [Fact]
    public void A_voice_failure_is_reported_where_every_other_failure_is()
    {
        var voice = VoiceRun();
        voice.OnFailed("Kein Zugriff auf das Mikrofon.");

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, voice));

        var status = cut.Find(".dock-status");
        Assert.Contains("Mikrofon", status.TextContent);
        Assert.Contains("is-error", cut.Find(".dock-status").ClassList);
    }
```

> `.composer` ist die Wurzelklasse von `DrylChatComposer`. Vor dem Schreiben in `DRYL.Components/Components/AI/DrylChatComposer.razor` nachsehen und die tatsächliche Klasse einsetzen, falls sie anders heißt.

- [ ] **Step 2: Tests laufen lassen, Fehlschlag bestätigen**

Run: `dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~DrylCanvasDockTests"`
Expected: Compile-Fehler — `Voice` ist kein Parameter von `DrylCanvasDock`.

- [ ] **Step 3: Die Parameter und die Abo-Logik ergänzen**

In `DrylCanvasDock.razor`, im `@code`-Block neben den anderen Parametern:

```csharp
    /// <summary>
    /// A voice session for this dock. With it the head grows a microphone button, and a live
    /// session takes the dock over: composer, suggestions and context chip step aside for the
    /// orb. Without it nothing about the dock changes.
    /// </summary>
    /// <remarks>Create it with <c>DrylVoiceRunner.Create(...)</c> and hold it in a service, not
    /// in the page — it should survive a re-render like the canvas run does.</remarks>
    [Parameter] public DrylVoiceRun? Voice { get; set; }

    /// <summary>Label of the microphone button — tooltip and aria-label both.</summary>
    [Parameter] public string VoiceLabel { get; set; } = "Talk to the assistant";
```

Felder neben den anderen `_subscribed*`-Feldern:

```csharp
    private DrylVoiceRun? _subscribedVoice;
```

In `OnParametersSet`, nach dem `Selection`-Block:

```csharp
        if (!ReferenceEquals(_subscribedVoice, Voice))
        {
            if (_subscribedVoice is not null) _subscribedVoice.OnChange -= HandleChange;
            _subscribedVoice = Voice;
            if (_subscribedVoice is not null) _subscribedVoice.OnChange += HandleChange;
        }
```

In `DisposeAsync`, neben den anderen Abmeldungen:

```csharp
        if (_subscribedVoice is not null) _subscribedVoice.OnChange -= HandleChange;
```

Hilfsglieder unten im `@code`-Block:

```csharp
    private bool VoiceActive => Voice?.IsActive == true;

    private const string VoiceStopLabel = "End voice session";

    // What the voice is doing, in the dock's own voice. Kept English like every other string in
    // this component — the host translates by overriding Status if it wants to.
    private string? VoiceStatus => Voice is null ? null : Voice.Phase switch
    {
        VoicePhase.Connecting => "Connecting…",
        VoicePhase.Closing => "Ending…",
        VoicePhase.Live => Voice.Activity switch
        {
            VoiceActivity.UserSpeaking => "Listening…",
            VoiceActivity.Thinking => "Thinking…",
            VoiceActivity.Speaking => "Speaking…",
            _ => "Listening…",
        },
        _ => null,
    };

    // The last thing said, whoever said it — one line under the orb, so the user can see that
    // they were understood without opening the whole log.
    private string? VoiceLine =>
        Voice is { Transcript.Count: > 0 } voice ? voice.Transcript[^1].Text : null;

    private Task StartVoiceAsync() => Voice?.StartAsync() ?? Task.CompletedTask;

    private Task StopVoiceAsync() => Voice?.StopAsync() ?? Task.CompletedTask;
```

- [ ] **Step 4: Status und Aura um die Stimme erweitern**

In `DrylCanvasDock.razor` `HasError` ersetzen durch:

```csharp
    private bool HasError => string.IsNullOrWhiteSpace(Status)
        && (Run?.Error is not null || Voice?.Error is not null);
```

In `DockAi` die Stimme vorziehen — solange sie läuft, ist sie das, was die KI gerade tut:

```csharp
    private AiState DockAi =>
        Voice?.IsActive == true ? Voice.State
        : !Working ? AiState.None
        : Run?.State is AiState.Streaming or AiState.Thinking ? Run.State
        : Busy ? AiState.Thinking
        : Run?.State ?? AiState.None;
```

In `StatusText`, direkt nach der `Status`-Zeile am Anfang des Getters:

```csharp
            if (!string.IsNullOrWhiteSpace(Status)) return Status!;
            if (VoiceStatus is { } spoken) return spoken;
            if (Voice?.Error is { } voiceError) return voiceError.Message;
            if (Run?.Error is { } error) return error.Message;
```

- [ ] **Step 5: Das Markup ergänzen**

In `DrylCanvasDock.razor`, im `dock-head`, **vor** dem `Log`-Umschalter (also direkt nach dem `Actions`-Block):

```razor
                @if (Voice is not null && !VoiceActive)
                {
                    <DrylTooltip Text="@VoiceLabel">
                        <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                    Size="DrylButton.ButtonSize.Small"
                                    AriaLabel="@VoiceLabel"
                                    Class="dock-voice-toggle"
                                    OnClick="StartVoiceAsync">
                            <DrylIcon Name="Microphone" Size="14" />
                        </DrylButton>
                    </DrylTooltip>
                }
```

> `DrylIcon Name="Microphone"` — im Icon-Katalog nachsehen, wie das Mikrofon dort tatsächlich heißt (`grep -o 'Microphone\|Mic' DRYL.Components/**/DrylIcon*`). Gibt es keins, nimm `Sparkle` **nicht** als Ersatz, sondern lege das fehlende Icon an; ein Sprachmodus ohne Mikrofon-Symbol ist nicht auffindbar.

Den Kontext-Chip, die Vorschläge und den Composer in eine Presence hüllen, die bei aktiver Stimme weicht — das vorhandene Markup bleibt, nur die `Visible`-Bedingungen bekommen `&& !VoiceActive`:

```razor
            <DrylPresence Visible="@(Selection?.HasSelection == true && !VoiceActive)" …>
            <DrylPresence Visible="@(Suggestions is not null && !VoiceActive)" …>
```

Der Composer stand bisher nackt im Panel; er wird eingehüllt:

```razor
            <DrylPresence Visible="@(!VoiceActive)" Transition="PresenceTransition.SlideUp"
                          Speed="PresenceSpeed.Fast">
                <DrylChatComposer @ref="_composer"
                                  @bind-Value="_draft"
                                  OnSend="SendAsync"
                                  Disabled="@Busy"
                                  Placeholder="@Placeholder"
                                  AriaLabel="@Title"
                                  Ai="@DockAi" />
            </DrylPresence>

            @*  The takeover: while the voice is live the dock is a voice panel. It arrives and
                leaves with a movement like every other row here. *@
            <DrylPresence Visible="@VoiceActive" Transition="PresenceTransition.Scale"
                          Speed="PresenceSpeed.Fast">
                <div class="dock-voice">
                    <DrylVoiceOrb Run="Voice" />

                    <div class="dock-voice-line">
                        <DrylPresence @key="VoiceLine" Visible Appear
                                      Transition="PresenceTransition.Fade" Speed="PresenceSpeed.Fast">
                            <span>@VoiceLine</span>
                        </DrylPresence>
                    </div>

                    <DrylButton Variant="DrylButton.ButtonVariant.Secondary"
                                Size="DrylButton.ButtonSize.Small"
                                LeadingIcon="X"
                                Class="dock-voice-stop"
                                OnClick="StopVoiceAsync">
                        @VoiceStopLabel
                    </DrylButton>
                </div>
            </DrylPresence>
```

- [ ] **Step 6: CSS ergänzen**

An `DrylCanvasDock.razor.css` anhängen:

```css
/* The takeover: while the voice is live this is what the dock is. Centred, generous, quiet —
   there is one thing happening and it is the conversation. */
.dock-voice {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: var(--sp-3);
    padding: var(--sp-4) var(--sp-2) var(--sp-3);
}

/* The last spoken line. Two lines at most: this is a glance, the full transcript is in the log.
   A fixed min-height keeps the panel from jumping every time a line arrives or leaves. */
.dock-voice-line {
    min-height: 2.6em;
    max-width: 34ch;
    text-align: center;
    font-size: .9rem;
    line-height: 1.3;
    color: var(--fg-muted);
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}
```

- [ ] **Step 7: Tests laufen lassen**

Run: `dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~DrylCanvasDockTests"`
Expected: PASS — die bestehenden Dock-Tests **und** die sechs neuen. Bricht ein alter Test, ist die Rückwärtskompatibilität verletzt; dann nicht den alten Test anpassen, sondern das Markup.

- [ ] **Step 8: Alle Tests laufen lassen**

Run: `dotnet test tests/DRYL.Components.Tests`
Expected: PASS.

- [ ] **Step 9: Commit**

```bash
git add DRYL.Components.Agents tests/DRYL.Components.Tests
git commit -m "feat(voice): das Dock wird zum Sprach-Panel"
```

---

### Task 7: Portfolio — der Assistent bekommt eine Stimme

**Files:**
- Modify: `DRYL.Portfolio/Services/Ai/OpenAiOptions.cs`
- Modify: `DRYL.Portfolio/Services/Ai/AssistantAgentService.cs`
- Modify: `DRYL.Portfolio/Features/Admin/AiAssistant.razor`

**Interfaces:**
- Consumes: `DrylVoiceRunner.Create`, `DrylVoiceOptions`, `DrylVoiceRun`, `DrylCanvasDock.Voice` aus Tasks 1–6.
- Produces: `AssistantAgentService.Voice` (`DrylVoiceRun`), `AssistantAgentService.VoiceHistory` (`IReadOnlyList<DrylVoiceMessage>`), `AssistantAgentService.AbsorbVoice(DrylVoiceRun)`.

> Dieser Task läuft im Repo `DRYL.Portfolio` auf einem eigenen Branch. Der ProjectReference zeigt schon auf die lokale `DRYL.Components.Agents`, es ist also kein NuGet-Zwischenschritt nötig.

- [ ] **Step 1: Branch anlegen**

```bash
cd ../DRYL.Portfolio && git checkout -b feat/assistant-voice
```

- [ ] **Step 2: Realtime-Modell in die Optionen**

In `Services/Ai/OpenAiOptions.cs`:

```csharp
    /// <summary>Realtime model for the spoken assistant. Also <c>gpt-realtime-2</c> and
    /// <c>gpt-realtime-2.1-mini</c> (rund ein Drittel der Kosten).</summary>
    public string RealtimeModel { get; set; } = "gpt-realtime-2.1";
```

Anschließend prüfen, wo `OpenAiOptions` gebunden wird (`Program.cs`, Suche nach `OpenAiOptions`), und die `.env`-Auflösung um `OPENAI_REALTIME_MODEL` erweitern — im exakt gleichen Muster wie `OPENAI_MODEL`.

- [ ] **Step 3: Den Sprach-Run im Assistenten-Service anlegen**

In `Services/Ai/AssistantAgentService.cs` den Konstruktor um `DrylVoiceRunner voice` erweitern und darunter ergänzen:

```csharp
    private DrylVoiceRun? _voice;
    private readonly List<DrylVoiceMessage> _voiceHistory = new();

    /// <summary>
    /// Die gesprochene Seite desselben Assistenten: gleiche Werkzeuge, gleicher Systemprompt,
    /// gleicher Verlauf. Circuit-scoped wie <see cref="Canvas"/>, damit ein Seitenwechsel eine
    /// laufende Unterhaltung nicht abschneidet.
    /// </summary>
    public DrylVoiceRun Voice
    {
        get
        {
            if (_voice is not null) return _voice;
            EnsureAgent();                       // baut die Werkzeugliste, an der auch die Stimme hängt

            _voice = voice.Create(new DrylVoiceOptions
            {
                ApiKey = options.ApiKey ?? string.Empty,
                Model = options.RealtimeModel,
                Instructions = BuildInstructions() + VoiceAddendum,
                Voice = "marin",
                Language = "de",
                Tools = _voiceTools,
            });
            return _voice;
        }
    }

    /// <summary>Der bisherige Verlauf beider Kanäle, den eine neue Sprachsitzung mitbekommt.</summary>
    public IReadOnlyList<DrylVoiceMessage> VoiceHistory => _voiceHistory;

    /// <summary>
    /// Übernimmt das Gesprochene in den gemeinsamen Verlauf, sobald eine Sitzung endet. Damit
    /// weiß die nächste Sitzung — gesprochen oder getippt — worüber geredet wurde.
    /// </summary>
    public void AbsorbVoice(DrylVoiceRun run)
    {
        foreach (var line in run.Transcript)
        {
            if (_voiceHistory.Count > 0 &&
                _voiceHistory[^1].Role == line.Role &&
                _voiceHistory[^1].Text == line.Text)
            {
                continue;                        // ein zweites Absorbieren derselben Sitzung
            }
            _voiceHistory.Add(line);
        }
    }

    // Was sich am Systemprompt ändert, wenn er vorgelesen statt gelesen wird. Der Rest ist
    // wortgleich der des Textmodus — es ist derselbe Assistent, nur mit Mund.
    private const string VoiceAddendum = """

        Du sprichst gerade — der Nutzer hört dich, er liest dich nicht.
        - Antworte kurz: ein bis drei Sätze. Was länger ist, gehört auf den Canvas, nicht ins Ohr.
        - Lies niemals Markdown, Aufzählungszeichen, Klammern oder JSON vor. Sprich in Sätzen.
        - Sag Zahlen aus, wie man sie spricht ("dreiundzwanzig Projekte", nicht "23").
        - Kündige längere Arbeit mit einem halben Satz an ("Moment, ich hole die Projekte"),
          bevor du ein Werkzeug aufrufst — Stille wirkt wie ein Abbruch.
        - Bau die Ansicht auf dem Canvas und beschreibe sie NICHT nach. Sag, dass sie da ist.
        """;
```

Dazu wird die Werkzeugliste festgehalten, damit Sprache und Text buchstäblich dieselbe bekommen. In `EnsureAgent()` vor dem `_agent = new ChatClientAgent(...)`:

```csharp
        _voiceTools = toolList;
```

und als Feld:

```csharp
    private List<AITool> _voiceTools = new();
```

In `Reset()` ergänzen:

```csharp
        var oldVoice = _voice;
        _voice = null;
        _voiceHistory.Clear();
        if (oldVoice is not null) _ = oldVoice.DisposeAsync();
```

- [ ] **Step 4: Die Seite verkabeln**

In `Features/Admin/AiAssistant.razor` am `DrylCanvasDock`:

```razor
    <DrylCanvasDock Run="Agent.Canvas"
                    Busy="_busy"
                    OnSend="SendAsync"
                    Selection="_selection"
                    Status="@PlanStatus"
                    Title="KI-Assistent"
                    Voice="@(Agent.IsConfigured ? Agent.Voice : null)"
                    VoiceLabel="Mit dem Assistenten sprechen"
                    Placeholder="Was soll ich bauen?">
```

Im `@code`-Block: die Sprach-Turns in den Verlauf holen, sobald eine Sitzung endet.

```csharp
    private VoicePhase _lastVoicePhase = VoicePhase.Idle;

    private void HandleVoiceChange()
    {
        var voice = Agent.Voice;

        // Der Übergang Live → Idle ist das Ende einer Sitzung: genau dann wandert das
        // Gesprochene in den gemeinsamen Verlauf, und nur dann.
        if (_lastVoicePhase is not VoicePhase.Idle && voice.Phase is VoicePhase.Idle)
        {
            Agent.AbsorbVoice(voice);
            foreach (var line in voice.Transcript)
            {
                _turns.Add(new Turn(
                    line.Role == VoiceRole.User ? line.Text : string.Empty,
                    /* placeholder */ null!));
            }
        }

        _lastVoicePhase = voice.Phase;
        _ = InvokeAsync(StateHasChanged);
    }
```

> **Halt.** Der `Turn`-Record dieser Seite trägt einen `DrylAgentRun`, den es für eine Sprachzeile nicht gibt — das obige `null!` würde beim Rendern werfen. Statt den Record zu verbiegen, bekommt die Seite eine eigene, schlichte Darstellung: `Turn` wird zu
> `private sealed record Turn(string UserText, DrylAgentRun? Run, DrylVoiceMessage? Spoken = null);`
> und die `Log`-Schleife rendert für `Spoken` eine `DrylMessage` mit der passenden Rolle und ohne Werkzeug-/Usage-Bausteine. Die bestehenden `new Turn(text, run)`-Aufrufe bleiben gültig.

Die konkrete Log-Ergänzung:

```razor
            @foreach (var turn in _turns)
            {
                @if (turn.Spoken is { } spoken)
                {
                    <DrylMessage @key="turn"
                                 Role="@(spoken.Role == VoiceRole.User ? MessageRole.User : MessageRole.Assistant)"
                                 Author="@(spoken.Role == VoiceRole.User ? "Du (gesprochen)" : "Assistent (gesprochen)")"
                                 AvatarIcon="@(spoken.Role == VoiceRole.User ? null : "Sparkle")">
                        @spoken.Text
                    </DrylMessage>
                }
                else if (turn.Run is { } run)
                {
                    @* … das bestehende Markup, unverändert … *@
                }
            }
```

und `HandleVoiceChange` entsprechend:

```csharp
            foreach (var line in voice.Transcript)
                _turns.Add(new Turn(string.Empty, null, line));
```

`OnInitializedAsync` abonniert:

```csharp
        if (Agent.IsConfigured) Agent.Voice.OnChange += HandleVoiceChange;
```

`DisposeAsync` meldet ab:

```csharp
        if (Agent.IsConfigured) Agent.Voice.OnChange -= HandleVoiceChange;
```

Und der Start bekommt den Verlauf mit — dafür startet die Seite die Sitzung selbst statt über den Dock-Knopf? **Nein.** Der Dock-Knopf ruft `StartAsync()` ohne Verlauf. Damit die Übergabe trotzdem passiert, bekommt `DrylCanvasDock` keinen zweiten Weg, sondern der Host setzt den Verlauf vorher: Ergänze in `DrylVoiceRun` (Task 3) eine Eigenschaft

```csharp
    /// <summary>Turns replayed into every new session — the conversation so far, from either
    /// channel. Set it before the dock's microphone button is pressed.</summary>
    public IEnumerable<DrylVoiceMessage>? SeedHistory { get; set; }
```

und in `StartAsync` `history ??= SeedHistory;` als erste Zeile nach dem Guard. Die Seite setzt in `OnInitializedAsync` und nach jeder Textrunde:

```csharp
        Agent.Voice.SeedHistory = Agent.VoiceHistory;
```

- [ ] **Step 5: Bauen**

Run: `cd ../DRYL.Portfolio && dotnet build`
Expected: 0 Fehler.

- [ ] **Step 6: Commit**

```bash
git add Services Features && git commit -m "feat(assistant): der Assistent bekommt eine Stimme"
```

> Wenn Step 4 die Eigenschaft `SeedHistory` in `DrylVoiceRun` nachträglich verlangt: diese Änderung gehört ins Components-Repo, mit eigenem Commit auf `feat/realtime-voice` und einem Test in `DrylVoiceRunTests` („`StartAsync` ohne Argument nimmt `SeedHistory`"). Nicht stillschweigend mit dem Portfolio-Commit vermischen.

---

### Task 8: Laufzeitprüfung, Doku, Version

**Files:**
- Modify: `CHANGELOG.md` (DRYL.Components)
- Modify: `DRYL.Components.Agents/DRYL.Components.Agents.csproj`
- Modify: `DRYL.Website` → `ComponentCatalog`

- [ ] **Step 1: Am laufenden Portfolio prüfen**

Voraussetzung: Postgres läuft (`docker compose -f docker-compose.dev.yml up -d`), `OPENAI_API_KEY` steht in `.env`.

```bash
cd ../DRYL.Portfolio && dotnet run
```

Auf `/admin/assistant` anmelden und der Reihe nach prüfen:

1. Der Mikrofon-Knopf steht im Dock-Kopf und hat einen Tooltip.
2. Klick → Browser fragt nach dem Mikrofon → Status „Connecting…", das Dock atmet.
3. Sprechen → Status „Listening…", der Orb reagiert auf die eigene Stimme.
4. Die Antwort ist hörbar, der Orb pulsiert dazu, der Status steht auf „Speaking…".
5. **In die Antwort hineinreden** — die Stimme bricht ab. Das ist der Kern des Ganzen.
6. „Zeig mir meine Projekte" → eine Ansicht entsteht auf dem Canvas, während geredet wird; im Log steht der Werkzeugaufruf.
7. „Lösch das Projekt X" → der Bestätigungsdialog erscheint.
8. Beenden → das Dock kommt als Composer zurück, das Transkript steht im Log.
9. Danach eine getippte Frage stellen, die sich auf das Gesprochene bezieht — die Antwort kennt es.
10. Beides in **hellem und dunklem** Modus ansehen (`data-dryl-mode` am `<html>` umschalten) und einmal bei 375 px Breite.

Was hier nicht funktioniert, wird gefixt, bevor irgendetwas als fertig gemeldet wird.

- [ ] **Step 2: CHANGELOG**

Unter `[Unreleased]` → `### Added`:

```markdown
- `DrylVoiceOrb` — Neue Komponente: die sichtbare Stimme einer Sprachsitzung, gebaut aus den vorhandenen `.ai-aura`-Primitiven; Pegel über eine CSS-Variable statt über den Circuit
- `DrylCanvasDock` — Neue Parameter `Voice` (`DrylVoiceRun`) und `VoiceLabel`: das Dock bekommt einen Mikrofon-Knopf und wird bei laufender Sitzung zum Sprach-Panel
- `DrylVoiceRunner` / `DrylVoiceRun` / `DrylVoiceOptions` — Sprachsitzungen über die OpenAI-Realtime-API (WebRTC): Modell, Stimme, Tempo, Tonalität, Turn-Detection und Werkzeuge werden in C# konfiguriert; der API-Schlüssel bleibt im Server, der Browser bekommt nur ein kurzlebiges Token
```

- [ ] **Step 3: Version**

`DRYL.Components.Agents.csproj`: `<Version>0.15.0</Version>` → `<Version>0.16.0</Version>`.
Im CHANGELOG den `[Unreleased]`-Block zu `## [0.16.0] - 2026-07-29` machen und ein leeres `[Unreleased]` darüber setzen — **nur**, wenn das Repo den Agents-Release im selben CHANGELOG führt; vorher nachsehen, wie die letzten Agents-Bumps dort notiert sind, und es genauso machen.

- [ ] **Step 4: ComponentCatalog**

Im Repo `DRYL.Website` auf einem Branch `docs/realtime-voice` den `ComponentCatalog` um `DrylVoiceOrb` erweitern und den `DrylCanvasDock`-Eintrag um den Sprachmodus ergänzen. Die vorhandenen Canvas-Einträge als Vorlage nehmen.

- [ ] **Step 5: Alles bauen und testen**

```bash
cd ../DRYL.Components && dotnet build && dotnet test tests/DRYL.Components.Tests
node scripts/check-light-sync.mjs
```

Expected: Build 0 Fehler, alle Tests grün, Light-Sync grün (sollte trivial grün sein — `dryl.css` wurde nicht angefasst).

- [ ] **Step 6: Commit**

```bash
git add CHANGELOG.md DRYL.Components.Agents/DRYL.Components.Agents.csproj
git commit -m "chore(voice): 0.16.0 — Changelog und Version"
```

---

## Self-Review

**Spec-Abdeckung**

| Spec-Abschnitt | Task |
|---|---|
| 5.1 `DrylVoiceOptions` | 1 |
| 5.2 `DrylVoiceRun`, Zustandsabbildung, keine Pegel im Run | 3 |
| 5.3 `DrylVoiceRunner`, Token, Werkzeugausführung | 2, 3 |
| 5.4 `dryl-voice.js`, WebRTC, Ereignisabbildung, Pegel | 4 |
| 5.5 Werkzeug-Brücke inkl. Fehlerrückmeldung | 3 (C#), 4 (JS) |
| 5.6 Übergabe hinein/hinaus | 3 (`SeedHistory`), 4 (`seed`), 7 (`AbsorbVoice`) |
| 5.7 Orb aus vorhandenen Primitiven | 5 |
| 5.8 Dock-Übernahme | 6 |
| 6 Fehler und Grenzen | 3 (`OnFailed`), 4 (`teardown`, Timer), 6 (Status) |
| 7 Sicherheit | 2 (Token, Header), 3 (`FindTool`) |
| 8 Tests | 1, 3, 5, 6 |
| 9 Version und Doku | 8 |

**Typ-Konsistenz** — `DrylVoiceMessage(VoiceRole, string)`, `VoicePhase`, `VoiceActivity`, `DrylVoiceRunner.Create`, `DrylVoiceRun.OnToolCallAsync(string,string,string) → Task<string>`, `DrylCanvasDock.Voice` sind in allen Tasks gleich geschrieben. Die JS-Seite ruft genau `OnConnected`, `OnActivity`, `OnTranscript`, `OnToolCallAsync`, `OnFailed`, `OnClosed` — dieselben sechs, die Task 3 als `[JSInvokable]` anlegt.

**Bekannte Unschärfen, die beim Umsetzen zu prüfen sind** (keine Platzhalter, sondern Stellen, an denen die Bibliotheksversion entscheidet):

1. `AIFunction.JsonSchema` / `InvokeAsync(AIFunctionArguments)` — Signatur in der installierten `Microsoft.Extensions.AI.Abstractions` 10.x verifizieren (Task 1 Step 5, Task 3 Step 5).
2. Der Klassenname der `DrylChatComposer`-Wurzel (`.composer`) — Task 6 Step 1.
3. Der Icon-Name fürs Mikrofon — Task 6 Step 5.
4. Ob das Repo Agents-Releases im gemeinsamen `CHANGELOG.md` führt — Task 8 Step 3.
