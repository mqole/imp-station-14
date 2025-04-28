using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.WashingMachine
{
    public abstract class SharedWashingMachineSystem : EntitySystem { }

    [Serializable, NetSerializable]
    public enum WashingMachineVisualState
    {
        Idle,
        Active,
        Broken,
        Bloody
    }
    [Serializable, NetSerializable]
    public sealed class DyeableComponentState : ComponentState
    {
        public Color CurrentColor = default!;
    }
}
