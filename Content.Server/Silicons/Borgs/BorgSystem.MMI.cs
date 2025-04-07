using Content.Server.Roles;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.Chemistry.Components;
using Content.Server.Popups;
using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Containers;
using Robust.Server.Audio;
using Content.Shared.Coordinates;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.EUI; //imp
using Content.Server._Impstation.Ghost; //imp
using Robust.Shared.Audio; //imp

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly EuiManager _euiManager = default!; //imp

    public SoundSpecifier MMIDissolve = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg"); //imp

    public void InitializeMMI()
    {
        SubscribeLocalEvent<MMIComponent, ComponentInit>(OnMMIInit);
        SubscribeLocalEvent<MMIComponent, EntInsertedIntoContainerMessage>(OnMMIEntityInserted);
        SubscribeLocalEvent<MMIComponent, MindAddedMessage>(OnMMIMindAdded);
        SubscribeLocalEvent<MMIComponent, MindRemovedMessage>(OnMMIMindRemoved);
        SubscribeLocalEvent<MMIComponent, ItemSlotInsertAttemptEvent>(OnMMIAttemptInsert);

        SubscribeLocalEvent<MMILinkedComponent, MindAddedMessage>(OnMMILinkedMindAdded);
        SubscribeLocalEvent<MMILinkedComponent, EntGotRemovedFromContainerMessage>(OnMMILinkedRemoved);
    }

    private void OnMMIInit(EntityUid uid, MMIComponent component, ComponentInit args)
    {
        if (!TryComp<ItemSlotsComponent>(uid, out var itemSlots))
            return;

        if (ItemSlots.TryGetSlot(uid, component.BrainSlotId, out var slot, itemSlots))
            component.BrainSlot = slot;
        else
            ItemSlots.AddItemSlot(uid, component.BrainSlotId, component.BrainSlot, itemSlots);
    }

    private void OnMMIEntityInserted(EntityUid uid, MMIComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != component.BrainSlotId)
            return;

        var ent = args.Entity;
        var linked = EnsureComp<MMILinkedComponent>(ent);
        linked.LinkedMMI = uid;
        Dirty(uid, component);

        if (_mind.TryGetMind(ent, out _, out var mind) && mind.Session is { } playerSession) //imp edit. is this even necessary
        {
            // imp: notify them they're being mmi'd.
            if (mind.CurrentEntity != ent)
            {
                _euiManager.OpenEui(new ReturnToMindEui(mind, _mind), playerSession);
            }
        }

        _appearance.SetData(uid, MMIVisuals.BrainPresent, true);
    }

    private void OnMMIMindAdded(EntityUid uid, MMIComponent component, MindAddedMessage args)
    {
        _appearance.SetData(uid, MMIVisuals.HasMind, true);
    }

    private void OnMMIMindRemoved(EntityUid uid, MMIComponent component, MindRemovedMessage args)
    {
        _appearance.SetData(uid, MMIVisuals.HasMind, false);
    }

    private void OnMMILinkedMindAdded(EntityUid uid, MMILinkedComponent component, MindAddedMessage args)
    //TODO: wait for checkbox!!!
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind) ||
            component.LinkedMMI == null)
            return;

        _mind.TransferTo(mindId, component.LinkedMMI, true, mind: mind);
        if (!_roles.MindHasRole<SiliconBrainRoleComponent>(mindId)) //imp, just in case
            _roles.MindAddRole(mindId, "MindRoleSiliconBrain", silent: true);
    }

    private void OnMMILinkedRemoved(EntityUid uid, MMILinkedComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (Terminating(uid))
            return;

        if (component.LinkedMMI is not { } linked)
            return;
        RemComp(uid, component);

        if (_mind.TryGetMind(linked, out var mindId, out var mind))
        {
            if (_roles.MindHasRole<SiliconBrainRoleComponent>(mindId))
                _roles.MindRemoveRole<SiliconBrainRoleComponent>(mindId);

            _mind.TransferTo(mindId, uid, true, mind: mind);
        }

        _appearance.SetData(linked, MMIVisuals.BrainPresent, false);
    }

    //Imp edit start
    //TODO: need checkbox to trigger this event instead
    private void OnMMIAttemptInsert(EntityUid uid, MMIComponent component, ItemSlotInsertAttemptEvent args)
    {
        var ent = args.Item;
        _popup.PopupEntity("The brain suddenly dissolves on contact with the interface!", uid, Shared.Popups.PopupType.MediumCaution);
        _audio.PlayPvs(MMIDissolve, uid);
        if (_solution.TryGetSolution(ent, "food", out var solution))
        {
            if (solution != null)
            {
                Entity<SolutionComponent> solutions = (Entity<SolutionComponent>)solution;
                _puddle.TrySpillAt(Transform(uid).Coordinates, solutions.Comp.Solution, out _);
            }
        }
        EntityManager.QueueDeleteEntity(ent);
    }
    //Imp edit end
}
