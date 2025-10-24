namespace Content.Shared._Impstation.Dye;

[RegisterComponent]
public sealed partial class ActiveWashingMachineComponent : Component
{
    [DataField]
    public bool Malfunctioning;

    [DataField]
    public float WashTimeRemaining;

    [DataField]
    public float TotalTime;
}
