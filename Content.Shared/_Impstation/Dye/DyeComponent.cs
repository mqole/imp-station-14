namespace Content.Shared._Impstation.Dye;

/// <summary>
///     Signifies that this entity can dye anything with <see cref="DyeableComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class DyeComponent : Component
{
    [DataField]
    public string Colour;
}
