using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.Fluids;
using Content.Shared.Power;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;

namespace Content.Shared._Impstation.Dye;

public abstract class SharedWashingMachineSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WashingMachineComponent, GetVerbsEvent<ActivationVerb>>(AddWashVerb);
        SubscribeLocalEvent<WashingMachineComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<WashingMachineComponent, PowerChangedEvent>(OnPowerChanged);
    }

    # region Housekeeping
    private void AddWashVerb(Entity<WashingMachineComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
            return;

        var washVerb = new ActivationVerb
        {
            Text = Loc.GetString("washing-verb-wash"),
            Act = () =>
            {
                TryStartWash(ent);
            },
            DoContactInteraction = true
        };
        args.Verbs.Add(washVerb);
    }

    private void OnBreak(Entity<WashingMachineComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        // broken state visuals
        StopWashing(ent);

        // TODO: this can probably be moved to yaml
        if (TryComp<SolutionContainerManagerComponent>(ent, out var solComp) &&
            _solution.TryGetSolution(solComp, ent.Comp.Solution, out var solution) &&
            solution is not null)
        {
            _puddle.TrySpillAt(Transform(ent).Coordinates, solution, out _);
        }
    }

    private void OnPowerChanged(Entity<WashingMachineComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            // idle state visuals
            StopWashing(ent);
        }
    }

    #endregion
    #region Washing
    private bool TryStartWash(Entity<WashingMachineComponent> ent)
    {
        // TODO: POPUPS
        // its broken
        if (ent.Comp.Broken)
        {
            return false;
        }
        // its in use
        if (HasComp<ActiveWashingMachineComponent>(ent))
        {
            return false;
        }
        // theres no power
        // its open
        if (!TryComp<EntityStorageComponent>(ent, out var storeComp))
        {
            return false;
        }
        // theres no water

        // ok now start :)
        StartWash(ent, storeComp);
        return true;
    }

    /// <summary>
    ///    Use <see cref="TryStartWash"/> first.
    /// </summary>
    private void StartWash(Entity<WashingMachineComponent> ent, EntityStorageComponent storeComp)
    {
        // enter washing state
        EnsureComp<ActiveWashingMachineComponent>(ent);
        // set the door to be sealed shut

        // apply washing state to all internal items
        foreach (var item in storeComp.Contents.ContainedEntities)
        {
            EnsureComp<ActivelyBeingWashedComponent>(item);
            // check if any items will make this washing machine malfunction
            // make anything who can get a status effect dizzy
        }

        var activeComp = EnsureComp<ActiveWashingMachineComponent>(ent);
        activeComp.WashTimeRemaining = ent.Comp.WashTimerTime;
        // set our timer
        // get the endtime
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // query active washmachine
        var query = EntityQueryEnumerator<ActiveWashingMachineComponent, WashingMachineComponent>();

        while (query.MoveNext(out var uid, out var active, out var wash))
        {
            // change timer
            active.WashTimeRemaining -= frameTime;

            // roll malfunction

            if (active.WashTimeRemaining > 0)
            {
                // add heat & steam - this should be its own function
                continue;
            }

            // when times up, add last bit of heat
            // and end wash
        }
    }


    /// <summary>
    ///     This is called when washing stops abruptly, although it also handles washing coming to a natural end.
    /// </summary>
    private void StopWashing(Entity<WashingMachineComponent> ent)
    {
        // remcompdef active
        // do the same for all internal entities
        // make it openable again
        // empty container
    }
    #endregion
}
