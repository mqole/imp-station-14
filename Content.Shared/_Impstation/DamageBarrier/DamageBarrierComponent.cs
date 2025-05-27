using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.DamageBarrier;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageBarrierComponent : Component
{
    [DataField, AutoNetworkedField]
    public float BarrierHealth = 30f;

    // reflected types (use randomcolour dicts?)
    // timer
    // hit noise
    // break noise
}
