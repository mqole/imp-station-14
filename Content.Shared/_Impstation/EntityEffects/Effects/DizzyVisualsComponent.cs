namespace Content.Shared._Impstation.EntityEffects.Effects;

/// <summary>
/// Sets which sprite RSI is used for displaying dizzy visuals
/// </summary>
[RegisterComponent]
public sealed partial class DizzyVisualsComponent : Component
{
    [DataField("state")]
    public string? State;

    [DataField("sprite")]
    public string? Sprite;
}
