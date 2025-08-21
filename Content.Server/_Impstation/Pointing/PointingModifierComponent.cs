namespace Content.Server._Impstation.Pointing;

[RegisterComponent]
public sealed partial class PointingModifierComponent : Component
{
    /// <summary>
    ///     Verb shown in the popup to the pointer.
    ///     eg. "You POINT at it".
    /// </summary>
    [DataField]
    public string TextSelf = "point";

    /// <summary>
    ///     Verb shown in the popup to viewers.
    ///     eg. "The person POINTS at it".
    /// </summary>
    [DataField]
    public string TextOther = "points";

    /// <summary>
    ///     Name of the entity to spawn at point location.
    /// </summary>
    [DataField]
    public string PointingArrow = "PointingArrow";
}
