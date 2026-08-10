using System.ComponentModel;
using DRYL.Components.Dialogs;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents.Tools;

/// <summary>
/// Ready-made human-in-the-loop tool functions for the Microsoft Agent Framework, backed by
/// DRYL dialogs. Create one per circuit with <see cref="Create"/> (it captures the scoped
/// <see cref="IDrylDialogService"/>), then hand <see cref="All"/> — or individual tools — to
/// your agent. Requires the agent run to execute in the same circuit as the UI.
/// </summary>
public sealed class DrylUiTools
{
    private readonly IDrylDialogService _dialogs;

    private DrylUiTools(IDrylDialogService dialogs)
    {
        _dialogs = dialogs;
        AskChoice = AIFunctionFactory.Create(AskChoiceImpl, "ask_choice",
            "Ask the user to pick exactly one option from a list. Use for either/or decisions.");
        AskMultiChoice = AIFunctionFactory.Create(AskMultiChoiceImpl, "ask_multi_choice",
            "Ask the user to pick one or more options from a list.");
        RequestPermission = AIFunctionFactory.Create(RequestPermissionImpl, "request_permission",
            "Ask the user to allow or deny an action before performing it.");
        AskText = AIFunctionFactory.Create(AskTextImpl, "ask_text",
            "Ask the user a free-text question and get their typed answer.");
        All = new List<AITool> { AskChoice, AskMultiChoice, RequestPermission, AskText };
    }

    /// <summary>Create the tool set, capturing the circuit's dialog service.</summary>
    public static DrylUiTools Create(IDrylDialogService dialogs) => new(dialogs);

    /// <summary>Single-choice question tool (backed by <c>DrylAskChoiceDialog</c>).</summary>
    public AITool AskChoice { get; }

    /// <summary>Multi-choice question tool (backed by <c>DrylAskMultiChoiceDialog</c>).</summary>
    public AITool AskMultiChoice { get; }

    /// <summary>Permission tool (backed by the core <c>DrylConfirmDialog</c>).</summary>
    public AITool RequestPermission { get; }

    /// <summary>Free-text question tool (backed by <c>DrylAskTextDialog</c>).</summary>
    public AITool AskText { get; }

    /// <summary>All four tools — hand straight to the agent.</summary>
    public IList<AITool> All { get; }

    private async Task<string> AskChoiceImpl(
        [Description("The question to ask the user.")] string question,
        [Description("The options the user can choose from.")] string[] options,
        [Description("The option you recommend (must match one of options), or null.")] string? recommended = null,
        CancellationToken ct = default)
    {
        var p = new DialogParameters
        {
            ["Question"] = question,
            ["Options"] = options,
            ["Recommended"] = recommended,
        };
        var dialogOptions = new DialogOptions
        {
            AnimateHandoff = true,
            HandoffStyle = DrylViewTransitionStyle.DepthGlass,
        };
        var reference = await _dialogs.ShowAsync<DrylAskChoiceDialog>("Choose", p, dialogOptions);
        var result = await Await(reference, ct);
        return result.Canceled
            ? "The user cancelled the question."
            : result.DataAs<string>() ?? "(no selection)";
    }

    private async Task<string> AskMultiChoiceImpl(
        [Description("The question to ask the user.")] string question,
        [Description("The options the user can choose from.")] string[] options,
        [Description("The options you recommend (subset of options), or null.")] string[]? recommended = null,
        CancellationToken ct = default)
    {
        var p = new DialogParameters
        {
            ["Question"] = question,
            ["Options"] = options,
            ["Recommended"] = recommended,
        };
        var dialogOptions = new DialogOptions
        {
            AnimateHandoff = true,
            HandoffStyle = DrylViewTransitionStyle.DepthGlass,
        };
        var reference = await _dialogs.ShowAsync<DrylAskMultiChoiceDialog>("Choose", p, dialogOptions);
        var result = await Await(reference, ct);
        if (result.Canceled) return "The user cancelled the question.";
        var chosen = result.DataAs<string[]>() ?? Array.Empty<string>();
        return chosen.Length == 0 ? "(no selection)" : string.Join(", ", chosen);
    }

    private async Task<string> RequestPermissionImpl(
        [Description("The action you want permission to perform.")] string action,
        [Description("Optional extra detail shown to the user.")] string? details = null,
        CancellationToken ct = default)
    {
        var message = string.IsNullOrWhiteSpace(details) ? action : $"{action}\n\n{details}";
        var dialogOptions = new DialogOptions
        {
            AnimateHandoff = true,
            HandoffStyle = DrylViewTransitionStyle.DepthGlass,
        };
        var showTask = _dialogs.ShowConfirmAsync("Permission required", message, "Allow", "Deny", dialogOptions);

        var tcs = new TaskCompletionSource<DialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using (ct.Register(() => tcs.TrySetResult(DialogResult.Cancel())))
        {
            var result = await await Task.WhenAny(showTask, tcs.Task);
            return result.Canceled ? "false (the user denied permission)" : "true (the user allowed it)";
        }
    }

    private async Task<string> AskTextImpl(
        [Description("The question to ask the user.")] string question,
        [Description("Optional placeholder for the input field.")] string? placeholder = null,
        CancellationToken ct = default)
    {
        var p = new DialogParameters
        {
            ["Question"] = question,
            ["Placeholder"] = placeholder,
        };
        var dialogOptions = new DialogOptions
        {
            AnimateHandoff = true,
            HandoffStyle = DrylViewTransitionStyle.DepthGlass,
        };
        var reference = await _dialogs.ShowAsync<DrylAskTextDialog>("Question", p, dialogOptions);
        var result = await Await(reference, ct);
        return result.Canceled
            ? "The user cancelled the question."
            : result.DataAs<string>() ?? string.Empty;
    }

    // Awaits the dialog result; if the run is cancelled first, cancels the dialog and reports it.
    private static async Task<DialogResult> Await(IDrylDialogReference reference, CancellationToken ct)
    {
        await using var _ = ct.Register(reference.Cancel).ConfigureAwait(false);
        return await reference.Result.ConfigureAwait(false);
    }
}
