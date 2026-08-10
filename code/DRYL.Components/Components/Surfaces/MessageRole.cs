namespace DRYL.Components;

/// <summary>Author role of a <see cref="DrylMessage"/> — drives bubble alignment and styling.</summary>
public enum MessageRole
{
    /// <summary>The human participant — right-aligned, accent-tinted bubble.</summary>
    User,
    /// <summary>The AI / agent — left-aligned, glass bubble (hosts AI aura when streaming).</summary>
    Assistant,
    /// <summary>A system / status line — centered, muted, no bubble chrome.</summary>
    System
}
