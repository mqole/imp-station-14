using Content.Client.Clothing;
using Content.Shared._Impstation.WashingMachine;
using Content.Shared.Clothing.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Reflection;

namespace Content.Client._Impstation.WashingMachine;

public sealed class WashingMachineSystem : SharedWashingMachineSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DyeableComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<DyeableComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not DyeableComponentState state)
            return;

        if (state.CurrentColor == ent.Comp.CurrentColor)
            return;

        // state.currentcolor will be changed by washing machines
        ent.Comp.CurrentColor = state.CurrentColor;

        UpdateSpriteComponentAppearance(ent);
        UpdateClothingComponentAppearance(ent);
    }

    private void UpdateClothingComponentAppearance(Entity<DyeableComponent, ClothingComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        foreach (var slotPair in ent.Comp2.ClothingVisuals)
            foreach (var layer in ent.Comp2.ClothingVisuals[slotPair.Key])
                layer.Color = ent.Comp1.CurrentColor;
    }

    private void UpdateSpriteComponentAppearance(Entity<DyeableComponent, SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        // TODO figure out why this doesnt work for inhands
        foreach (var layer in ent.Comp2.AllLayers)
        {
            layer.Color = ent.Comp1.CurrentColor;
        }
    }
}
