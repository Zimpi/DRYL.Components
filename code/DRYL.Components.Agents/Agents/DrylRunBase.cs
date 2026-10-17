using System.Threading.Channels;

namespace DRYL.Components.Agents;

/// <summary>
/// Shared observable plumbing for agent runs: accumulated <see cref="Text"/>, a live
/// <see cref="ToolCalls"/> trace, automatic <see cref="State"/>, a stable <see cref="TextStream"/>,
/// and an <see cref="OnChange"/> notification. Base for <see cref="DrylAgentRun"/> and
/// <see cref="DrylArtifactRun{T}"/>.
/// </summary>
public abstract class DrylRunBase : IAsyncDisposable
{
    private readonly List<DrylToolInvocation> _toolCalls = new();
    private readonly Channel<string> _textChannel =
        Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
    private readonly TaskCompletionSource _completed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _cts = new();
    private TaskCompletionSource? _decided;
    // Decisions come from the UI while the processing loop adds calls and waits; outside a
    // Blazor synchronization context those are different threads.
    private readonly object _gate = new();
    private bool _disposed;

    // Cached once so TextStream is a STABLE reference across re-renders: consumers like
    // DrylAiStream restart their enumeration whenever Source changes by reference, and the run
    // re-renders on every tool-call/state change. A fresh ReadAllAsync() per access would reset
    // the streamed text mid-run.
    private readonly IAsyncEnumerable<string> _textStream;

    /// <summary>Creates the run plumbing.</summary>
    protected DrylRunBase() => _textStream = _textChannel.Reader.ReadAllAsync();

    /// <summary>Current AI state, driven automatically by the run.</summary>
    public AiState State { get; internal set; } = AiState.Thinking;

    /// <summary>The accumulated answer text so far.</summary>
    public string Text { get; internal set; } = string.Empty;

    /// <summary>
    /// The terminal error of the run, or null while running / after success. When set, the run
    /// has settled at <see cref="AiState.None"/> — render via <c>DrylAgentError</c>.
    /// </summary>
    public DrylRunError? Error { get; internal set; }

    /// <summary>
    /// Accumulated token usage, or null if the provider never reported any. Grows as
    /// <c>UsageContent</c> updates arrive — render via <c>DrylAgentUsage</c>.
    /// </summary>
    public DrylRunUsage? Usage { get; private set; }

    /// <summary>The tool calls observed in this run, in arrival order.</summary>
    public IReadOnlyList<DrylToolInvocation> ToolCalls => _toolCalls;

    /// <summary>The calls awaiting a human decision (<see cref="DrylToolApproval.Pending"/>), in arrival order.</summary>
    public IReadOnlyList<DrylToolInvocation> PendingApprovals
    {
        get { lock (_gate) return _toolCalls.Where(t => t.Approval == DrylToolApproval.Pending).ToList(); }
    }

    /// <summary>
    /// Approves one pending call. Once the last pending call has a decision, the run answers the
    /// agent and streams on. Returns <c>false</c> when no pending call has that id.
    /// </summary>
    public bool Approve(string callId) => Decide(callId, DrylToolApproval.Approved);

    /// <summary>
    /// Denies one pending call; the model receives a failed function result for it. Returns
    /// <c>false</c> when no pending call has that id.
    /// </summary>
    public bool Deny(string callId) => Decide(callId, DrylToolApproval.Declined);

    /// <summary>Raised whenever <see cref="State"/>, <see cref="Text"/>, <see cref="ToolCalls"/> or subclass state changes.</summary>
    public event Action? OnChange;

    /// <summary>The text deltas as an async stream — feed directly to <c>DrylAiStream Source="..."</c>.</summary>
    public IAsyncEnumerable<string> TextStream => _textStream;

    /// <summary>Cancelled when the run is disposed; lets in-flight work (e.g. an artifact reveal) stop cleanly.</summary>
    internal CancellationToken DisposalToken => _cts.Token;

    internal void AddToolCall(DrylToolInvocation t) { lock (_gate) _toolCalls.Add(t); Raise(); }

    private bool Decide(string callId, DrylToolApproval decision)
    {
        TaskCompletionSource? release = null;
        lock (_gate)
        {
            var call = _toolCalls.FirstOrDefault(t => t.CallId == callId && t.Approval == DrylToolApproval.Pending);
            if (call is null) return false;
            call.Approval = decision;
            if (!_toolCalls.Any(t => t.Approval == DrylToolApproval.Pending)) release = _decided;
        }
        Raise();
        release?.TrySetResult();
        return true;
    }

    /// <summary>
    /// Completes once no call is pending. Cancelling <paramref name="ct"/> or disposing the run
    /// ends the wait with <see cref="OperationCanceledException"/>, after declining what was pending.
    /// </summary>
    internal async Task WaitForDecisionsAsync(CancellationToken ct)
    {
        TaskCompletionSource decided;
        lock (_gate)
        {
            if (!_toolCalls.Any(t => t.Approval == DrylToolApproval.Pending)) return;
            decided = _decided = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        try
        {
            if (_disposed) throw new OperationCanceledException();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            await decided.Task.WaitAsync(linked.Token);
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
            // Nobody decided: the call did not run, so it reads as declined rather than as still waiting.
            lock (_gate)
            {
                foreach (var t in _toolCalls.Where(t => t.Approval == DrylToolApproval.Pending))
                    t.Approval = DrylToolApproval.Declined;
            }
            throw new OperationCanceledException();
        }
        finally
        {
            lock (_gate) _decided = null;
        }
    }

    /// <summary>Drops the trace. For runs that are reused across sessions (a voice run is one),
    /// so the next session does not inherit the previous one's tool calls.</summary>
    /// <remarks>Silent on purpose: clearing is part of a larger state change, and the caller
    /// raises once for the whole of it. Raising here too would cost every host a second render
    /// for the same event.</remarks>
    internal void ClearToolCalls() => _toolCalls.Clear();
    internal void AddUsage(Microsoft.Extensions.AI.UsageDetails details) { (Usage ??= new DrylRunUsage()).Add(details); Raise(); }
    internal void AddUsage(DrylRunUsage usage) { (Usage ??= new DrylRunUsage()).Add(usage); Raise(); }
    internal void Raise() => OnChange?.Invoke();
    internal void PushText(string delta) => _textChannel.Writer.TryWrite(delta);
    internal void CompleteText() => _textChannel.Writer.TryComplete();
    internal void MarkCompleted() => _completed.TrySetResult();

    /// <summary>Test/consumer helper: completes when the run's processing loop finishes.</summary>
    public Task WaitForCompletionAsync() => _completed.Task;

    /// <summary>Cancels the run and releases its resources.</summary>
    public virtual ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _textChannel.Writer.TryComplete();
            _completed.TrySetResult();
            _cts.Cancel();
            _cts.Dispose();
        }
        return ValueTask.CompletedTask;
    }
}
