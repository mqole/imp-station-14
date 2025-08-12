using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Medical.UI;

[Serializable, NetSerializable]
public enum TriageBotUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class TriageBotInterfaceState : BoundUserInterfaceState
{
    public string CurrentMode;

    public TriageBotInterfaceState(string currentMode)
    {
        CurrentMode = currentMode;
    }
}

[Serializable, NetSerializable]
public sealed class TriageBotModeChangedMessage : BoundUserInterfaceMessage
{
    public readonly string Mode;
    public TriageBotModeChangedMessage(string mode)
    {
        Mode = mode;
    }
}
