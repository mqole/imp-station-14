using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Ghost;

[Serializable, NetSerializable]
public sealed class ReturnToMMIMessage : EuiMessageBase
{
    public readonly bool Accepted;

    public ReturnToMMIMessage(bool accepted)
    {
        Accepted = accepted;
    }
}
