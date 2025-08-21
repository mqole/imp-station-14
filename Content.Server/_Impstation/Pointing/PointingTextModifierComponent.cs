namespace Content.Server._Impstation.Pointing;

[RegisterComponent]
public sealed partial class PointingTextModifierComponent : Component
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
}
