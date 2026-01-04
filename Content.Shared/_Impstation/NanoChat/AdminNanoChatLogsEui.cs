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
    public string Message { get; }

    public AdminNanoChatLogEntry(
        NetUserId senderUser,
        string message)
    {
        SenderUser = senderUser;
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
