namespace Content.Shared._Impstation.Dye;

[RegisterComponent]
public sealed partial class WashingMachineComponent : Component
{
    [DataField]
    public bool Broken;

    [DataField]
    public string Solution = "tank";

    /// <summary>
    ///     Length of a cycle in seconds.
    /// </summary>
    [DataField]
    public uint WashTimerTime = 10;
}
