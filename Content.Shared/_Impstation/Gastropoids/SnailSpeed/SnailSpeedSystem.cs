using Content.Shared.Movement.Systems;

namespace Content.Shared._Impstation.SnailSpeed;

/// <summary>
/// Allows mobs to produce materials using Thirst with <see cref="ExcretionComponent"/>.
/// </summary>
public abstract partial class SharedSnailSpeedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SnailSpeedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SnailSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnMapInit(Entity<SnailSpeedComponent> ent, ref MapInitEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    /// apply constant movespeed modifier as long as entity is not flying
	private void OnRefreshMovespeed(Entity<SnailSpeedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (_jetpack.IsUserFlying(ent))
            return;

        args.ModifySpeed(ent.Comp.SnailSlowdownModifier, ent.Comp.SnailSlowdownModifier);
    }

}
