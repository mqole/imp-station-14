using Content.Shared._DV.CartridgeLoader.Cartridges;
using Content.Shared.Eui;
using Robust.Shared.Network;
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
    public NetUserId SenderUser { get; }
    public NetEntity SenderEntity { get; }
    public NetEntity Card { get; }
    public List<NetEntity> Recipients { get; }
    public NanoChatMessage Message { get; }

    public AdminNanoChatLogEntry(
        NetUserId senderUser,
        NetEntity senderEntity,
        NetEntity card,
        List<NetEntity> recipients,
        NanoChatMessage message)
    {
        SenderUser = senderUser;
        SenderEntity = senderEntity;
        Card = card;
        Recipients = recipients;
        Message = message;
    }
}

public static class AdminNanoChatLogsEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Close : EuiMessageBase { }

    [Serializable, NetSerializable]
    public sealed class RefreshLogs : EuiMessageBase { }
}
