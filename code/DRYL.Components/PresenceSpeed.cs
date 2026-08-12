namespace DRYL.Components;

/// <summary>
/// Playback speed of a <see cref="DrylPresence"/> enter/exit animation. Each value maps
/// onto one of the fixed motion duration tokens — no new durations are introduced.
/// </summary>
public enum PresenceSpeed
{
    /// <summary>Default speed — <c>--dur-med</c> (240 ms).</summary>
    Medium,

    /// <summary>Snappy — <c>--dur-fast</c> (140 ms). For small, frequent UI.</summary>
    Fast,

    /// <summary>Deliberate — <c>--dur-slow</c> (420 ms). For content reveals the user should notice, e.g. chat attachments.</summary>
    Slow
}
