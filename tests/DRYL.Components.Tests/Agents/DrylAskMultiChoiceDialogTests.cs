using Bunit;
using DRYL.Components.Agents.Tools;
using DRYL.Components.Dialogs;

namespace DRYL.Components.Tests.Agents;

public class DrylAskMultiChoiceDialogTests : BunitContext
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
    public void Returns_prechecked_recommendations_on_confirm()
    {
        var instance = new FakeInstance();
        var cut = Render<DrylAskMultiChoiceDialog>(p => p
            .AddCascadingValue<IDrylDialogInstance>(instance)
            .Add(x => x.Question, "Pick several")
            .Add(x => x.Options, new[] { "A", "B", "C" })
            .Add(x => x.Recommended, new[] { "A", "C" }));

        cut.FindAll("button").Last().Click();   // confirm

        var result = instance.Closed!.DataAs<string[]>()!;
        Assert.Equal(new[] { "A", "C" }, result);
    }
}
