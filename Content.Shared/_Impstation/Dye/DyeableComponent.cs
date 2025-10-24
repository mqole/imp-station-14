using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Dye;

/// <summary>
///     Signifies that an entity can be affected by dye.
/// </summary>
[RegisterComponent]
public sealed partial class DyeableComponent : Component
{
    /// <summary>
    ///     Special recipes that transform this entity into another entity.
    ///     Colour here is stored as a string.
    /// </summary>
    [DataField]
    public Dictionary<string, EntProtoId> Recipes;

    /// <summary>
    ///     When false, only special recipes will change this entity's colour.
    /// </summary>
    [DataField]
    public bool AcceptAnyColor = true;

    [DataField]
    public bool IsDyed;

    /// <summary>
    ///     If this entity has been transformed into another, the original entity will be stored here.
    /// </summary>
    [DataField]
    public EntProtoId? OriginalProto = null;
}
