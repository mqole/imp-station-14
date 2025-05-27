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
    /// True if the snail is curled up in its shell.
    /// </summary>
    public bool Active = false;
}
