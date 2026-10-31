using DRYL.Components;
using DRYL.Components.Ai;

namespace DRYL.Components.Tests;

public class DrylAiOperationTests
{
    [Fact]
    public void Proposed_sets_the_key_to_proposed_and_rests_there()
    {
        var service = new DrylAiActivityService();
        using var op = service.Begin("compose");

        op.Proposed();

        Assert.Equal(AiState.Proposed, service.GetState("compose"));
    }

    [Fact]
    public void Accepting_a_proposal_moves_the_key_to_generated()
    {
        var service = new DrylAiActivityService();
        using var op = service.Begin("compose");
        op.Proposed();

        op.Generated();

        Assert.Equal(AiState.Generated, service.GetState("compose"));
    }

    [Fact]
    public void Dismissing_a_proposal_by_disposing_settles_the_key_to_none()
    {
        var service = new DrylAiActivityService();
        var op = service.Begin("compose");
        op.Proposed();

        op.Dispose();

        Assert.Equal(AiState.None, service.GetState("compose"));
    }

    [Fact]
    public void An_implementation_written_before_Proposed_existed_inherits_a_no_op()
    {
        IDrylAiOperation legacy = new LegacyOperation();

        legacy.Proposed();   // compiles and does nothing

        Assert.False(((LegacyOperation)legacy).Touched);
    }

    private sealed class LegacyOperation : IDrylAiOperation
    {
        public bool Touched { get; private set; }
        public string Key => "legacy";
        public void Thinking() => Touched = true;
        public void Streaming() => Touched = true;
        public void Generated() => Touched = true;
        public void Dispose() => Touched = true;
    }
}
