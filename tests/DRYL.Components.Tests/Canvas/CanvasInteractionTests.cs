using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasInteractionTests
{
    [Fact]
    public void CanvasFormState_Get_returns_set_value()
    {
        var state = new CanvasFormState();
        state.Set("budget", 42);
        var value = state.Get("budget");
        Assert.Equal(42, value);
    }

    [Fact]
    public void CanvasFormState_Get_generic_returns_typed_value()
    {
        var state = new CanvasFormState();
        state.Set("budget", 42.0);
        var value = state.Get<double>("budget");
        Assert.Equal(42.0, value);
    }

    [Fact]
    public void CanvasFormState_Get_generic_returns_default_on_miss()
    {
        var state = new CanvasFormState();
        var value = state.Get<double>("nonexistent");
        Assert.Equal(0.0, value);
    }

    [Fact]
    public void CanvasFormState_Get_generic_returns_default_on_type_mismatch()
    {
        var state = new CanvasFormState();
        state.Set("budget", "not a double");
        var value = state.Get<double>("budget");
        Assert.Equal(0.0, value);
    }

    [Fact]
    public void CanvasFormState_Snapshot_is_defensive_copy()
    {
        var state = new CanvasFormState();
        state.Set("budget", 42);
        var snapshot = state.Snapshot();

        // Mutate the original state
        state.Set("budget", 100);

        // Snapshot should still have old value
        Assert.Equal(42, snapshot["budget"]);
    }

    [Fact]
    public void CanvasFormState_OnChanged_fires_on_Set()
    {
        var state = new CanvasFormState();
        var fired = false;
        state.OnChanged += () => fired = true;

        state.Set("budget", 42);
        Assert.True(fired);
    }

    [Fact]
    public void CanvasFormState_OnChanged_fires_multiple_times()
    {
        var state = new CanvasFormState();
        var count = 0;
        state.OnChanged += () => count++;

        state.Set("budget", 42);
        state.Set("name", "test");

        Assert.Equal(2, count);
    }

    [Fact]
    public void CanvasInteraction_ToPromptMessage_contains_intent()
    {
        var values = new Dictionary<string, object?> { { "budget", 42 } };
        var interaction = new CanvasInteraction("submit", "node-1", values);
        var message = interaction.ToPromptMessage();

        Assert.Contains("submit", message);
    }

    [Fact]
    public void CanvasInteraction_ToPromptMessage_contains_serialized_values()
    {
        var values = new Dictionary<string, object?> { { "budget", 42 } };
        var interaction = new CanvasInteraction("submit", "node-1", values);
        var message = interaction.ToPromptMessage();

        // Check for camelCase key preserved in JSON
        Assert.Contains("\"budget\":42", message);
    }

    [Fact]
    public void CanvasInteraction_is_record()
    {
        var values = new Dictionary<string, object?> { { "budget", 42 } };
        var i1 = new CanvasInteraction("submit", "node-1", values);
        var i2 = new CanvasInteraction("submit", "node-1", values);

        // Records use value equality
        Assert.Equal(i1, i2);
    }

    [Fact]
    public void CanvasInteraction_multiple_values_in_prompt()
    {
        var values = new Dictionary<string, object?>
        {
            { "budget", 42 },
            { "name", "test" }
        };
        var interaction = new CanvasInteraction("submit", "node-1", values);
        var message = interaction.ToPromptMessage();

        Assert.Contains("\"budget\":42", message);
        Assert.Contains("\"name\"", message);
    }
}
