using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Radio;

/// <summary>
///     Component for an entity which can toggle between two modes and send a radio message depending on the entity state.
/// </summary>
[RegisterComponent]
public sealed partial class ToggleRadioMessageComponent : Component
{

    [DataField]
    public bool Toggled = false;

    /// <summary>
    ///     Message to send when toggled to 'off' state
    /// </summary>
    [DataField]
    public LocId? StandardMessage;

    /// <summary>
    ///     Message to send when toggled to 'on' state
    /// </summary>
    [DataField]
    public LocId? ToggledMessage;

    /// <summary>
    ///     Radio channel to send message on.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Common";
}
