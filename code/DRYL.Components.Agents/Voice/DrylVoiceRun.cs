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

    /// <summary>Token minting, microphone acquisition and peer connection negotiation.</summary>
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
    private readonly object _sync = new();
    private Attempt? _attempt;
    private long _generation;
    private long _turnGeneration;
    private bool _disposed;
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

    /// <summary>True while a session is connecting, running or closing.</summary>
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
    /// Opens a session: the runner mints the token, the browser asks for the microphone, and the
    /// two sides negotiate a peer connection.
    /// </summary>
    /// <param name="history">Earlier turns — typically the text conversation so far — replayed
    /// into the session so the voice knows what has already been discussed. Falls back to
    /// <see cref="SeedHistory"/>.</param>
    /// <param name="ct">Cancels startup while this operation is pending.</param>
    public async Task StartAsync(
        IEnumerable<DrylVoiceMessage>? history = null,
        CancellationToken ct = default)
    {
        Attempt attempt;
        lock (_sync)
        {
            if (_disposed || IsActive || _attempt is not null) return;
            attempt = new Attempt(ct);
            _attempt = attempt;
            history ??= SeedHistory;
            MarkConnecting();
        }

        // Cancellation invalidates the owner immediately. Resource-producing JS calls are
        // still observed: cancelling their await could lose a reference returned afterwards.
        using var registration = ct.Register(() => _ = EndAttemptAsync(attempt, null));

        try
        {
            if (!Current(attempt)) return;
            var token = await _runner.MintTokenAsync(Options, attempt.Token).ConfigureAwait(false);
            if (!Current(attempt)) return;

            var module = await _runner.Js
                .InvokeAsync<IJSObjectReference>("import", ModulePath)
                .ConfigureAwait(false);
            lock (_sync)
            {
                if (Current(attempt))
                {
                    attempt.Module = module;
                    attempt.Callback = DotNetObjectReference.Create(new AttemptCallbacks(this, attempt));
                }
            }
            if (!ReferenceEquals(attempt.Module, module))
            {
                await ReleaseAsync(module, stop: false).ConfigureAwait(false);
                return;
            }

            if (!Current(attempt)) return;
            var handle = await module.InvokeAsync<IJSObjectReference>("createSession", token, new
            {
                baseUrl = Options.BaseUrl.TrimEnd('/'),
                idleMs = (int)Options.IdleTimeout.TotalMilliseconds,
                maxMs = (int)Options.MaxDuration.TotalMilliseconds,
                history = (history ?? Array.Empty<DrylVoiceMessage>())
                    .Select(m => new { role = m.Role.ToString(), text = m.Text })
                    .ToArray(),
            }, attempt.Callback).ConfigureAwait(false);
            bool adopted;
            lock (_sync)
            {
                adopted = Current(attempt);
                if (adopted) attempt.Handle = handle;
            }
            if (!adopted)
            {
                await ReleaseAsync(handle, stop: true).ConfigureAwait(false);
                return;
            }
            // A stop racing dispatch closes this very handle. Its late start is then inert;
            // it cannot claim the page or stop the handle belonging to a newer run.
            if (Current(attempt)) await handle.InvokeVoidAsync("start").ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await EndAttemptAsync(attempt, null).ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            await EndAttemptAsync(attempt, null).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await EndAttemptAsync(attempt, ex.Message).ConfigureAwait(false);
        }
    }

    /// <summary>Ends the session and releases the microphone.</summary>
    public async Task StopAsync()
    {
        Attempt? attempt;
        long generation;
        lock (_sync)
        {
            if (_disposed || Phase is VoicePhase.Idle) return;
            attempt = Invalidate();
            generation = _generation;
            Phase = VoicePhase.Closing;
            State = AiState.None;
            Raise();
        }
        if (attempt is not null) await CleanupAsync(attempt).ConfigureAwait(false);
        lock (_sync)
        {
            if (!_disposed && _generation == generation) SetClosed();
        }
    }

    // ── Reported by the browser ──────────────────────────────────────────────

    /// <summary>Enters the connecting phase. The public way in is <see cref="StartAsync"/>.</summary>
    internal void MarkConnecting()
    {
        _generation++;
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
        Connected(null);
    }

    private void Connected(Attempt? expected)
    {
        lock (_sync)
        {
            if (!Accept(expected)) return;
            Phase = VoicePhase.Live;
            Activity = VoiceActivity.Listening;
            Sync();
        }
    }

    /// <summary>Who is doing what right now. Values are <see cref="VoiceActivity"/> names.</summary>
    /// <param name="activity">The activity name, case-insensitive.</param>
    [JSInvokable]
    public void OnActivity(string activity)
    {
        ActivityChanged(null, activity);
    }

    private void ActivityChanged(Attempt? expected, string activity)
    {
        lock (_sync)
        {
            if (!Accept(expected)) return;
            if (Phase is not VoicePhase.Live) return;

            if (Enum.TryParse<VoiceActivity>(activity, ignoreCase: true, out var parsed))
                Activity = parsed;

            // The user has the floor; future work starts with a fresh continuation budget.
            if (Activity is VoiceActivity.UserSpeaking)
            {
                _autoTurns = 0;
                _turnGeneration++;
            }

            Sync();
        }
    }

    /// <summary>A finished transcript line.</summary>
    /// <param name="role">A <see cref="VoiceRole"/> name, case-insensitive.</param>
    /// <param name="text">What was said.</param>
    [JSInvokable]
    public void OnTranscript(string role, string text)
    {
        TranscriptReceived(null, role, text);
    }

    private void TranscriptReceived(Attempt? expected, string role, string text)
    {
        lock (_sync)
        {
            if (!Accept(expected)) return;
            if (string.IsNullOrWhiteSpace(text)) return;   // a cough transcribes to ""

            var parsed = Enum.TryParse<VoiceRole>(role, ignoreCase: true, out var r)
                ? r
                : VoiceRole.Assistant;

            _transcript.Add(new DrylVoiceMessage(parsed, text.Trim()));
            Raise();
        }
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
    public Task<string> OnToolCallAsync(string callId, string name, string argumentsJson) =>
        ToolCallAsync(null, callId, name, argumentsJson);

    private async Task<string> ToolCallAsync(Attempt? expected, string callId, string name, string argumentsJson)
    {
        long generation;
        AIFunction? tool;
        var invocation = new DrylToolInvocation
        {
            CallId = callId,
            ToolName = name,
            Arguments = argumentsJson,
        };
        lock (_sync)
        {
            if (!Accept(expected) || Phase is not VoicePhase.Live) return Fail("Voice session ended.");
            generation = _generation;
            // Only fruitless nudges are limited: a configured tool represents progress.
            _autoTurns = 0;
            tool = Options.FindTool(name);
            if (tool is null) invocation.Error = $"Unknown tool \"{name}\".";
            AddToolCall(invocation);
            // OnChange may synchronously stop or restart the run. Complete all mutations
            // before notifying, then recheck before dispatching host work.
            if (!Accept(expected) || _generation != generation) return Fail("Voice session ended.");
            if (tool is null) return Fail(invocation.Error!);
        }

        try
        {
            var result = await tool.InvokeAsync(ParseArguments(argumentsJson)).ConfigureAwait(false);
            var json = result as string ?? JsonSerializer.Serialize(result);
            lock (_sync)
            {
                if (!Accept(expected) || _generation != generation) return Fail("Voice session ended.");
                invocation.Result = json;
                Raise();
            }
            return json;
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                if (!Accept(expected) || _generation != generation) return Fail("Voice session ended.");
                invocation.Error = ex.Message;
                Raise();
            }
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
    public Task<bool> OnTurnEndedAsync() => TurnEndedAsync(null);

    private async Task<bool> TurnEndedAsync(Attempt? expected)
    {
        long generation;
        long turnGeneration;
        Func<ValueTask<bool>> predicate;
        lock (_sync)
        {
            if (!Accept(expected)) return false;
            if (Phase is not VoicePhase.Live || ShouldContinue is null) return false;
            generation = _generation;
            turnGeneration = _turnGeneration;
            predicate = ShouldContinue;

            if (_autoTurns >= MaxAutoContinuations)
            {
                // The user can decide whether unfinished work deserves another attempt.
                _autoTurns = 0;
                return false;
            }
        }

        bool more;
        try
        {
            more = await predicate().ConfigureAwait(false);
        }
        catch
        {
            // A host predicate that throws must not wedge the conversation — the session is fine,
            // and going back to listening still leaves the user able to talk.
            return false;
        }

        lock (_sync)
        {
            if (!Accept(expected) || _generation != generation || _turnGeneration != turnGeneration || Phase is not VoicePhase.Live) return false;
            if (!more)
            {
                _autoTurns = 0;
                return false;
            }
            if (_autoTurns >= MaxAutoContinuations) return false;
            _autoTurns++;
            Activity = VoiceActivity.Thinking;
            Sync();
            return Accept(expected) && _generation == generation && _turnGeneration == turnGeneration;
        }
    }

    /// <summary>Something went wrong; the session is over.</summary>
    /// <param name="message">What to show the user.</param>
    [JSInvokable]
    public void OnFailed(string message)
    {
        _ = EndAttemptAsync(null, message);
    }

    /// <summary>The session ended — by the user, by a timeout, or by the network.</summary>
    [JSInvokable]
    public void OnClosed()
    {
        _ = EndAttemptAsync(null, null);
    }

    private void SetClosed()
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
        Attempt? attempt;
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            attempt = Invalidate();
            SetClosed();
        }
        if (attempt is not null) await CleanupAsync(attempt).ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private bool Current(Attempt attempt)
    {
        lock (_sync) return !_disposed && ReferenceEquals(_attempt, attempt);
    }

    private bool Accept(Attempt? expected) => !_disposed && (expected is null || ReferenceEquals(_attempt, expected));

    // Called under _sync. Invalidation precedes every await and every cancellation callback.
    private Attempt? Invalidate()
    {
        var previous = _attempt;
        _attempt = null;
        _generation++;
        return previous;
    }

    private async Task EndAttemptAsync(Attempt? expected, string? error)
    {
        Attempt? attempt;
        lock (_sync)
        {
            if (!Accept(expected)) return;
            attempt = Invalidate();
            if (error is not null) Error = new DrylRunError(error);
            SetClosed();
        }
        if (attempt is not null) await CleanupAsync(attempt).ConfigureAwait(false);
    }

    private async Task CleanupAsync(Attempt attempt)
    {
        IJSObjectReference? handle, module;
        DotNetObjectReference<AttemptCallbacks>? callback;
        lock (_sync)
        {
            if (attempt.Released) return;
            attempt.Released = true;
            handle = attempt.Handle;
            module = attempt.Module;
            callback = attempt.Callback;
        }
        try { attempt.Cancellation.Cancel(); }
        catch (AggregateException) { /* a host cancellation callback failed */ }
        if (handle is not null) await ReleaseAsync(handle, stop: true).ConfigureAwait(false);
        if (module is not null) await ReleaseAsync(module, stop: false).ConfigureAwait(false);
        callback?.Dispose();
        attempt.Cancellation.Dispose();
    }

    private static async Task ReleaseAsync(IJSObjectReference reference, bool stop)
    {
        if (stop)
        {
            try { await reference.InvokeVoidAsync("stop").ConfigureAwait(false); }
            catch (Exception ex) when (ex is JSException or InvalidOperationException or OperationCanceledException) { }
        }
        try { await reference.DisposeAsync().ConfigureAwait(false); }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or OperationCanceledException) { }
    }

    private sealed class Attempt
    {
        internal Attempt(CancellationToken ct)
        {
            Cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Token = Cancellation.Token;
        }
        internal CancellationTokenSource Cancellation { get; }
        internal CancellationToken Token { get; }
        internal IJSObjectReference? Module, Handle;
        internal DotNetObjectReference<AttemptCallbacks>? Callback;
        internal bool Released;
    }

    // The callback signature stays familiar to JS, but this target can never become a newer
    // session. Even an invocation already dispatched before Dispose cannot cross that boundary.
    private sealed class AttemptCallbacks(DrylVoiceRun run, Attempt attempt)
    {
        [JSInvokable] public void OnConnected() => run.Connected(attempt);
        [JSInvokable] public void OnActivity(string activity) => run.ActivityChanged(attempt, activity);
        [JSInvokable] public void OnTranscript(string role, string text) => run.TranscriptReceived(attempt, role, text);
        [JSInvokable] public Task<string> OnToolCallAsync(string callId, string name, string argumentsJson) => run.ToolCallAsync(attempt, callId, name, argumentsJson);
        [JSInvokable] public Task<bool> OnTurnEndedAsync() => run.TurnEndedAsync(attempt);
        [JSInvokable] public Task OnFailed(string message) => run.EndAttemptAsync(attempt, message);
        [JSInvokable] public Task OnClosed() => run.EndAttemptAsync(attempt, null);
    }
}
