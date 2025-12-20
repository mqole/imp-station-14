using Content.Shared.Mind.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Components; // imp unborgable
using Content.Shared.Chemistry.EntitySystems; // imp unborgable
using Content.Shared.Containers.ItemSlots; // imp unborgable
using Content.Shared.Fluids; // imp; for Grammar
using Content.Shared.Traits.Assorted; // imp unborgable
using Robust.Shared.Enums; // imp; for Gender
using Robust.Shared.GameObjects.Components.Localization; // imp
using Robust.Shared.Audio; // imp

namespace Content.Shared.Silicons.Borgs;

public abstract partial class SharedBorgSystem
{
    [Dependency] private readonly SharedPuddleSystem _puddle = default!; // imp
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!; // imp

    private static readonly EntProtoId SiliconBrainRole = "MindRoleSiliconBrain";
    private static readonly LocId UnborgableFailPopup = "unborgable-fail-popup"; // imp
    private static readonly SoundPathSpecifier UnborgableFailSound = new("/Audio/Effects/Fluids/splat.ogg"); // imp

    public void InitializeMMI()
    {
        SubscribeLocalEvent<MMIComponent, ComponentInit>(OnMMIInit);
        SubscribeLocalEvent<MMIComponent, EntInsertedIntoContainerMessage>(OnMMIEntityInserted);
        SubscribeLocalEvent<MMIComponent, MindAddedMessage>(OnMMIMindAdded);
        SubscribeLocalEvent<MMIComponent, MindRemovedMessage>(OnMMIMindRemoved);

        SubscribeLocalEvent<MMILinkedComponent, MindAddedMessage>(OnMMILinkedMindAdded);
        SubscribeLocalEvent<MMILinkedComponent, EntGotRemovedFromContainerMessage>(OnMMILinkedRemoved);

        SubscribeLocalEvent<MMIComponent, ItemSlotInsertAttemptEvent>(OnMMIAttemptInsert); // imp
    }

    private void OnMMIInit(Entity<MMIComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.BrainSlotId, ent.Comp.BrainSlot);
    }

    private void OnMMIEntityInserted(Entity<MMIComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked with the same game state

        if (args.Container.ID != ent.Comp.BrainSlotId)
            return;

        var brain = args.Entity;

        if (HasComp<UnborgableComponent>(brain)) // imp add
        {
            return;
        }

        var linked = EnsureComp<MMILinkedComponent>(brain);
        linked.LinkedMMI = ent.Owner;
        Dirty(brain, linked);

        //IMP EDIT: keep the pronouns of the brain inserted
        var grammar = EnsureComp<GrammarComponent>(brain);
        if (TryComp<GrammarComponent>(brain, out var formerSelf))
        {
            _grammar.SetGender((brain, grammar), formerSelf.Gender);
            //man-machine interface is not a proper noun, so i'm not setting proper here
        }
        //END IMP EDIT

        if (_mind.TryGetMind(brain, out var mindId, out var mindComp))
        {
            _mind.TransferTo(mindId, ent.Owner, true, mind: mindComp);

            if (!_roles.MindHasRole<SiliconBrainRoleComponent>(mindId))
                _roles.MindAddRole(mindId, SiliconBrainRole, silent: true);
        }

        _appearance.SetData(ent.Owner, MMIVisuals.BrainPresent, true);
    }

    private void OnMMIMindAdded(Entity<MMIComponent> ent, ref MindAddedMessage args)
    {
        _appearance.SetData(ent.Owner, MMIVisuals.HasMind, true);
    }

    private void OnMMIMindRemoved(Entity<MMIComponent> ent, ref MindRemovedMessage args)
    {
        //IMP EDIT: no brain, no gender, bucko
        if (TryComp<GrammarComponent>(ent, out var grammar))
        {
            _grammar.SetGender((ent, grammar), Gender.Neuter); // it/its
        }
        //END IMP EDIT

        _appearance.SetData(ent.Owner, MMIVisuals.HasMind, false);
    }

    private void OnMMILinkedMindAdded(Entity<MMILinkedComponent> ent, ref MindAddedMessage args)
    {
        if (ent.Comp.LinkedMMI == null || !_mind.TryGetMind(ent.Owner, out var mindId, out var mindComp))
            return;

        _mind.TransferTo(mindId, ent.Comp.LinkedMMI, true, mind: mindComp);
    }

    private void OnMMILinkedRemoved(Entity<MMILinkedComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked with the same game state

        if (Terminating(ent.Owner))
            return;

        if (ent.Comp.LinkedMMI is not { } linked)
            return;

        RemCompDeferred<MMILinkedComponent>(ent.Owner);

        if (_mind.TryGetMind(linked, out var mindId, out var mindComp))
        {
            if (_roles.MindHasRole<SiliconBrainRoleComponent>(mindId))
                _roles.MindRemoveRole<SiliconBrainRoleComponent>(mindId);

            _mind.TransferTo(mindId, ent.Owner, true, mind: mindComp);
        }

        _appearance.SetData(linked, MMIVisuals.BrainPresent, false);
    }

    // imp add
    private void OnMMIAttemptInsert(Entity<MMIComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        var brain = args.Item;
        if (HasComp<UnborgableComponent>(brain))
        {
            _popup.PopupEntity(Loc.GetString(UnborgableFailPopup), ent, Popups.PopupType.MediumCaution);
            _audio.PlayPvs(UnborgableFailSound, brain);
            if (_solution.TryGetSolution(brain, "food", out var solution))
            {
                if (solution != null)
                {
                    var solutions = (Entity<SolutionComponent>)solution;
                    _puddle.TrySpillAt(Transform(ent).Coordinates, solutions.Comp.Solution, out _);
                }
            }
            EntityManager.QueueDeleteEntity(brain);
        }
    }
}
