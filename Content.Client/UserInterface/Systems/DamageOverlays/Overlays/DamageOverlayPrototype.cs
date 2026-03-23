using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.DamageOverlays.Overlays;

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
