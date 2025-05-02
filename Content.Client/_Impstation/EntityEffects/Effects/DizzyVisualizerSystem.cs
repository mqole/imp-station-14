using Content.Client.Atmos.Components;
using Content.Shared._Impstation.EntityEffects.Effects;
using Content.Shared.Atmos;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._Impstation.EntityEffects.Effects;

/// <summary>
/// This handles the display of dizzy effects.
/// </summary>
public sealed class DizzyVisualizerSystem : VisualizerSystem<DizzyVisualsComponent>
{
    [Dependency] private readonly PointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DizzyVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DizzyVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(EntityUid uid, DizzyVisualsComponent component, ComponentShutdown args)
    {
        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            sprite.LayerMapTryGet(DizzyVisualLayers.Dizzy, out var layer))
        {
            sprite.RemoveLayer(layer);
        }
    }

    private void OnComponentInit(EntityUid uid, DizzyVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        sprite.LayerMapReserveBlank(DizzyVisualLayers.Dizzy);
        sprite.LayerSetVisible(DizzyVisualLayers.Dizzy, false);
        sprite.LayerSetShader(DizzyVisualLayers.Dizzy, "unshaded");
        if (component.Sprite != null)
            sprite.LayerSetRSI(DizzyVisualLayers.Dizzy, component.Sprite);

        UpdateAppearance(sprite);
    }

    protected override void OnAppearanceChange(EntityUid uid, DizzyVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateAppearance(args.Sprite);
    }

    private void UpdateAppearance(SpriteComponent sprite)
    {
        if (!sprite.LayerMapTryGet(DizzyVisualLayers.Dizzy, out var index))
            return;
        sprite.LayerSetVisible(index, true);
    }
}

public enum DizzyVisualLayers : byte
{
    Dizzy
}
