using Content.Shared._DV.CartridgeLoader.Cartridges;
using Content.Shared._DV.NanoChat;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.NanoChat;

[Serializable, NetSerializable]
public sealed class AdminNanoChatLogsEuiState : EuiStateBase
{
    public List<AdminNanoChatLogEntry> Logs { get; }

    public AdminNanoChatLogsEuiState(List<AdminNanoChatLogEntry> logs)
    {
        Logs = logs;
    }
}

[Serializable, NetSerializable]
public sealed class AdminNanoChatLogEntry
{
    public EntityUid Sender { get; }
    public Entity<NanoChatCardComponent> Card { get; }
    public List<Entity<NanoChatCardComponent>>? Recipients { get; }
    public NanoChatMessage Message { get; }

    public AdminNanoChatLogEntry(EntityUid sender,
        Entity<NanoChatCardComponent> card,
        List<Entity<NanoChatCardComponent>> recipients,
        NanoChatMessage message)
    {
        Sender = sender;
        Card = card;
        Recipients = recipients;
        Message = message;
    }
}
