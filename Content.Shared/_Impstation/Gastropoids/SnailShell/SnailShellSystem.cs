using Content.Shared._Impstation.DamageBarrier;
using Content.Shared.Actions;

namespace Content.Shared._Impstation.Gastropoids.SnailShell;

public sealed partial class SharedSnailShellSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDamageBarrierSystem _barrier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnailShellComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SnailShellComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SnailShellComponent, SnailShellActionEvent>(OnSnailShellAction);
        SubscribeLocalEvent<SnailShellComponent, DamageBarrierBreakEvent>(OnSnailShellBreak);
    }

    /// <summary>
    /// Gives the action to the entity
    /// </summary>
    private void OnStartup(Entity<SnailShellComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    /// <summary>
    /// Removes the action from the entity
    /// </summary>
    private void OnShutdown(Entity<SnailShellComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnSnailShellAction(Entity<SnailShellComponent> ent, ref SnailShellActionEvent args)
    {
        if (ent.Comp.Active)
        {
            RemCompDeferred<DamageBarrierComponent>(ent);
            // fix sprite
            return;
        }

        if (ent.Comp.Broken) // could prolly use a popup
            return;

        _barrier.ApplyDamageBarrier(ent, ent.Comp.DamageModifier, null, ent.Comp.ShellHitSound, ent.Comp.ShellBreakSound);
        // apply new sprite
        args.Handled = true;
    }

    private void OnSnailShellBreak(Entity<SnailShellComponent> ent, ref DamageBarrierBreakEvent args)
    {
        //ent.Comp.Broken = true;
        // make it look broken
        // how tf are we healing it?
    }
}

/// <summary>
/// Relayed upon using the action
/// </summary>
public sealed partial class SnailShellActionEvent : InstantActionEvent { }
