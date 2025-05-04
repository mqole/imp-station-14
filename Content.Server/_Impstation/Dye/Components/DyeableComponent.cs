using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Dye.Components;

[RegisterComponent, NetworkedComponent]
/// <summary>
/// Allows an entity with this component to be dyed in a washing machine.
/// </summary>
public sealed partial class DyeableComponent : Component
{
    [DataField]
    public Dictionary<string, EntProtoId> Recipes;

    [DataField]
    public bool AcceptAnyColor = true;
}
