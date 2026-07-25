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

    // The schema is the model's only view of the catalog: a type that is not in it can never be
    // authored, and a line for a type that no longer exists teaches the model to fail validation.
    [Fact]
    public void Schema_mentions_every_catalog_type()
    {
        foreach (var type in CanvasCatalog.KnownTypes)
            Assert.Contains(type, CanvasPrompt.SchemaText);
    }

    // The schema is repeated verbatim in every generation. This is the budget the phase-4 spec
    // fixed: cross it and the answer is catalog compression, not one more line.
    [Fact]
    public void Schema_stays_under_budget() =>
        Assert.InRange(CanvasPrompt.SchemaText.Length, 1, 4500);

    [Fact]
    public void Schema_lists_the_new_container_types() =>
        Assert.Contains("stack, grid, card, tabs, accordion, form", CanvasPrompt.SchemaText);

    [Fact]
    public void Data_prompt_maps_rows_to_all_row_types()
    {
        var block = CanvasDataPrompt.Block(new[]
        {
            new CanvasDataDescriptor("orders.open", "Open orders.", CanvasDataShape.Rows,
                Array.Empty<CanvasParamInfo>()),
        });

        Assert.Contains("rows -> table|dataGrid|list|keyValue", block);
    }

    [Fact]
    public void Layout_budget_constrains_the_new_dense_types()
    {
        Assert.Contains("dataGrid", CanvasPrompt.LayoutBudget(400));
        Assert.Contains("kpi", CanvasPrompt.LayoutBudget(400));
        Assert.Contains("dataGrid", CanvasPrompt.LayoutBudget(1200));
    }
}
