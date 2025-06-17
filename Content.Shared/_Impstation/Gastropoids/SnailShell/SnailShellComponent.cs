using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Gastropoids.SnailShell;

/// <summary>
/// Grants the entity with this component the ability to curl up in its shell, applying a temporary (breakable) damage barrier.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSnailShellSystem)), AutoGenerateComponentState]
public sealed partial class SnailShellComponent : Component
{
    /// <summary>
    /// The entity needed to perform the action. Granted upon the creation of the entity.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Action;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Damage modifiers granted to this entity when shell is activated.
    /// </summary>
    [DataField]
    public DamageModifierSet DamageModifier = default!;
    public bool Active = false;
    public bool Broken = false;

    /// <summary>
    /// Layers on the entity to be declared as 'shell', these will not be hidden when the entity goes into the shell
    /// </summary>
    [DataField]
    public List<string> ShellLayers = new();

    /// <summary>
    /// Sound the shell makes when hit.
    /// </summary>
    [DataField]
    public SoundSpecifier ShellHitSound = new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg"); // FIX LATER

    /// <summary>
    /// Sound the shell makes when broken
    /// </summary>
    [DataField]
    public SoundSpecifier ShellBreakSound = new SoundPathSpecifier("/Audio/Effects/metal_break1.ogg"); // FIX LATER
}
