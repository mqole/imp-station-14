using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Revenant.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Revenant.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class RevenantAnimatedComponent : Component
{
    /// <summary>
    ///     The revenant that animated this item. Used for initialization.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Entity<RevenantComponent>? Revenant;

    /// <summary>
    ///     Components added to make this item animated.
    ///     Removed when the item becomes inanimate.
    /// </summary>
    public List<Component> AddedComponents = [];

    /// <summary>
    ///     When the item should become inanimate. If null,
    ///     the item never becomes inanimate.
    /// </summary>
    [AutoPausedField]
    public TimeSpan? EndTime;

    /// <summary>
    ///     Accumulated frame time the component has been active for.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Accumulator = 0f;

    /// <summary>
    ///     The pointlight component attached to the animated entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? LightOverlay;

    [DataField]
    public Color LightColor = Color.MediumPurple;

    [DataField]
    public float LightRadius = 2f;

    /// <summary>
    ///     If the animated entity has no damage value provided,
    ///     it will deal this amount of damage.
    /// </summary>
    [DataField]
    public DamageSpecifier MeleeDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Blunt", 5 },
        },
    };
}
