using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.WashingMachine.Components;

[RegisterComponent]
public sealed partial class DyedComponent : Component
{
    [DataField]
    public EntProtoId? OriginalEntity;
}
