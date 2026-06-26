using Bunit;
using DRYL.Components.Agents.Tools;
using DRYL.Components.Dialogs;

namespace DRYL.Components.Tests.Agents;

public class DrylAskChoiceDialogTests : BunitContext
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
    public void Defaults_to_recommended_and_returns_it_on_confirm()
    {
        var instance = new FakeInstance();
        var cut = Render<DrylAskChoiceDialog>(p => p
            .AddCascadingValue<IDrylDialogInstance>(instance)
            .Add(x => x.Question, "Pick one")
            .Add(x => x.Options, new[] { "A", "B", "C" })
            .Add(x => x.Recommended, "B"));

        cut.FindAll("button").Last().Click();   // confirm

        Assert.Equal("B", instance.Closed!.DataAs<string>());
    }
}
