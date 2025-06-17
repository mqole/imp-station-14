using Content.Shared._Impstation.Gastropoids.SnailShell;
using Robust.Client.GameObjects;

namespace Content.Client._Impstation.Gastropoids.SnailShell;

public sealed partial class SnailShellSystem : SharedSnailShellSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnailShellComponent, ToggleShellSpriteEvent>(ToggleShellSprite);
    }

    private void ToggleShellSprite(Entity<SnailShellComponent> ent, ref ToggleShellSpriteEvent args)
    {
        // make a new list of layers on the entity which will remain visible
        var shellLayerList = new List<int>();
        foreach (var shellLayer in ent.Comp.ShellLayers)
        {
            _sprite.LayerMapTryGet(ent.Owner, shellLayer, out var shellLayerMap, false);
            shellLayerList.Add(shellLayerMap);
        }

        if (!ent.Comp.Active)
        {
            _sprite.SetVisible(ent.Owner, false);
            foreach (var layer in shellLayerList)
                _sprite.LayerSetVisible(ent.Owner, layer, true);
            return;
        }

        _sprite.SetVisible(ent.Owner, true);
    }
}
