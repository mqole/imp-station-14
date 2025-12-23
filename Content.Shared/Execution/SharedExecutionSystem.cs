using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Interaction.Events;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.Execution;

/// <summary>
///     Verb for violently murdering cuffed creatures.
/// </summary>
public sealed class SharedExecutionSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSuicideSystem _suicide = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedExecutionSystem _execution = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!; // IMP
    [Dependency] private readonly SharedGunSystem _gun = default!; // IMP

    private const string ChamberSlot = "gun_chamber"; // IMP, WHY IS THIS NOT A DATAFIELD IN GUNCOMPONENT?????????

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecutionComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionsVerbs);
        SubscribeLocalEvent<ExecutionComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<ExecutionComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);
        SubscribeLocalEvent<ExecutionComponent, ExecutionDoAfterEvent>(OnExecutionDoAfter);
    }

    private void OnGetInteractionsVerbs(EntityUid uid, ExecutionComponent comp, GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using.Value;
        var victim = args.Target;

        if (!CanBeExecuted(victim, attacker))
            return;

        UtilityVerb verb = new()
        {
            Act = () => TryStartExecutionDoAfter(weapon, victim, attacker, comp),
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private void TryStartExecutionDoAfter(EntityUid weapon, EntityUid victim, EntityUid attacker, ExecutionComponent comp)
    {
        if (!CanBeExecuted(victim, attacker))
            return;

        // IMP: determine which execution verbs to use
        var internalSelfPopup = comp.GunExecute ? comp.InternalSelfRangedExecutionMessage : comp.InternalSelfExecutionMessage;
        var externalSelfPopup = comp.GunExecute ? comp.ExternalSelfRangedExecutionMessage : comp.ExternalSelfExecutionMessage;
        var internalPopup = comp.GunExecute ? comp.InternalRangedExecutionMessage : comp.InternalMeleeExecutionMessage;
        var externalPopup = comp.GunExecute ? comp.ExternalRangedExecutionMessage : comp.ExternalMeleeExecutionMessage;
        // END IMP

        if (attacker == victim)
        {
            ShowExecutionInternalPopup(internalSelfPopup, attacker, victim, weapon); // imp variable popup
            ShowExecutionExternalPopup(externalSelfPopup, attacker, victim, weapon); // imp variable popup
        }
        else
        {
            ShowExecutionInternalPopup(internalPopup, attacker, victim, weapon); // imp variable popup
            ShowExecutionExternalPopup(externalPopup, attacker, victim, weapon); // imp variable popup
        }

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, comp.DoAfterDuration, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfter.TryStartDoAfter(doAfter);

    }

    public bool CanBeExecuted(EntityUid victim, EntityUid attacker)
    {
        // No point executing someone if they can't take damage
        if (!HasComp<DamageableComponent>(victim))
            return false;

        // You can't execute something that cannot die
        if (!TryComp<MobStateComponent>(victim, out var mobState))
            return false;

        // You're not allowed to execute dead people (no fun allowed)
        if (_mobState.IsDead(victim, mobState))
            return false;

        // You must be able to attack people to execute
        if (!_actionBlocker.CanAttack(attacker, victim))
            return false;

        // The victim must be incapacitated to be executed
        if (victim != attacker && _actionBlocker.CanInteract(victim, null))
            return false;

        // All checks passed
        return true;
    }

    private void OnGetMeleeDamage(Entity<ExecutionComponent> entity, ref GetMeleeDamageEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(entity, out var melee) || !entity.Comp.Executing)
        {
            return;
        }

        var bonus = melee.Damage * entity.Comp.DamageMultiplier - melee.Damage;
        args.Damage += bonus;
        args.ResistanceBypass = true;
    }

    private void OnSuicideByEnvironment(Entity<ExecutionComponent> entity, ref SuicideByEnvironmentEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(entity, out var melee))
            return;

        string? internalMsg = entity.Comp.CompleteInternalSelfExecutionMessage;
        string? externalMsg = entity.Comp.CompleteExternalSelfExecutionMessage;

        if (!TryComp<DamageableComponent>(args.Victim, out var damageableComponent))
            return;

        ShowExecutionInternalPopup(internalMsg, args.Victim, args.Victim, entity, false);
        ShowExecutionExternalPopup(externalMsg, args.Victim, args.Victim, entity);
        _audio.PlayPredicted(melee.HitSound, args.Victim, args.Victim);
        _suicide.ApplyLethalDamage((args.Victim, damageableComponent), melee.Damage);
        args.Handled = true;
    }

    private void ShowExecutionInternalPopup(string locString, EntityUid attacker, EntityUid victim, EntityUid weapon, bool predict = true)
    {
        if (predict)
        {
            _popup.PopupClient(
               Loc.GetString(locString, ("attacker", Identity.Entity(attacker, EntityManager)), ("victim", Identity.Entity(victim, EntityManager)), ("weapon", weapon)),
               attacker,
               attacker,
               PopupType.MediumCaution
               );
        }
        else
        {
            _popup.PopupEntity(
               Loc.GetString(locString, ("attacker", Identity.Entity(attacker, EntityManager)), ("victim", Identity.Entity(victim, EntityManager)), ("weapon", weapon)),
               attacker,
               attacker,
               PopupType.MediumCaution
               );
        }
    }

    private void ShowExecutionExternalPopup(string locString, EntityUid attacker, EntityUid victim, EntityUid weapon)
    {
        _popup.PopupEntity(
            Loc.GetString(locString, ("attacker", Identity.Entity(attacker, EntityManager)), ("victim", Identity.Entity(victim, EntityManager)), ("weapon", weapon)),
            attacker,
            Filter.PvsExcept(attacker),
            true,
            PopupType.MediumCaution
            );
    }

    private void OnExecutionDoAfter(Entity<ExecutionComponent> entity, ref ExecutionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        // IMP START, gun execute
        if (entity.Comp.GunExecute)
        {
            if (!TryComp<GunComponent>(entity, out var gun) ||
                !_gun.CanShoot(gun) ||
                _gun.GetAmmoCount(entity) > 0)
                return;
        }
        else // END IMP
            if (!TryComp<MeleeWeaponComponent>(entity, out /* IMP REPLACE 'var meleeWeaponComp' with discard*/_))
                return;

        var attacker = args.User;
        var victim = args.Target.Value;
        var weapon = args.Used.Value;

        if (!_execution.CanBeExecuted(victim, attacker))
            return;

        // This is needed so the melee system does not stop it.
        var prev = _combat.IsInCombatMode(attacker);
        _combat.SetInCombatMode(attacker, true);
        entity.Comp.Executing = true;

        // IMP EDIT: ternary assignments
        var internalMsg = entity.Comp.GunExecute ?
            entity.Comp.CompleteInternalRangedExecutionMessage :
            entity.Comp.CompleteInternalMeleeExecutionMessage;
        var externalMsg = entity.Comp.GunExecute ?
            entity.Comp.CompleteExternalRangedExecutionMessage :
            entity.Comp.CompleteExternalMeleeExecutionMessage;
        // END IMP

        // IMP ADD: bool check to see if the execution goes through
        bool attackSuccess = false;

        if (attacker == victim)
        {
            var suicideEvent = new SuicideEvent(victim);
            RaiseLocalEvent(victim, suicideEvent);

            var suicideGhostEvent = new SuicideGhostEvent(victim);
            RaiseLocalEvent(victim, suicideGhostEvent);
        }
        else
        {
            // IMP EDIT START: adding gun execute logic, adding attackSuccess to melee check
            if (entity.Comp.GunExecute &&
                TryComp<GunComponent>(entity, out var gun))
                attackSuccess = TryGunExecution(attacker, (entity, gun));
            else if (TryComp<MeleeWeaponComponent>(entity, out var meleeWeaponComp))
                attackSuccess = _melee.AttemptLightAttack(attacker, weapon, meleeWeaponComp, victim);
            // IMP END
        }

        _combat.SetInCombatMode(attacker, prev);
        entity.Comp.Executing = false;
        args.Handled = true;

        if (attacker != victim
            && attackSuccess) // IMP ADD
        {
            _execution.ShowExecutionInternalPopup(internalMsg, attacker, victim, entity);
            _execution.ShowExecutionExternalPopup(externalMsg, attacker, victim, entity);
        }
    }

    // IMP ADDITION
    private bool TryGunExecution(EntityUid user, Entity<GunComponent> gun)
    {
        // jesus christ ok we cannot just use AttemptShoot because someone might get in the way of the execution bullet,
        // so we have to SIMULATE the shot. in a stupid hacky way.
        var prevention = new ShotAttemptedEvent
        {
            User = user,
            Used = gun
        };
        RaiseLocalEvent(gun, ref prevention);
        if (prevention.Cancelled)
            return false;
        RaiseLocalEvent(user, ref prevention);
        if (prevention.Cancelled)
            return false;

        var attemptEv = new AttemptShootEvent(user, null);
        RaiseLocalEvent(gun, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            if (attemptEv.Message != null)
                _popup.PopupClient(attemptEv.Message, gun, user);
            return false;
        }

        // Remove ammo
        var fromCoordinates = Transform(user).Coordinates;
        var ev = new TakeAmmoEvent(1/*SORRY.*/, [], fromCoordinates, user);
        RaiseLocalEvent(gun, ev);
        var updateClientAmmoEvent = new UpdateClientAmmoEvent();
        RaiseLocalEvent(gun, ref updateClientAmmoEvent);

        // GOD I WISH SHAREDGUNSYSTEM WAS MORE MODULAR!!!!!!!!
        if (ev.Ammo.Count <= 0)
        {
            // triggers effects on the gun if it's empty
            var emptyGunShotEvent = new OnEmptyGunShotEvent(user);
            RaiseLocalEvent(gun, ref emptyGunShotEvent);

            // Play empty gun sounds if relevant
            _popup.PopupClient(ev.Reason ?? Loc.GetString("gun-magazine-fired-empty"), gun, user);

            _audio.PlayPredicted(gun.Comp.SoundEmpty, gun, user);

            return false;
        }

        // Shot confirmed. here we fucking go
        // MQ NOTE: this is the point where i had to get out of my chair and stand outside and look at the sky recontemplating my life decisions for 30 minutes

        return true;
    }
}
