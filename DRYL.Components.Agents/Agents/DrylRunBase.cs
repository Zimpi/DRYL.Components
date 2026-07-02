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

    /// <summary>Raised whenever <see cref="State"/>, <see cref="Text"/>, <see cref="ToolCalls"/> or subclass state changes.</summary>
    public event Action? OnChange;

    /// <summary>The text deltas as an async stream — feed directly to <c>DrylAiStream Source="..."</c>.</summary>
    public IAsyncEnumerable<string> TextStream => _textStream;

    /// <summary>Cancelled when the run is disposed; lets in-flight work (e.g. an artifact reveal) stop cleanly.</summary>
    internal CancellationToken DisposalToken => _cts.Token;

    internal void AddToolCall(DrylToolInvocation t) { _toolCalls.Add(t); Raise(); }
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
