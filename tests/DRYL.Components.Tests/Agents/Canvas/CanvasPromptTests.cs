using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

public class CanvasPromptTests
{
    [Fact]
    public void SchemaText_documents_the_value_template()
    {
        Assert.Contains("{value}", CanvasPrompt.SchemaText);
        Assert.Contains("display template", CanvasPrompt.SchemaText);
    }
}
