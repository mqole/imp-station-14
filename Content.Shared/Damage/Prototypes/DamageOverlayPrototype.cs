using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Prototypes;

/// <summary>
///     Prototype for a damage overlay applied to a client's window when their entity has sustained damage.
/// </summary>
[Prototype]
public sealed partial class DamageOverlayPrototype : IPrototype
{
    /// <inheritdoc/>/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Masking ShaderPrototype to be used for this overlay
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string ShaderMask = "GradientCircleMask";

    // TODO: yes, i know damage types are obsolete, but this is how damage overlays were already implemented and im not going to bother changing it for a medical system that isnt fully implemented. someone smarter can do that
    /// <summary>
    ///     Optional damage groups that can be set in order to
    ///     display this overlay when that damage is sustained.
    /// </summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>> DamageTypes = [];

    /// <summary>
    ///     If this entity has the pain numbness status effect,
    ///     should this overlay be hidden?
    /// </summary>
    [DataField]
    public bool HideOnPainNumbness = false;

    // We use 2 rings of shading so that transitions are smooth.
    // Ensure that values for these are OuterMax >= OuterMin > InnerMax >= InnerMin

    /// <summary>
    ///     Max radius of outer circle
    /// </summary>
    [DataField]
    public float OuterMaxLevel = 1f;

    /// <summary>
    ///     Min radius of outer circle
    /// </summary>
    [DataField]
    public float OuterMinLevel = 0.5f;

    /// <summary>
    ///     Max radius of inner circle
    /// </summary>
    [DataField]
    public float InnerMaxLevel = 0.2f;

    /// <summary>
    ///     Min radius of inner circle
    /// </summary>
    [DataField]
    public float InnerMinLevel = 0.1f;

    /// <summary>
    ///     Rate at which this shader pulses
    /// </summary>
    [DataField]

    public float PulseRate = 0f;

    /// <summary>
    ///     Colour of the overlay
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    ///     The maximum alpha for anything outside of the larger circle.
    /// </summary>
    [DataField]
    public float DarknessAlphaOuter = 1f;

    /// <summary>
    ///     <see cref="DamageOverlayRule"/> to be used
    /// </summary>
    [DataField]
    public DamageOverlayRule Rule = DamageOverlayRule.Static;
};

/// <summary>
///     Unique animation rules for damage overlays.
/// </summary>
public enum DamageOverlayRule : byte
{
    Static, // used for oxygen & brute overlay
    Fade, // used for crit overlay, (fades in on center of screen)
}
