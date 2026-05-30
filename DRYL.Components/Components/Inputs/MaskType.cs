namespace DRYL.Components;

/// <summary>Predefined input mask formats for <see cref="DrylInputMask"/>.</summary>
public enum MaskType
{
    /// <summary>International phone number format: <c>+## ### ### ####</c>.</summary>
    Phone,

    /// <summary>IBAN format: <c>AA## #### #### #### #### ##</c>.</summary>
    Iban,

    /// <summary>German postal code (PLZ): five digits <c>#####</c>.</summary>
    PostalCode,

    /// <summary>Credit/debit card number: <c>#### #### #### ####</c>.</summary>
    CreditCard,

    /// <summary>Use the <see cref="DrylInputMask.CustomPattern"/> parameter to define the mask.</summary>
    Custom
}
