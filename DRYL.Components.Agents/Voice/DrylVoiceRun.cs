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

    /// <summary>The model is working — reasoning, or running a tool.</summary>
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
/// <para>The audio never touches .NET. The browser holds a WebRTC peer connection straight to the
/// API; this object receives state changes and tool calls over a data channel and answers them.
/// That is deliberate — routing audio through a Blazor Server circuit costs a few hundred
/// milliseconds in each direction, which is the difference between a conversation and a
/// walkie-talkie.</para>
/// <para>Input and output levels are deliberately absent from this class. A level that updates
/// 30 times a second and raises <see cref="DrylRunBase.OnChange"/> is 30 renders a second for a
/// decoration; the level stays in the browser and drives a CSS variable on the orb.</para>
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
    private int _autoTurns;      // consecutive turns continued without the user saying anything

    internal DrylVoiceRun(DrylVoiceRunner runner, DrylVoiceOptions options)
    {
        _runner = runner;
        Options = options;

        // A voice run is a possibility, not an errand: it is created when the page loads and may
        // sit there forever. DrylRunBase starts at Thinking, which would make the dock claim it
        // is working from the moment the page opens — see the same note on DrylCanvasRun.
        State = AiState.None;
    }

    /// <summary>The session's configuration. The browser cannot change it: it is baked into the
    /// minted token.</summary>
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
    /// Turns replayed into every new session — the conversation so far, from either channel. Set
    /// it before the session starts; <see cref="StartAsync"/> falls back to it when it is called
    /// without an explicit history, which is what the dock's microphone button does.
    /// </summary>
    public IEnumerable<DrylVoiceMessage>? SeedHistory { get; set; }

    /// <summary>
    /// Asked after the model finished a turn <em>without</em> calling a tool: return true and the
    /// session prompts it to carry on by itself, return false and it goes back to listening.
    /// Null — the default — means a turn always ends the model's part, which is right for a plain
    /// conversation and wrong for anything with a plan.
    /// </summary>
    /// <remarks>
    /// <para>A realtime session has no agent loop of its own. The only thing the protocol
    /// continues by itself is a turn that carried a function call: its results go back and the
    /// next response follows. A turn that was <em>only</em> speech ends there, and nothing ever
    /// starts another one — so an assistant that says "ich lege mal eine Liste an" and stops
    /// talking is not thinking, it is finished, and it waits for the user to poke it.</para>
    /// <para>Wire this to whatever the host uses to know there is work left — an open task list,
    /// a queue, a half-written document:</para>
    /// <code>
    /// voice.ShouldContinue = () =>
    ///     ValueTask.FromResult(tasks.Items.Any(t => t.Status != AssistantTaskStatus.Done));
    /// </code>
    /// <para>The predicate runs on the circuit and may be async, but it sits between two spoken
    /// turns — keep it to reading state that is already in memory. A database round trip here is
    /// silence the user hears.</para>
    /// </remarks>
    public Func<ValueTask<bool>>? ShouldContinue { get; set; }

    /// <summary>
    /// How many turns in a row may be continued by <see cref="ShouldContinue"/> before the session
    /// hands the floor back, however the predicate answers.
    /// </summary>
    /// <remarks>
    /// The backstop for a predicate that never goes false — a task the model cannot finish would
    /// otherwise loop forever, and an assistant talking to itself is both a bill and a thing the
    /// user cannot interrupt politely. Only <em>fruitless</em> turns count: a turn that ran a tool
    /// is progress and resets the counter, as does the user speaking. So this caps how often the
    /// model may be nudged while achieving nothing, not how long it may work.
    /// </remarks>
    public int MaxAutoContinuations { get; set; } = 6;

    /// <summary>
    /// Opens a session: the browser asks for the microphone, the server mints the token, and the
    /// two sides negotiate a peer connection.
    /// </summary>
    /// <param name="history">Earlier turns — typically the text conversation so far — replayed
    /// into the session so the voice knows what has already been discussed. Falls back to
    /// <see cref="SeedHistory"/>.</param>
    /// <param name="ct">Cancels the token request.</param>
    public async Task StartAsync(
        IEnumerable<DrylVoiceMessage>? history = null,
        CancellationToken ct = default)
    {
        if (IsActive || _starting) return;

        _starting = true;
        history ??= SeedHistory;
        MarkConnecting();

        try
        {
            var token = await _runner.MintTokenAsync(Options, ct).ConfigureAwait(false);

            _module ??= await _runner.Js
                .InvokeAsync<IJSObjectReference>("import", ct, ModulePath)
                .ConfigureAwait(false);
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
            OnClosed();                          // circuit gone; there is nobody left to tell
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

    /// <summary>Enters the connecting phase. The public way in is <see cref="StartAsync"/>.</summary>
    internal void MarkConnecting()
    {
        Phase = VoicePhase.Connecting;
        Activity = VoiceActivity.Listening;
        Error = null;                            // a new attempt does not carry the old failure
        _transcript.Clear();                     // a session is a conversation; a new one starts empty
        ClearToolCalls();                        // …and so is its trace
        _autoTurns = 0;                          // …and it does not inherit the last one's budget
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
    /// <param name="activity">The activity name, case-insensitive.</param>
    [JSInvokable]
    public void OnActivity(string activity)
    {
        if (Phase is not VoicePhase.Live) return;

        if (Enum.TryParse<VoiceActivity>(activity, ignoreCase: true, out var parsed))
            Activity = parsed;

        // The user talking is the thing the continuation budget exists to protect: they now have
        // the floor, and whatever happens after this is a fresh errand, not the old one spinning.
        if (Activity is VoiceActivity.UserSpeaking) _autoTurns = 0;

        Sync();
    }

    /// <summary>A finished transcript line.</summary>
    /// <param name="role">A <see cref="VoiceRole"/> name, case-insensitive.</param>
    /// <param name="text">What was said.</param>
    [JSInvokable]
    public void OnTranscript(string role, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;   // a cough transcribes to ""

        var parsed = Enum.TryParse<VoiceRole>(role, ignoreCase: true, out var r)
            ? r
            : VoiceRole.Assistant;

        _transcript.Add(new DrylVoiceMessage(parsed, text.Trim()));
        Raise();
    }

    /// <summary>
    /// Runs a tool the model asked for and returns its result as JSON for the data channel.
    /// </summary>
    /// <param name="callId">The API's call id, echoed back with the result.</param>
    /// <param name="name">The tool the model named.</param>
    /// <param name="argumentsJson">Its arguments, as a JSON object.</param>
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

        // Running a tool is the model doing the work rather than talking about it, so the budget
        // for fruitless nudges starts over. Without this a long plan would run out of turns
        // halfway through precisely because it was going well.
        _autoTurns = 0;

        // The browser only ever supplies a name. What runs is whatever the host put in the tool
        // list — a manipulated page cannot invent a tool.
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

        static string Fail(string message) => new JsonObject { ["error"] = message }.ToJsonString();
    }

    /// <summary>
    /// Asked by the browser when a turn ended with no tool call, to decide whether the model
    /// should be prompted to carry on. True means the session sends it back to work.
    /// </summary>
    /// <remarks>Public because it is <c>[JSInvokable]</c>; the answer comes entirely from
    /// <see cref="ShouldContinue"/> and <see cref="MaxAutoContinuations"/>.</remarks>
    [JSInvokable]
    public async Task<bool> OnTurnEndedAsync()
    {
        if (Phase is not VoicePhase.Live || ShouldContinue is null) return false;

        if (_autoTurns >= MaxAutoContinuations)
        {
            // Spent. Handing the floor back is the only honest move: the user can see the plan
            // is unfinished and say so, which is far better than a model nudging itself forever.
            _autoTurns = 0;
            return false;
        }

        bool more;
        try
        {
            more = await ShouldContinue().ConfigureAwait(false);
        }
        catch
        {
            // A host predicate that throws must not wedge the conversation — the session is fine,
            // and going back to listening still leaves the user able to talk.
            return false;
        }

        if (!more)
        {
            _autoTurns = 0;
            return false;
        }

        _autoTurns++;
        Activity = VoiceActivity.Thinking;
        Sync();
        return true;
    }

    /// <summary>Something went wrong; the session is over.</summary>
    /// <param name="message">What to show the user.</param>
    [JSInvokable]
    public void OnFailed(string message)
    {
        Error = new DrylRunError(message);
        Phase = VoicePhase.Idle;
        Activity = VoiceActivity.Listening;
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

    // The one place phase and activity become the shared AI vocabulary. No new states: a voice
    // session breathes in exactly the same language as every other AI surface in the library.
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
