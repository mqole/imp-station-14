using Content.Shared.Actions;

namespace Content.Shared._Impstation.Gastropoids.SnailShell;

public sealed partial class SharedSnailShellSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnailShellComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SnailShellComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SnailShellComponent, SnailShellActionEvent>(OnSnailShellAction);
    }

    /// <summary>
    /// Gives the action to the entity
    /// </summary>
    private void OnStartup(Entity<SnailShellComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    /// <summary>
    /// Removes the action from the entity
    /// </summary>
    private void OnShutdown(Entity<SnailShellComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnSnailShellAction(Entity<SnailShellComponent> ent, ref SnailShellActionEvent args)
    {
        // toggle if already shelled
        // run barrier action
        // apply new sprite
        args.Handled = true;
    }
}

/// <summary>
/// Relayed upon using the action
/// </summary>
public sealed partial class SnailShellActionEvent : InstantActionEvent { }
