using DRYL.Components.Agents.Tools;
using DRYL.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

public class DrylUiToolsTests
{
    // Fake service: completes every dialog with a preset result.
    private sealed class FakeDialogService : IDrylDialogService
    {
        private readonly DialogResult _result;
        public FakeDialogService(DialogResult result) => _result = result;

        public Task<IDrylDialogReference> ShowAsync<TDialog>(
            string? title = null, DialogParameters? parameters = null, DialogOptions? options = null)
            where TDialog : IComponent =>
            Task.FromResult<IDrylDialogReference>(new FakeRef(_result));

        public Task<DialogResult> ShowConfirmAsync(string title, string message,
            string confirmLabel = "Confirm", string cancelLabel = "Cancel", DialogOptions? options = null) =>
            Task.FromResult(_result);

        public Task<DialogResult> ShowAlertAsync(string title, string message,
            string okLabel = "OK", DialogOptions? options = null) => Task.FromResult(DialogResult.Ok());

        public event Action<IDrylDialogReference>? OnDialogInstanceAdded;
        public event Action<IDrylDialogReference>? OnDialogCloseRequested;
        public event Action<IDrylDialogReference>? OnDialogInstanceUpdated;

        private sealed class FakeRef : IDrylDialogReference
        {
            private readonly DialogResult _r;
            public FakeRef(DialogResult r) => _r = r;
            public Guid Id { get; } = Guid.NewGuid();
            public Task<DialogResult> Result => Task.FromResult(_r);
            public void Close(DialogResult result) { }
            public void Cancel() { }
        }
    }

    // A dialog whose Result never completes until cancelled.
    private sealed class NeverCompletingDialogService : IDrylDialogService
    {
        public Task<IDrylDialogReference> ShowAsync<TDialog>(
            string? title = null, DialogParameters? parameters = null, DialogOptions? options = null)
            where TDialog : IComponent =>
            Task.FromResult<IDrylDialogReference>(new PendingRef());

        public Task<DialogResult> ShowConfirmAsync(string title, string message,
            string confirmLabel = "Confirm", string cancelLabel = "Cancel", DialogOptions? options = null)
            => new PendingRef().Result;
        public Task<DialogResult> ShowAlertAsync(string title, string message,
            string okLabel = "OK", DialogOptions? options = null) => Task.FromResult(DialogResult.Ok());
        public event Action<IDrylDialogReference>? OnDialogInstanceAdded;
        public event Action<IDrylDialogReference>? OnDialogCloseRequested;
        public event Action<IDrylDialogReference>? OnDialogInstanceUpdated;

        private sealed class PendingRef : IDrylDialogReference
        {
            private readonly TaskCompletionSource<DialogResult> _tcs =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            public Guid Id { get; } = Guid.NewGuid();
            public Task<DialogResult> Result => _tcs.Task;
            public void Close(DialogResult result) => _tcs.TrySetResult(result);
            public void Cancel() => _tcs.TrySetResult(DialogResult.Cancel());
        }
    }

    private static async Task<string?> InvokeAsync(AITool tool, Dictionary<string, object> args)
    {
        var fn = (AIFunction)tool;
        var result = await fn.InvokeAsync(new AIFunctionArguments(args));
        return result?.ToString();
    }

    [Fact]
    public async Task AskText_returns_entered_text()
    {
        var tools = DrylUiTools.Create(new FakeDialogService(DialogResult.Ok("Jan")));
        var answer = await InvokeAsync(tools.AskText, new() { ["question"] = "Name?" });
        Assert.Contains("Jan", answer);
    }

    [Fact]
    public async Task RequestPermission_returns_true_on_confirm()
    {
        var tools = DrylUiTools.Create(new FakeDialogService(DialogResult.Ok(true)));
        var answer = await InvokeAsync(tools.RequestPermission, new() { ["action"] = "Delete file" });
        Assert.Contains("true", answer!.ToLowerInvariant());
    }

    [Fact]
    public async Task AskText_returns_declined_on_cancel()
    {
        var tools = DrylUiTools.Create(new FakeDialogService(DialogResult.Cancel()));
        var answer = await InvokeAsync(tools.AskText, new() { ["question"] = "Name?" });
        Assert.Contains("cancel", answer!.ToLowerInvariant());
    }

    [Fact]
    public void All_contains_four_tools()
    {
        var tools = DrylUiTools.Create(new FakeDialogService(DialogResult.Ok()));
        Assert.Equal(4, tools.All.Count);
    }

    [Fact]
    public async Task AskText_returns_cancelled_when_token_cancelled()
    {
        var tools = DrylUiTools.Create(new NeverCompletingDialogService());
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        var fn = (AIFunction)tools.AskText;
        var result = await fn.InvokeAsync(
            new AIFunctionArguments(new Dictionary<string, object> { ["question"] = "Name?" }),
            cts.Token);

        Assert.Contains("cancel", result!.ToString()!.ToLowerInvariant());
    }
}
