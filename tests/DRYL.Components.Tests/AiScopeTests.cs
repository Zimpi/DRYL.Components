using DRYL.Components;
using DRYL.Components.Ai;

namespace DRYL.Components.Tests;

/// <summary>
/// Unit tests for the AI-scope resolution rules shared by every AI-aware host.
/// <see cref="AiScope.Resolve"/> governs the ambient <see cref="AiState"/> and
/// <see cref="AiScope.ResolveAura"/> the <see cref="AiAura"/> variant. The precedence
/// (explicit parameter wins, otherwise inherit the surrounding <c>DrylAiScope</c>,
/// otherwise a fixed default) must hold identically wherever it is applied.
/// </summary>
public class AiScopeTests
{
    // ── Resolve (AiState): explicit wins → scope → None ──────────────────────

    [Fact]
    public void Resolve_ExplicitState_WinsOverScope()
    {
        var scope = new AiScope { State = AiState.Thinking };
        Assert.Equal(AiState.Streaming, AiScope.Resolve(AiState.Streaming, scope));
    }

    [Fact]
    public void Resolve_NoneState_InheritsScope()
    {
        var scope = new AiScope { State = AiState.Thinking };
        Assert.Equal(AiState.Thinking, AiScope.Resolve(AiState.None, scope));
    }

    [Fact]
    public void Resolve_NoneState_NoScope_FallsBackToNone()
    {
        Assert.Equal(AiState.None, AiScope.Resolve(AiState.None, null));
    }

    // ── ResolveAura (AiAura): explicit wins → scope → Comet ──────────────────

    [Fact]
    public void ResolveAura_ExplicitAurora_WinsOverScope()
    {
        var scope = new AiScope { Aura = AiAura.Comet };
        Assert.Equal(AiAura.Aurora, AiScope.ResolveAura(AiAura.Aurora, scope));
    }

    [Fact]
    public void ResolveAura_ExplicitComet_WinsOverAuroraScope()
    {
        // Comet is a real value (0), not an "unset" sentinel — an explicit Comet must
        // still override an Aurora scope. The opt-out is null, never Comet.
        var scope = new AiScope { Aura = AiAura.Aurora };
        Assert.Equal(AiAura.Comet, AiScope.ResolveAura(AiAura.Comet, scope));
    }

    [Fact]
    public void ResolveAura_Null_InheritsScopeVariant()
    {
        var scope = new AiScope { Aura = AiAura.Aurora };
        Assert.Equal(AiAura.Aurora, AiScope.ResolveAura(null, scope));
    }

    [Fact]
    public void ResolveAura_Null_ScopeWithoutAura_FallsBackToComet()
    {
        var scope = new AiScope { State = AiState.Thinking };   // Aura not pinned
        Assert.Equal(AiAura.Comet, AiScope.ResolveAura(null, scope));
    }

    [Fact]
    public void ResolveAura_Null_NoScope_FallsBackToComet()
    {
        Assert.Equal(AiAura.Comet, AiScope.ResolveAura(null, null));
    }
}
