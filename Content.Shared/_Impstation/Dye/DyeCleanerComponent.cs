using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Dye;

/// <summary>
///     Signifies an entity that, when washed alongside a dyed item, will remove the item's colour.
/// </summary>
[RegisterComponent]
public sealed partial class DyeCleanerComponent : Component
{
    [DataField]
    public bool DeleteOnUse;
}
