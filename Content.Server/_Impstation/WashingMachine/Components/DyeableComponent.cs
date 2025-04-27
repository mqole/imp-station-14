using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.WashingMachine.Components;

[RegisterComponent]
/// <summary>
/// Allows an entity with this component to be dyed in a washing machine.
/// </summary>
public sealed partial class DyeableComponent : Component
{
    [DataField]
    public Dictionary<string, EntProtoId> Recipes = new();

    [DataField]
    public bool AcceptAnyColor = true;
}

// some sorta system to convert recipe string into colour
// make rainbow a random colour, have null return transparent(or just null lol)
// idk man
