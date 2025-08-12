using Content.Server.Radio.EntitySystems;

namespace Content.Server._Impstation.Radio;

public sealed class ToggleRadioMessageSystem : EntitySystem
{
    [Dependency] private readonly RadioSystem _radio = default!;


    /// <summary>
    ///     Toggles the state of this entity, which can then be called to send a variable message.
    /// </summary>
    public static void Toggle(Entity<ToggleRadioMessageComponent> ent)
    {
        if (ent.Comp.Toggled)
            ent.Comp.Toggled = false;
        else
            ent.Comp.Toggled = true;
    }

    /// <summary>
    ///     Sends a message from the entity to a radio channel. The message sent will depend on the state this entity is in.
    /// </summary>
    public void SendMessage(Entity<ToggleRadioMessageComponent> ent)
    {
        var message = ent.Comp.Toggled switch
        {
            false => ent.Comp.StandardMessage,
            true => ent.Comp.ToggledMessage
        };
        if (message == null)
            return;

        _radio.SendRadioMessage(ent, message, ent.Comp.RadioChannel, ent);
    }
}
