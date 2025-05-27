using Content.Shared.Damage;

namespace Content.Shared._Impstation.DamageBarrier;

public sealed partial class SharedDamageBarrierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageBarrierComponent, BeforeDamageChangedEvent>(OnDamaged);
    }

    public void ApplyDamageBarrier(EntityUid uid)
    {
        EnsureComp<DamageBarrierComponent>(uid);
        // do parameter shit
    }

    private void OnDamaged(Entity<DamageBarrierComponent> ent, ref BeforeDamageChangedEvent args)
    {
        // does damage bypass whitelist?
        // if so, return
        // maybe we want a special case for piercing damage that also hurts the barrier.

        args.Cancelled = true;

        // apply the sum of damage to the barrier.
        // fuuuck maybe the barrier should have weaknesses. god damn!
        if (ent.Comp.BarrierHealth <= 0)
        {
            OnBreak(ent);
            return;
        }
        // play deflect noise here, since we dont want it to overlap with the break noise
    }

    private void OnBreak(Entity<DamageBarrierComponent> ent)
    {
        // play sound & all
        RemComp<DamageBarrierComponent>(ent);
        // run the break event on the entity
        // after all, the break event should have special behaviour for snails
    }
}
