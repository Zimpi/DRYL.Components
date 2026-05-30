using Microsoft.AspNetCore.Components;

namespace DRYL.Components;

/// <summary>
/// Cascading context provided by <see cref="DrylRadioGroup{TValue}"/> to its
/// <see cref="DrylRadio{TValue}"/> children.
/// </summary>
internal sealed record RadioGroupContext<TValue>(
    TValue? SelectedValue,
    EventCallback<TValue> OnChange,
    bool GroupDisabled,
    string Name);
