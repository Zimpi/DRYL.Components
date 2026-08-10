namespace DRYL.Components.Agents;

/// <summary>
/// The terminal error of an agent run. Set on <see cref="DrylRunBase.Error"/> when the run's
/// processing loop faults; render it with <c>DrylAgentError</c>. A run that errored settles at
/// <see cref="AiState.None"/> with <see cref="DrylRunBase.Error"/> non-null — the same
/// convention <see cref="DrylToolInvocation"/> uses for a failed tool call.
/// </summary>
public sealed class DrylRunError
{
    internal DrylRunError(string message, Exception? exception = null, string? source = null)
    {
        Message = message;
        Exception = exception;
        ExceptionType = exception?.GetType().Name;
        Source = source;
    }

    /// <summary>The human-readable error message.</summary>
    public string Message { get; }

    /// <summary>The short type name of the underlying exception (e.g. <c>HttpRequestException</c>), if any.</summary>
    public string? ExceptionType { get; }

    /// <summary>The underlying exception, for programmatic consumers (logging, retry policies).</summary>
    public Exception? Exception { get; }

    /// <summary>In a multi-agent run: the name of the step that failed. Null for single-agent runs.</summary>
    public string? Source { get; }
}
