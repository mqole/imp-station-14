using Content.Shared.Actions;
using Content.Shared.Actions.Components;

namespace Content.Shared._Impstation.Deadlock;

public sealed partial class MoKrillSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionsComponent, MoKrillScornEvent>(OnScorn);
        SubscribeLocalEvent<ActionsComponent, MoKrillBurrowEvent>(OnBurrow);
        SubscribeLocalEvent<ActionsComponent, MoKrillSandBlastEvent>(OnSandBlast);
        SubscribeLocalEvent<ActionsComponent, MoKrillComboEvent>(OnCombo);
    }

    private void OnScorn(Entity<ActionsComponent> ent, ref MoKrillScornEvent args)
    {
        // play sound.

        // get all entities in a certain radius.
        // damage them.
        // heal.
    }

    private void OnBurrow(Entity<ActionsComponent> ent, ref MoKrillBurrowEvent args)
    {
        // play sound.

        // make mokrill untargetable
        // swap sprite to a 'burrowing' layer

        // wait duration

        // get all entities in a certain radius.
        // damage them.
        // play sound, stop other sound.
        // swap sprite back.
        // become targetable again.
    }

    private void OnSandBlast(Entity<ActionsComponent> ent, ref MoKrillSandBlastEvent args)
    {
        // play sound.

        // get all entities in a certain radius.
        // damage them.
        // disarm them.
    }

    private void OnCombo(Entity<ActionsComponent> ent, ref MoKrillComboEvent args)
    {
        // play sound.

        // pull target entity to mokrill.
        // stun target entity.
        // continually do damage to that entity over a duration.
    }
}

public sealed partial class MoKrillScornEvent : InstantActionEvent { }
public sealed partial class MoKrillBurrowEvent : InstantActionEvent { }
public sealed partial class MoKrillSandBlastEvent : InstantActionEvent { }
public sealed partial class MoKrillComboEvent : EntityTargetActionEvent { }
