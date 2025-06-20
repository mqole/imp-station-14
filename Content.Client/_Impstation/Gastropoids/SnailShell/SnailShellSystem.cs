using Content.Shared._Impstation.Gastropoids.SnailShell;

namespace Content.Client._Impstation.Gastropoids.SnailShell;

/// <summary>
/// Handles sprite changes on toggling snail shell
/// </summary>
public sealed partial class SnailShellSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnailShellComponent, ToggleShellSpriteEvent>(OnToggleSprite);
    }

    /// <summary>
    /// Toggle between the two shell visual states
    /// </summary>
    private void OnToggleSprite(Entity<SnailShellComponent> ent, ref ToggleShellSpriteEvent args)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (ent.Comp.Active)
            ent.Comp.Active = false;
        else
            ent.Comp.Active = true;

        _appearance.SetData(ent, ShellVisuals.On, ent.Comp.Active, appearance);
        Dirty(ent);
    }
}
