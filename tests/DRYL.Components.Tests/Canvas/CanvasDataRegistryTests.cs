using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasDataRegistryTests
{
    public sealed record SalesParams(int Year, string? Region = null);
    public sealed record RangeParams(DateOnly From, DateOnly To, IReadOnlyList<string>? Tags = null);
    public sealed record ModeParams(SalesMode Mode);
    public sealed record BadParams(TimeSpan Window);

    public enum SalesMode { Gross, Net }

    private static IServiceCollection Services() => new ServiceCollection();

    private static CanvasDataRegistry Registry(IServiceCollection services) =>
        services.BuildServiceProvider().GetRequiredService<CanvasDataRegistry>();

    [Fact]
    public void Derives_required_and_optional_params_from_the_record()
    {
        var services = Services().AddDrylCanvasDataSource("sales.byMonth", "Umsatz je Monat in Tsd €.",
            (SalesParams p, CanvasDataContext _, CancellationToken _) =>
                Task.FromResult(CanvasData.Series(new[] { "Jan" }, ("Umsatz", new[] { 1d }))));

        var d = Assert.Single(Registry(services).Descriptors);

        Assert.Equal("sales.byMonth", d.Name);
        Assert.Equal(CanvasDataShape.Series, d.Shape);
        Assert.Collection(d.Params,
            p => { Assert.Equal("year", p.Name); Assert.Equal("int", p.TypeName); Assert.True(p.Required); },
            p => { Assert.Equal("region", p.Name); Assert.Equal("string", p.TypeName); Assert.False(p.Required); });
    }

    [Fact]
    public void Supports_dates_lists_and_enums()
    {
        var services = Services()
            .AddDrylCanvasDataSource("a.range", "Range.",
                (RangeParams _, CanvasDataContext _, CancellationToken _) =>
                    Task.FromResult(CanvasData.Rows(new[] { "c" }, Array.Empty<string[]>())))
            .AddDrylCanvasDataSource("a.mode", "Mode.",
                (ModeParams _, CanvasDataContext _, CancellationToken _) =>
                    Task.FromResult(CanvasData.Scalar(1)));

        var descriptors = Registry(services).Descriptors;

        var range = descriptors[0];
        Assert.Equal(new[] { "date", "date", "string[]" }, range.Params.Select(p => p.TypeName));
        Assert.Equal(new[] { true, true, false }, range.Params.Select(p => p.Required));
        Assert.Equal(CanvasDataShape.Rows, range.Shape);

        // An enum is a closed set — spelling out the literals is the cheapest way to stop the
        // model guessing at them.
        Assert.Equal("\"gross\"|\"net\"", Assert.Single(descriptors[1].Params).TypeName);
        Assert.Equal(CanvasDataShape.Scalar, descriptors[1].Shape);
    }

    [Fact]
    public void A_parameterless_source_has_an_empty_signature()
    {
        var services = Services().AddDrylCanvasDataSource("orders.open", "Offene Aufträge.",
            (CanvasDataContext _, CancellationToken _) =>
                Task.FromResult(CanvasData.Rows(new[] { "Nr" }, new[] { new[] { "4711" } })));

        Assert.Empty(Assert.Single(Registry(services).Descriptors).Params);
    }

    [Fact]
    public void An_unsupported_param_type_throws_at_registration()
    {
        // Not when the model happens to hit it — at startup, where a developer sees it.
        var ex = Assert.Throws<ArgumentException>(() => Services().AddDrylCanvasDataSource(
            "bad.source", "Bad.",
            (BadParams _, CanvasDataContext _, CancellationToken _) => Task.FromResult(CanvasData.Scalar(1))));

        Assert.Contains("window", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not supported", ex.Message);
    }

    [Fact]
    public void A_duplicate_source_name_throws_at_registration()
    {
        var services = Services().AddDrylCanvasDataSource("orders.open", "Offene Aufträge.",
            (CanvasDataContext _, CancellationToken _) => Task.FromResult(CanvasData.Scalar(1)));

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddDrylCanvasDataSource(
            "orders.open", "Noch mal.",
            (CanvasDataContext _, CancellationToken _) => Task.FromResult(CanvasData.Scalar(2))));

        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void AddDrylComponents_and_source_registration_share_one_registry()
    {
        // Order must not matter — a host may call either first.
        var a = Services().AddDrylComponents().AddDrylCanvasDataSource("x.one", "One.",
            (CanvasDataContext _, CancellationToken _) => Task.FromResult(CanvasData.Scalar(1)));
        var b = Services().AddDrylCanvasDataSource("x.one", "One.",
            (CanvasDataContext _, CancellationToken _) => Task.FromResult(CanvasData.Scalar(1))).AddDrylComponents();

        Assert.Single(Registry(a).Descriptors);
        Assert.Single(Registry(b).Descriptors);
    }

    [Fact]
    public void The_prompt_block_carries_name_signature_shape_and_description()
    {
        var services = Services()
            .AddDrylCanvasDataSource("sales.byMonth", "Umsatz je Monat in Tsd €.",
                (SalesParams _, CanvasDataContext _, CancellationToken _) =>
                    Task.FromResult(CanvasData.Series(new[] { "Jan" }, ("Umsatz", new[] { 1d }))))
            .AddDrylCanvasDataSource("orders.open", "Offene Aufträge.",
                (CanvasDataContext _, CancellationToken _) =>
                    Task.FromResult(CanvasData.Rows(new[] { "Nr" }, Array.Empty<string[]>())));

        var block = CanvasDataPrompt.Block(Registry(services).Descriptors);

        Assert.Contains("sales.byMonth(year: int, region?: string) -> series — \"Umsatz je Monat in Tsd €.\"", block);
        Assert.Contains("orders.open() -> rows — \"Offene Aufträge.\"", block);
        Assert.Contains("\"$field\"", block);
        Assert.Contains("Do NOT invent numbers", block);
    }

    [Fact]
    public void The_prompt_block_is_empty_without_registered_sources()
    {
        // A2: with no registry the generator's contract stays exactly as it was, so every
        // existing chat artifact keeps working.
        Assert.Equal(string.Empty, CanvasDataPrompt.Block(Array.Empty<CanvasDataDescriptor>()));
        Assert.Equal(string.Empty, CanvasDataPrompt.Block(null));
    }
}
