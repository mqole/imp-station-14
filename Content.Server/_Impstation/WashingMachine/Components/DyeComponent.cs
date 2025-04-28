namespace Content.Server._Impstation.WashingMachine.Components;

[RegisterComponent]
public sealed partial class DyeComponent : Component
{
    [DataField("color")]
    public string Color;
}
