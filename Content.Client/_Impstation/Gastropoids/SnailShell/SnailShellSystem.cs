using Content.Shared._Impstation.Gastropoids.SnailShell;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;

namespace Content.Client._Impstation.Gastropoids.SnailShell;

public sealed partial class SnailShellSystem : SharedSnailShellSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnailShellComponent, ToggleShellSpriteEvent>(ToggleShellSprite);
    }

    private void ToggleShellSprite(Entity<SnailShellComponent> ent, ref ToggleShellSpriteEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;

        if (!ent.Comp.Active)
        {
            _humanoid.SetLayersVisibility(ent.Owner, humanoid.BaseLayers.Keys, false);
            foreach (var shellLayer in ent.Comp.ShellLayers)
                _humanoid.SetLayerVisibility(ent.Owner, shellLayer, true);
        }
        else
            _humanoid.SetLayersVisibility(ent.Owner, humanoid.BaseLayers.Keys, true);
    }
}
