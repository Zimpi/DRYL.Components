using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

public class AiFieldPromptTests
{
    [Fact]
    public void Empty_field_prompt_contains_instruction_and_field_target()
    {
        var p = AiFieldPrompt.Build("Write a subject line", context: null, value: "", selection: null);

        Assert.StartsWith("Write a subject line", p);
        Assert.DoesNotContain("Current field value:", p);
        Assert.DoesNotContain("Selected portion", p);
        Assert.Contains("replacement text for the field", p);
        Assert.Contains("no quotes, no markdown fences, no explanation", p);
    }

    [Fact]
    public void Context_is_included_when_set()
    {
        var p = AiFieldPrompt.Build("Write a subject", "Mail body: hello world", "", null);

        Assert.Contains("Additional context:", p);
        Assert.Contains("Mail body: hello world", p);
    }

    [Fact]
    public void Field_value_is_quoted_in_triple_quotes()
    {
        var p = AiFieldPrompt.Build("Rewrite professionally", null, "yo, send me the report", null);

        Assert.Contains("Current field value:", p);
        Assert.Contains("\"\"\"\nyo, send me the report\n\"\"\"", p.Replace("\r\n", "\n"));
        Assert.Contains("replacement text for the field", p);
    }

    [Fact]
    public void Selection_switches_replacement_target_and_is_quoted()
    {
        var p = AiFieldPrompt.Build("Translate to English", null, "Hallo Welt, wie geht's", "Hallo Welt");

        Assert.Contains("Selected portion (transform ONLY this):", p);
        Assert.Contains("\"\"\"\nHallo Welt\n\"\"\"", p.Replace("\r\n", "\n"));
        Assert.Contains("replacement text for the selected portion", p);
        Assert.DoesNotContain("replacement text for the field", p);
    }

    [Theory]
    [InlineData("plain text", "plain text")]
    [InlineData("  padded  ", "padded")]
    [InlineData("\"quoted reply\"", "quoted reply")]
    [InlineData("```\nfenced reply\n```", "fenced reply")]
    [InlineData("```text\nfenced with lang\n```", "fenced with lang")]
    [InlineData("has \"inner\" quotes", "has \"inner\" quotes")]
    public void Clean_strips_wrapping_quotes_and_fences(string raw, string expected)
    {
        Assert.Equal(expected, AiFieldPrompt.Clean(raw));
    }
}
