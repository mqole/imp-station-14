using Content.Shared._Impstation.DamageBarrier;
using Content.Shared.Actions;

namespace Content.Shared._Impstation.Gastropoids.SnailShell;

public sealed partial class SnailShellSystem : EntitySystem
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
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
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
            var ev1 = new ToggleShellSpriteEvent();
            RaiseLocalEvent(ent, ref ev1);
            return;
        }

        if (ent.Comp.Broken) // could prolly use a popup
            return;

        _barrier.ApplyDamageBarrier(ent, ent.Comp.DamageModifier, null, ent.Comp.ShellHitSound, ent.Comp.ShellBreakSound);
        var ev2 = new ToggleShellSpriteEvent();
        RaiseLocalEvent(ent, ref ev2);
        args.Handled = true;
    }

    private void OnSnailShellBreak(Entity<SnailShellComponent> ent, ref DamageBarrierBreakEvent args)
    {
        // ent.Comp.Broken = true;
        var ev = new SnailShellBreakEvent();
        RaiseLocalEvent(ent, ref ev);
        // how tf are we healing it?
    }
}

/// <summary>
/// Relayed when the snail shell breaks while in use
/// </summary>
[ByRefEvent]
public record struct SnailShellBreakEvent;

/// <summary>
/// Relayed upon using the action
/// </summary>
public sealed partial class SnailShellActionEvent : InstantActionEvent { }

/// <summary>
/// Relayed by <see cref="SharedSnailShellSystem"/> to toggle shell sprites when using the snail shell
/// </summary>
[ByRefEvent]
public record struct ToggleShellSpriteEvent;
