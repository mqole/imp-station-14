using Content.Client.Clothing;
using Content.Shared.Clothing.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Reflection;
using Content.Shared._Impstation.WashingMachine;

namespace Content.Client._Impstation.WashingMachine;
public sealed class WashingMachineSystem : SharedWashingMachineSystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ClientClothingSystem _clothing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DyeableComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, DyeableComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not DyeableComponentState state)
            return;

        if (state.CurrentColor == component.CurrentColor)
            return;

        state.CurrentColor = component.CurrentColor;

        UpdateSpriteComponentAppearance(uid, component);
        UpdateClothingComponentAppearance(uid, component);
    }

    private void UpdateClothingComponentAppearance(EntityUid uid, DyeableComponent component, ClothingComponent? clothing = null)
    {
        if (!Resolve(uid, ref clothing, false))
            return;

        foreach (var slotPair in clothing.ClothingVisuals)
        {
            _clothing.SetLayerColor(clothing, slotPair.Key, "dye", component.CurrentColor);
        }
    }

    private void UpdateSpriteComponentAppearance(EntityUid uid, DyeableComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, false))
            return;

        foreach (var layer in component.CurrentColor)
        {
            int index;
            if (_reflection.TryParseEnumReference(layer.Key, out var @enum))
            {
                if (!sprite.LayerMapTryGet(@enum, out index, logError: true))
                    continue;
            }
            else if (!sprite.LayerMapTryGet(layer.Key, out index))
            {
                if (layer.Key is not { } strKey || !int.TryParse(strKey, out index))
                {
                    Log.Error($"Invalid key `{layer.Key}` for entity with dyed sprite {ToPrettyString(uid)}");
                    continue;
                }
            }
            sprite.LayerSetState(index, layer.Value.State);
            sprite.LayerSetColor(index, layer.Value.Color ?? Color.White);
        }
    }
}
