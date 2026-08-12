namespace DRYL.Components;

/// <summary>The input type of a <see cref="DrylCommandArgument"/> — drives both the
/// manual input rendered in the palette and the argument's JSON-schema type.</summary>
public enum CommandArgType
{
    /// <summary>Free text (default).</summary>
    Text,
    /// <summary>Numeric input.</summary>
    Number,
    /// <summary>Boolean toggle.</summary>
    Boolean,
    /// <summary>One value chosen from <see cref="DrylCommandArgument.Options"/>.</summary>
    Choice
}
