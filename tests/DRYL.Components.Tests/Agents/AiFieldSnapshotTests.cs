using System.Text.Json;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

public class AiFieldSnapshotTests
{
    // JS interop deserializes with web defaults (camelCase, case-insensitive) — the JS module
    // returns { found, value, selStart, selEnd }.
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Deserializes_camel_case_js_payload()
    {
        var snap = JsonSerializer.Deserialize<AiFieldSnapshot>(
            """{"found":true,"value":"hello","selStart":1,"selEnd":3}""", Web)!;

        Assert.True(snap.Found);
        Assert.Equal("hello", snap.Value);
        Assert.Equal(1, snap.SelStart);
        Assert.Equal(3, snap.SelEnd);
        Assert.True(snap.HasSelection);
    }

    [Theory]
    [InlineData(-1, -1)]  // element without selection API
    [InlineData(4, 4)]    // caret only, no range
    [InlineData(0, 0)]    // caret at start
    public void No_selection_when_range_is_empty(int start, int end)
    {
        var snap = new AiFieldSnapshot { Found = true, Value = "hello", SelStart = start, SelEnd = end };
        Assert.False(snap.HasSelection);
    }

    [Fact]
    public void Not_found_never_has_selection()
    {
        var snap = new AiFieldSnapshot { Found = false, SelStart = 0, SelEnd = 3 };
        Assert.False(snap.HasSelection);
    }
}
