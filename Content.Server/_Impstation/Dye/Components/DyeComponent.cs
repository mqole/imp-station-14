namespace Content.Server._Impstation.Dye.Components;

[RegisterComponent]
public sealed partial class DyeComponent : Component
{
    [DataField("color")]
    public string Color;
}
