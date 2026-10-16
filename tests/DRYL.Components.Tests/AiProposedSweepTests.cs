using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// The I20 risk was silent gaps: a component that maps AI states by hand and does not know
/// <see cref="AiState.Proposed"/> renders it as nothing. This sweep closes the door twice —
/// a source scan that makes every <c>Ai</c>-carrying component either compose the shared aura
/// or be accounted for by name, and render probes across the families that compose it.
/// </summary>
public class AiProposedSweepTests : BunitContext
{
    private const string Description = "AI suggestion, awaiting your decision";

    // Every Ai-carrying file that does not compose AiAuraCss + DrylAuraElements itself, and why
    // it still handles Proposed. A new component that lands here must be added with its reason.
    private static readonly Dictionary<string, string> AccountedFor = new()
    {
        ["DrylCanvas.razor"] = "no aura by design (spec S2): the wrapper DrylAiCanvas owns it",
        ["DrylSplitButton.razor"] = "hands Ai to its DrylButton segments",
        ["DrylImage.razor"] = "bespoke aura with Proposed, wave and description (AiProposedTests)",
        ["DrylErrorBoundary.razor"] = "hands Ai to the fallback it renders",
        ["DrylStep.razor"] = "rendered by DrylStepper, which composes the shared aura",
        ["DrylTab.razor"] = "rendered by DrylTabs, which maps Proposed (AiProposedTests)",
        ["DrylChartBase.cs"] = "AiAuraCss here, DrylAuraElements in each chart's markup",
        ["DialogOptions.cs"] = "data carrier for DrylDialog",
        ["DrylDialogReference.cs"] = "data carrier for DrylDialog",
        ["DrylNotification.cs"] = "data carrier for DrylNotifications rows, which map Proposed",
        ["DrylToastReference.cs"] = "data carrier for DrylToast",
        ["ToastOptions.cs"] = "data carrier for DrylToast",
    };

    private static string CoreDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DRYL.slnx"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "code", "DRYL.Components");
    }

    [Fact]
    public void Every_Ai_carrying_component_composes_the_shared_aura_or_is_accounted_for()
    {
        var unaccounted = new List<string>();
        var carriers = 0;
        foreach (var file in Directory.EnumerateFiles(CoreDir(), "*.*", SearchOption.AllDirectories)
                     .Where(f => (f.EndsWith(".razor") || f.EndsWith(".cs")) && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))
        {
            var name = Path.GetFileName(file);
            // A code-behind is read together with its .razor below.
            if (name is "DrylAiAware.cs" || name.EndsWith(".razor.cs")) continue;
            var behind = file + ".cs";
            var all = File.ReadAllText(file) + (File.Exists(behind) ? File.ReadAllText(behind) : "");
            var carriesAi = all.Contains("public AiState Ai ") || all.Contains("@inherits DrylAiAware") || all.Contains(": DrylAiAware");
            if (!carriesAi) continue;

            carriers++;
            var shared = all.Contains("AiAuraCss.Append") && all.Contains("<DrylAuraElements");
            if (!shared && !AccountedFor.ContainsKey(name)) unaccounted.Add(name);
        }

        // Guard against a scan that silently finds nothing (a moved folder, a renamed base).
        Assert.True(carriers >= 40, $"only {carriers} Ai-carrying files found");
        Assert.Empty(unaccounted);
    }

    [Fact]
    public void Every_accounted_for_file_still_exists()
    {
        var names = Directory.EnumerateFiles(CoreDir(), "*.*", SearchOption.AllDirectories).Select(Path.GetFileName).ToHashSet();

        Assert.All(AccountedFor.Keys, k => Assert.Contains(k, names));
    }

    // ── Render probes: one per family that composes the shared aura ──────

    public static TheoryData<Type> SharedHosts => new()
    {
        typeof(DrylButton), typeof(DrylAlert), typeof(DrylEmptyState), typeof(DrylCodeBlock),
        typeof(DrylStat), typeof(DrylTimelineItem), typeof(DrylExpansion), typeof(DrylCard),
        typeof(DrylMarkdown), typeof(DrylToolCall), typeof(DrylToolCallGroup),
        typeof(DrylInputText), typeof(DrylTextarea), typeof(DrylLineChart),
    };

    [Theory]
    [MemberData(nameof(SharedHosts))]
    public void A_shared_host_shows_the_proposal(Type type)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render(Fragment(type, AiState.Proposed));

        Assert.NotEmpty(cut.FindAll(".ai-aura.ai-proposed"));
        Assert.Contains(Description, cut.Markup);
    }

    [Theory]
    [MemberData(nameof(SharedHosts))]
    public void A_shared_host_hands_an_accept_to_the_wave(Type type)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<Probe>(ps => ps.Add(p => p.Type, type).Add(p => p.State, AiState.Proposed));

        cut.Render(ps => ps.Add(p => p.State, AiState.Generated));

        Assert.NotEmpty(cut.FindAll(".ai-aura.ai-from-proposed"));
        Assert.NotEmpty(cut.FindAll(".ai-aura-wave"));
    }

    /// <summary>Renders one host with a given state, so a test can move it to the next state.</summary>
    private sealed class Probe : ComponentBase
    {
        [Parameter] public Type Type { get; set; } = default!;
        [Parameter] public AiState State { get; set; }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b) => Fragment(Type, State)(b);
    }

    private static RenderFragment Fragment(Type type, AiState state) => b =>
    {
        b.OpenComponent(0, type);
        b.AddAttribute(1, "Ai", state);
        if (type == typeof(DrylCodeBlock)) b.AddAttribute(2, "Code", "var x = 1;");
        if (type == typeof(DrylStat)) { b.AddAttribute(2, "Label", "Revenue"); b.AddAttribute(3, "Value", "42"); }
        if (type == typeof(DrylMarkdown)) b.AddAttribute(2, "Content", "Draft");
        if (type == typeof(DrylToolCall)) b.AddAttribute(2, "Name", "send_email");
        if (type == typeof(DrylTimelineItem)) b.AddAttribute(2, "Title", "Step");
        if (type == typeof(DrylExpansion)) b.AddAttribute(2, "Title", "Details");
        if (type == typeof(DrylEmptyState)) b.AddAttribute(2, "Title", "Nothing yet");
        if (type == typeof(DrylLineChart)) b.AddAttribute(2, "Series", new ChartSeries[] { new("Revenue", new double[] { 3, 7, 5, 9 }) });
        b.CloseComponent();
    };
}
