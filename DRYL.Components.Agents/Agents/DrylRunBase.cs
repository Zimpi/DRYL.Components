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

    /// <summary>The tool calls observed in this run, in arrival order.</summary>
    public IReadOnlyList<DrylToolInvocation> ToolCalls => _toolCalls;

    /// <summary>Raised whenever <see cref="State"/>, <see cref="Text"/>, <see cref="ToolCalls"/> or subclass state changes.</summary>
    public event Action? OnChange;

    /// <summary>The text deltas as an async stream — feed directly to <c>DrylAiStream Source="..."</c>.</summary>
    public IAsyncEnumerable<string> TextStream => _textStream;

    internal void AddToolCall(DrylToolInvocation t) { _toolCalls.Add(t); Raise(); }
    internal void Raise() => OnChange?.Invoke();
    internal void PushText(string delta) => _textChannel.Writer.TryWrite(delta);
    internal void CompleteText() => _textChannel.Writer.TryComplete();
    internal void MarkCompleted() => _completed.TrySetResult();

    /// <summary>Test/consumer helper: completes when the run's processing loop finishes.</summary>
    public Task WaitForCompletionAsync() => _completed.Task;

    /// <summary>Cancels the run and releases its resources.</summary>
    public ValueTask DisposeAsync()
    {
        _textChannel.Writer.TryComplete();
        _completed.TrySetResult();
        return ValueTask.CompletedTask;
    }
}
