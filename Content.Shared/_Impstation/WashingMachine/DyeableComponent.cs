using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.WashingMachine;

[RegisterComponent, NetworkedComponent]
/// <summary>
/// Allows an entity with this component to be dyed in a washing machine.
/// </summary>
public sealed partial class DyeableComponent : Component
{
    [DataField]
    public Dictionary<string, EntProtoId> Recipes = new();

    [DataField]
    public bool AcceptAnyColor = true;
    [DataField]
    public bool Dyed = false;
    [DataField]
    public EntProtoId OriginalEntity;
    [DataField]
    public Color CurrentColor = default!;
}
