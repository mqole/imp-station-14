using Content.Shared.Damage.Prototypes;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.DamageOverlays;

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
    ///     Mask shader to be used for this overlay
    /// </summary>
    public ProtoId<ShaderPrototype> ShaderMask = "GradientCircleMask";

    // TODO: yes, i know damage types are obsolete, but this is how damage overlays were already implemented and im not going to bother changing it for a medical system that isnt fully implemented. someone smarter can do that
    /// <summary>
    ///     Optional damage groups that can be set in order to
    ///     display this overlay when that damage is sustained.
    /// </summary>
    public List<ProtoId<DamageTypePrototype>> DamageTypes = [];

    /// <summary>
    ///     If this entity has the pain numbness status effect,
    ///     should this overlay be hidden?
    /// </summary>
    public bool HideOnPainNumbness = false;

    // We use 2 rings of shading so that transitions are smooth.

    /// <summary>
    ///     Max radius of outer circle
    /// </summary>
    public float OuterMaxLevel = 0f;

    /// <summary>
    ///     Min radius of outer circle
    /// </summary>
    public float OuterMinLevel = 0f;

    /// <summary>
    ///     Max radius of inner circle
    /// </summary>
    public float InnerMaxLevel = 0f;

    /// <summary>
    ///     Min radius of inner circle
    /// </summary>
    public float InnerMinLevel = 0f;

    /// <summary>
    ///     Rate at which this shader pulses
    /// </summary>
    public float PulseRate = 0f;

    /// <summary>
    ///     Colour of the overlay
    /// </summary>
    public Color Color = Color.White;

    /// <summary>
    ///     The maximum alpha for anything outside of the larger circle.
    /// </summary>
    public float DarknessAlphaOuter = 0f;

    /// <summary>
    ///     <see cref="DamageOverlayRule"/> to be used
    /// </summary>
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
