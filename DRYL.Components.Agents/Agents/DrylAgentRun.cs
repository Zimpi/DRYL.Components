using System.Threading.Channels;

namespace DRYL.Components.Agents;

/// <summary>
/// Observable handle to a running agent. Drives <see cref="State"/> automatically and
/// exposes the accumulated <see cref="Text"/>, the live <see cref="ToolCalls"/> trace,
/// and a <see cref="TextStream"/> ready to drop into <c>DrylAiStream</c>/<c>DrylMarkdown</c>.
/// </summary>
public sealed class DrylAgentRun : IAsyncDisposable
{
    private readonly List<DrylToolInvocation> _toolCalls = new();
    private readonly Channel<string> _textChannel =
        Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
    private readonly TaskCompletionSource _completed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Current AI state, driven automatically by the run.</summary>
    public AiState State { get; internal set; } = AiState.Thinking;

    /// <summary>The accumulated answer text so far.</summary>
    public string Text { get; internal set; } = string.Empty;

    /// <summary>The tool calls observed in this run, in arrival order.</summary>
    public IReadOnlyList<DrylToolInvocation> ToolCalls => _toolCalls;

    /// <summary>Raised whenever <see cref="State"/>, <see cref="Text"/>, or <see cref="ToolCalls"/> changes.</summary>
    public event Action? OnChange;

    /// <summary>The text deltas as an async stream — feed directly to <c>DrylAiStream Source="..."</c>.</summary>
    public IAsyncEnumerable<string> TextStream => _textChannel.Reader.ReadAllAsync();

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
