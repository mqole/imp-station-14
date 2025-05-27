using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.SnailSpeed;

/// <summary>
/// Should be applied to any mob that you want to be able to produce any material with an action and the cost of thirst.
/// TODO: Probably adjust this to utilize organs?
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSnailSpeedSystem)), AutoGenerateComponentState]
public sealed partial class SnailSpeedComponent : Component
{
    /// <summary>
    /// The amount of slowdown applied to snails.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SnailSlowdownModifier = 0.5f;
}
