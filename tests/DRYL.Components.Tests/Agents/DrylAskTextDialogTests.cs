using Bunit;
using DRYL.Components.Agents.Tools;
using DRYL.Components.Dialogs;

namespace DRYL.Components.Tests.Agents;

public class DrylAskTextDialogTests : BunitContext
{
    private sealed class FakeInstance : IDrylDialogInstance
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string? Title => null;
        public DialogOptions Options { get; } = new();
        public AiState Ai => AiState.None;
        public DialogResult? Closed { get; private set; }
        public void Close(DialogResult result) => Closed = result;
        public void Cancel() => Closed = DialogResult.Cancel();
        public void SetAi(AiState state) { }
    }

    [Fact]
    public void Submit_returns_entered_text()
    {
        var instance = new FakeInstance();
        var cut = Render<DrylAskTextDialog>(p => p
            .AddCascadingValue<IDrylDialogInstance>(instance)
            .Add(x => x.Question, "Your name?"));

        cut.Find("input").Input("Jan");
        cut.FindAll("button").Last().Click();   // submit button

        Assert.NotNull(instance.Closed);
        Assert.False(instance.Closed!.Canceled);
        Assert.Equal("Jan", instance.Closed.DataAs<string>());
    }
}
