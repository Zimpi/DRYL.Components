using Microsoft.Agents.AI;

namespace DRYL.Components.Agents;

public sealed partial class DrylAgentRunner
{
    /// <summary>
    /// Start a sequential multi-agent flow (a handoff chain): each step's agent receives the
    /// previous agent's answer as its input, starting from <paramref name="message"/>. Returns
    /// one observable <see cref="DrylMultiAgentRun"/> whose <see cref="DrylRunBase.State"/>
    /// mirrors the active step and whose <see cref="DrylRunBase.TextStream"/> carries the final
    /// agent's answer — render the flow with <c>DrylHandoffTrace</c>.
    /// </summary>
    public DrylMultiAgentRun StartSequential(
        IReadOnlyList<DrylAgentStep> steps, string message, string? aiKey = null, CancellationToken ct = default)
        => StartFlow(DrylAgentFlow.Sequential, steps, message, aiKey, ct);

    /// <summary>
    /// Start a concurrent multi-agent flow: every step's agent runs in parallel on the same
    /// <paramref name="message"/>. The flow's own <see cref="DrylRunBase.Text"/> stays empty —
    /// each lane's answer lives on its <see cref="DrylAgentHandoff.Run"/>. Render the flow with
    /// <c>DrylHandoffTrace</c>.
    /// </summary>
    public DrylMultiAgentRun StartConcurrent(
        IReadOnlyList<DrylAgentStep> steps, string message, string? aiKey = null, CancellationToken ct = default)
        => StartFlow(DrylAgentFlow.Concurrent, steps, message, aiKey, ct);

    private DrylMultiAgentRun StartFlow(
        DrylAgentFlow flow, IReadOnlyList<DrylAgentStep> steps, string message, string? aiKey, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(steps);
        if (steps.Count == 0)
            throw new ArgumentException("A multi-agent run needs at least one step.", nameof(steps));

        var run = new DrylMultiAgentRun(flow, steps.Select(s => s.Name));

        // Stop in-flight children when the caller cancels OR the flow run is disposed.
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, run.DisposalToken);
        var processing = flow == DrylAgentFlow.Sequential
            ? ProcessSequentialAsync(run, steps, message, aiKey, linkedCts.Token)
            : ProcessConcurrentAsync(run, steps, message, aiKey, linkedCts.Token);
        _ = processing.ContinueWith(_ => linkedCts.Dispose(), TaskScheduler.Default);

        return run;
    }

    private async Task ProcessSequentialAsync(
        DrylMultiAgentRun run, IReadOnlyList<DrylAgentStep> steps, string message, string? aiKey, CancellationToken ct)
    {
        SetState(run, AiState.Thinking, aiKey);
        try
        {
            var input = message;
            for (var i = 0; i < steps.Count; i++)
            {
                var handoff = run.Steps[i];
                var isLast = i == steps.Count - 1;
                var child = new DrylAgentRun();
                handoff.Run = child;
                run.ActiveIndex = i;

                // Mirror the active child onto the flow: the final step also forwards its text
                // deltas, so the flow's TextStream carries the chain's final answer.
                var forwarded = 0;
                void Mirror()
                {
                    if (isLast && child.Text.Length > forwarded)
                    {
                        var delta = child.Text[forwarded..];
                        forwarded = child.Text.Length;
                        run.Text += delta;
                        run.PushText(delta);
                    }
                    var mapped = child.State switch
                    {
                        AiState.Generated when !isLast => AiState.Thinking,   // handing off — still working
                        AiState.None => AiState.Thinking,                     // child settling; resolved below
                        var state => state,
                    };
                    SetState(run, mapped, aiKey);
                }

                child.OnChange += Mirror;
                try
                {
                    await ProcessAsync(child, GetStepUpdates(steps[i], input, ct), aiKey: null, ct);
                }
                finally
                {
                    child.OnChange -= Mirror;
                }

                if (child.Usage is { } usage) run.AddUsage(usage);

                if (child.Error is { } error)
                {
                    run.Error = new DrylRunError(error.Message, error.Exception, handoff.Name);
                    SetStateRaw(run, AiState.None, aiKey);
                    return;
                }
                if (ct.IsCancellationRequested)
                {
                    SetStateRaw(run, AiState.None, aiKey);
                    return;
                }

                input = child.Text;
            }

            SetState(run, AiState.Generated, aiKey);
        }
        catch (OperationCanceledException)
        {
            SetStateRaw(run, AiState.None, aiKey);
        }
        catch (Exception ex)
        {
            run.Error = new DrylRunError(ex.Message, ex);
            SetStateRaw(run, AiState.None, aiKey);
        }
        finally
        {
            run.ActiveIndex = null;
            run.CompleteText();
            run.MarkCompleted();
            run.Raise();
        }
    }

    private async Task ProcessConcurrentAsync(
        DrylMultiAgentRun run, IReadOnlyList<DrylAgentStep> steps, string message, string? aiKey, CancellationToken ct)
    {
        SetState(run, AiState.Thinking, aiKey);
        var unhook = new List<Action>();
        try
        {
            var tasks = new List<Task>(steps.Count);
            for (var i = 0; i < steps.Count; i++)
            {
                var handoff = run.Steps[i];
                var child = new DrylAgentRun();
                handoff.Run = child;

                // While lanes are in flight the flow streams as soon as any lane streams.
                void Mirror()
                {
                    var streaming = run.Steps.Any(s => s.Run?.State == AiState.Streaming);
                    SetState(run, streaming ? AiState.Streaming : AiState.Thinking, aiKey);
                }

                child.OnChange += Mirror;
                unhook.Add(() => child.OnChange -= Mirror);
                tasks.Add(ProcessAsync(child, GetStepUpdates(steps[i], message, ct), aiKey: null, ct));
            }
            run.Raise();

            await Task.WhenAll(tasks);
            foreach (var detach in unhook) detach();
            unhook.Clear();

            foreach (var step in run.Steps)
            {
                if (step.Run?.Usage is { } usage) run.AddUsage(usage);
            }

            var failed = run.Steps.FirstOrDefault(s => s.Error is not null);
            if (failed is not null)
            {
                run.Error = new DrylRunError(failed.Error!.Message, failed.Error.Exception, failed.Name);
                SetStateRaw(run, AiState.None, aiKey);
            }
            else if (ct.IsCancellationRequested)
            {
                SetStateRaw(run, AiState.None, aiKey);
            }
            else
            {
                SetState(run, AiState.Generated, aiKey);
            }
        }
        catch (OperationCanceledException)
        {
            SetStateRaw(run, AiState.None, aiKey);
        }
        catch (Exception ex)
        {
            run.Error = new DrylRunError(ex.Message, ex);
            SetStateRaw(run, AiState.None, aiKey);
        }
        finally
        {
            foreach (var detach in unhook) detach();
            run.CompleteText();
            run.MarkCompleted();
        }
    }

    private static IAsyncEnumerable<AgentResponseUpdate> GetStepUpdates(
        DrylAgentStep step, string message, CancellationToken ct)
    {
        if (step.UpdatesFactory is not null) return step.UpdatesFactory(message, ct);
        if (step.Agent is null)
            throw new InvalidOperationException($"Multi-agent step '{step.Name}' has no Agent.");
        return step.Agent.RunStreamingAsync(message, step.Session, options: null, cancellationToken: ct);
    }
}
