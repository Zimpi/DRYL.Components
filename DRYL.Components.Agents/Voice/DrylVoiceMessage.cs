namespace DRYL.Components.Agents;

/// <summary>Who said a line in a voice conversation.</summary>
public enum VoiceRole
{
    /// <summary>The person at the microphone.</summary>
    User,

    /// <summary>The assistant.</summary>
    Assistant,
}

/// <summary>
/// One spoken turn, transcribed. The transcript is what lets a voice conversation survive its
/// session: it seeds the next one and hands back to the text conversation.
/// </summary>
/// <param name="Role">Who spoke.</param>
/// <param name="Text">What was said, as text.</param>
public sealed record DrylVoiceMessage(VoiceRole Role, string Text);
