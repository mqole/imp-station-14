using Content.Server._Impstation.WashingMachine.Components;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Storage.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._Impstation.WashingMachine;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server._Impstation.WashingMachine.EntitySystems
{
    /// <seealso cref="ChemicalWashingMachineAdapterComponent"/>
    public sealed class WashingMachineSystem : SharedWashingMachineSystem
    {
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
        [Dependency] private readonly EmagSystem _emag = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedBodySystem _body = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly TemperatureSystem _temperature = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WashingMachineComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<WashingMachineComponent, GetVerbsEvent<ActivationVerb>>(AddWashVerb);

            SubscribeLocalEvent<WashingMachineComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<WashingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<WashingMachineComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<WashingMachineComponent, GotEmaggedEvent>(OnEmagged);

            SubscribeLocalEvent<ActiveWashingMachineComponent, ComponentStartup>(OnWashStart);
            SubscribeLocalEvent<ActiveWashingMachineComponent, ComponentShutdown>(OnWashStop);
            SubscribeLocalEvent<ActivelyWashedComponent, OnConstructionTemperatureEvent>(OnConstructionTemp);

            SubscribeLocalEvent<DyeableComponent, ComponentGetState>(OnGetState);

            SubscribeLocalEvent<ChemicalWashingMachineAdapterComponent, WashingMachineGetReagentsEvent>(ChemicalGetReagents);
            SubscribeLocalEvent<ChemicalWashingMachineAdapterComponent, WashingMachineUseReagent>(ChemicalUseReagent);
        }

        #region Startup

        private void OnMapInit(Entity<WashingMachineComponent> ent, ref MapInitEvent args)
        {
            _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort);
        }
        private void AddWashVerb(EntityUid uid, WashingMachineComponent comp, GetVerbsEvent<ActivationVerb> args) // if interacting gets annoying, change to alternativeverb
        {
            // We need an actor to give the verb.
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            // Make sure ghosts can't use verb.
            if (!args.CanInteract)
                return;

            var washVerb = new ActivationVerb
            {
                Text = Loc.GetString("washing-verb-wash"),
                Act = () =>
                {
                    if (HasComp<ActiveWashingMachineComponent>(uid))
                    {
                        _popupSystem.PopupEntity(Loc.GetString("washing-popup-notify-wash-locked"), uid, actor.PlayerSession, PopupType.Large);
                        return;
                    }

                    // If either the actor or comp have magically vanished
                    if (actor.PlayerSession.AttachedEntity == null || !HasComp<WashingMachineComponent>(uid))
                        return;

                    if (!(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
                    {
                        _popupSystem.PopupEntity(Loc.GetString("washingmachine-component-interact-using-no-power"), uid, args.User);
                        return;
                    }

                    if (comp.Broken)
                    {
                        _popupSystem.PopupEntity(Loc.GetString("washingmachine-component-interact-using-broken"), uid, args.User);
                        return;
                    }

                    if (GetReagents(uid).Item1 < comp.WaterRequired)
                    {
                        _popupSystem.PopupEntity(Loc.GetString("washingmachine-component-interact-using-no-water"), uid, args.User);
                        return;
                    }

                    if (TryComp<EntityStorageComponent>(uid, out var storage) && storage.Open)
                    {
                        _popupSystem.PopupEntity(Loc.GetString("washingmachine-component-interact-using-open"), uid, args.User);
                        return;
                    }

                    StartWash(uid, comp, args.User);
                },
                Impact = LogImpact.Low,
            };
            washVerb.Impact = LogImpact.Low;
            args.Verbs.Add(washVerb);
        }

        private void OnPowerChanged(Entity<WashingMachineComponent> ent, ref PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                SetAppearance(ent, WashingMachineVisualState.Idle, ent.Comp);
                StopWashing(ent);
            }
        }

        private void OnSignalReceived(Entity<WashingMachineComponent> ent, ref SignalReceivedEvent args)
        {
            if (args.Port != ent.Comp.OnPort)
                return;

            if (ent.Comp.Broken || !_power.IsPowered(ent))
                return;

            StartWash(ent.Owner, ent.Comp, null);
        }

        private void OnBreak(Entity<WashingMachineComponent> ent, ref BreakageEventArgs args)
        {
            ent.Comp.Broken = true;
            SetAppearance(ent, WashingMachineVisualState.Broken, ent.Comp);
            StopWashing(ent);

            if (TryComp<EntityStorageComponent>(ent, out var storeComp))
            {
                _container.EmptyContainer(storeComp.Contents);
            }

            if (TryComp<SolutionContainerManagerComponent>(ent, out var solComp))
            {
                _solutionContainer.TryGetSolution(solComp, "tank", out var solution);
                if (solution != null)
                {
                    _puddle.TrySpillAt(Transform(ent).Coordinates, solution, out _);
                }
            }
        }

        #endregion
        #region Washing

        /// <summary>
        /// Starts Washing
        /// </summary>
        /// <remarks>
        /// im NOT fucking naming this wrhzhzh -mq
        /// </remarks>
        public void StartWash(EntityUid uid, WashingMachineComponent component, EntityUid? user)
        {
            if (HasComp<ActiveWashingMachineComponent>(uid) || !(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
                return;

            var malfunctioning = false;

            EnsureComp<EntityStorageComponent>(uid, out var storeComp); // if we got this far we should already have the thing
            storeComp.Openable = false;
            foreach (var item in storeComp.Contents.ContainedEntities.ToArray())
            {
                var ev = new BeingWashedEvent(uid, user);
                RaiseLocalEvent(item, ev);

                if (_tag.HasTag(item, "Lint"))
                {
                    malfunctioning = true;
                }

                var activeWashedComp = AddComp<ActivelyWashedComponent>(item);
                activeWashedComp.WashingMachine = uid;
            }

            _audio.PlayPvs(component.StartWashingSound, uid);
            var activeComp = AddComp<ActiveWashingMachineComponent>(uid); // washing machine is go!
            activeComp.WashTimeRemaining = component.WashTimerTime * component.WashTimeMultiplier;
            activeComp.TotalTime = component.WashTimerTime; //this doesn't scale so that we can have the "actual" time
            // Maybe this works maybe this doesnt idk.
            component.CurrentWashTimeEnd = _gameTiming.CurTime + TimeSpan.FromSeconds(component.WashTimerTime * component.WashTimeMultiplier);
            if (malfunctioning)
                activeComp.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.MalfunctionInterval);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveWashingMachineComponent, WashingMachineComponent, EntityStorageComponent>();

            while (query.MoveNext(out var uid, out var active, out var wash, out var storage)) //so this wont trigger if someone manually makes something a washing machine without also giving it entitystorage. if they try to wash them, then im pretty sure they'll be stuck making washing noises forever. things to consider.
            {
                active.WashTimeRemaining -= frameTime;
                var tileMix = _atmos.GetTileMixture(uid, excite: true);

                RollMalfunction((uid, active, wash), tileMix);

                //check if there's still wash time left
                if (active.WashTimeRemaining > 0)
                {
                    AddWashDamage(uid, wash, storage, frameTime);
                    tileMix?.AdjustMoles(Gas.WaterVapor, wash.BaseSteamOutput);
                    continue;
                }

                //this means the cycle has finished.
                AddWashDamage(uid, wash, storage, Math.Max(frameTime + active.WashTimeRemaining, 0)); //Though there's still a little bit more heat to pump out

                DoRecipes(uid, wash, storage);
                StopWashing((uid, wash));
                RaiseLocalEvent(uid, new WashingMachineUseReagent(wash.WaterRequired, false));

                var ents = storage.Contents.ContainedEntities.ToList();
                if (_emag.CheckFlag(uid, EmagType.Interaction))
                    foreach (var user in ents)
                        if (TryComp<BodyComponent>(user, out _))
                            _body.GibBody(user, true);

                storage.Openable = true;
                _container.EmptyContainer(storage.Contents);
                wash.CurrentWashTimeEnd = TimeSpan.Zero;
                _audio.PlayPvs(wash.CycleDoneSound, uid);
            }
        }

        /// <summary>
        ///     Adds temperature &/or damage to every item in the washing machine,
        ///     based on the time it took to wash.
        /// </summary>
        /// <param name="washComp">The washing machine that is heating up.</param>
        /// <param name="storeComp">The entity storage attached to the washing machine.</param>
        /// <param name="time">The time on the washing machine, in seconds.</param>
        private void AddWashDamage(EntityUid uid, WashingMachineComponent washComp, EntityStorageComponent storeComp, float time)
        {
            var heatToAdd = time * washComp.BaseHeatMultiplier;
            var damageToDo = time * washComp.Damage;
            var emaggedDamage = damageToDo;
            foreach (var entity in storeComp.Contents.ContainedEntities)
            {
                if (TryComp<TemperatureComponent>(entity, out var tempComp))
                    _temperature.ChangeHeat(entity, heatToAdd * washComp.ObjectHeatMultiplier, false, tempComp);

                if (TryComp<SolutionContainerManagerComponent>(entity, out var solutions))
                    foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((entity, solutions)))
                    {
                        var solution = soln.Comp.Solution;
                        if (solution.Temperature > washComp.TemperatureUpperThreshold)
                            break;

                        _solutionContainer.AddThermalEnergy(soln, heatToAdd);
                    }
                // ideally if emagged, the entity should be crit (half dead) JUST as the cycle ends. theyre getting gibbed anyway so the specifics dont matter.
                if (_emag.CheckFlag(uid, EmagType.Interaction)
                && TryComp<MobThresholdsComponent>(entity, out var threshold)
                && TryComp<DamageableComponent>(entity, out var damageable))
                {
                    foreach (var damage in damageToDo.DamageDict)
                        emaggedDamage.DamageDict[damage.Key] = (threshold.Thresholds.Keys.Last() - damageable.TotalDamage) / 2 * time;
                    damageToDo = emaggedDamage;
                }
                _damageable.TryChangeDamage(entity, damageToDo);
            }
        }

        #endregion
        #region Recipe Shit

        /// <summary>
        /// Dye + Dyeable item = profit
        /// </summary>
        private void DoRecipes(EntityUid uid, WashingMachineComponent wash, EntityStorageComponent store)
        {
            var contents = new List<EntityUid>(store.Contents.ContainedEntities);

            var dyeContents = new List<Entity<DyeComponent>>();
            var dyeableContents = new List<Entity<DyeableComponent>>();
            var cleanerContents = new List<Entity<DyeCleanerComponent>>();
            var dyedContents = new List<Entity<DyeableComponent>>();

            // thui-wa in hell
            var containsDye = false;
            var containsDyeable = false;
            var containsCleaner = false;
            var containsDyed = false;

            foreach (var item in contents)
            {
                if (TryComp<DyeComponent>(item, out var dye))
                {
                    dyeContents.Add((item, dye));
                    containsDye = true;
                }
                if (TryComp<DyeableComponent>(item, out var dyeable))
                {
                    dyeableContents.Add((item, dyeable));
                    containsDyeable = true;
                    if (dyeable.Dyed == true)
                    {
                        dyedContents.Add((item, dyeable));
                        containsDyed = true;
                    }
                }
                if (TryComp<DyeCleanerComponent>(item, out var cleaner))
                {
                    cleanerContents.Add((item, cleaner));
                    containsCleaner = true;
                }
            }

            // THE DYEING PART
            if (containsDye && containsDyeable && !containsCleaner)
            {
                foreach (var dyeableItem in dyeableContents)
                {
                    var uniqueRecipe = false;
                    // check for unique recipes first
                    foreach (var (colorname, ent) in dyeableItem.Comp.Recipes)
                    {
                        if (uniqueRecipe)
                            break;
                        foreach (var dyeItem in dyeContents)
                        {
                            if (dyeItem.Comp.Color == colorname)
                            {
                                _container.Remove(dyeableItem.Owner, store.Contents);
                                Del(dyeableItem.Owner);
                                var spawned = Spawn(dyeableItem.Comp.Recipes[colorname], Transform(uid).Coordinates);
                                var spawnDyed = EnsureComp<DyedComponent>(spawned);
                                uniqueRecipe = true;
                                spawnDyed.OriginalEntity = MetaData(dyeableItem.Owner).EntityPrototype?.ID;
                                break;
                            }
                        }
                    }
                    // not dying something that's been hit by unique recipe beam
                    if (!uniqueRecipe)
                        if (dyeableItem.Comp.AcceptAnyColor)
                        {
                            var colorString = "";
                            var dyeTally = 0;
                            foreach (var dyeItem in dyeContents)
                            {
                                colorString = dyeItem.Comp.Color;
                                dyeTally += 1;
                            }
                            if (!Color.TryFromName(colorString, out var color))
                                color = Color.Transparent;
                            if (dyeTally > 1)
                                color = MixColors(dyeContents, dyeTally);

                            dyeableItem.Comp.CurrentColor = color;
                            dyeableItem.Comp.Dyed = true;
                            Dirty(dyeableItem.Owner, dyeableItem.Comp);
                        }
                }
            }
            // THE CLEANING PART
            if (containsCleaner || GetReagents(uid).Item2 >= wash.CleanerRequired && containsDyed)
            {
                foreach (var dyeableItem in dyedContents)
                {
                    if (GetReagents(uid).Item2 < wash.CleanerRequired && !containsCleaner)
                        break;
                    if (TryComp<DyedComponent>(dyeableItem, out var dyed) && dyed.OriginalEntity != null)
                    {
                        _container.Remove(dyeableItem.Owner, store.Contents);
                        Del(dyeableItem.Owner);
                        Spawn(dyed.OriginalEntity, Transform(uid).Coordinates);
                        continue;
                    }
                    dyeableItem.Comp.CurrentColor = Color.White;
                    Dirty(dyeableItem.Owner, dyeableItem.Comp);

                    if (GetReagents(uid).Item2 >= wash.CleanerRequired)
                        RaiseLocalEvent(uid, new WashingMachineUseReagent(wash.CleanerRequired, true));
                }
            }
        }

        private static Color MixColors(List<Entity<DyeComponent>> contents, int count)
        {
            Vector4 hsl = new(0, 0, 0, 255);
            Vector4 outColor;
            foreach (var dye in contents)
            {
                if (!Color.TryFromName(dye.Comp.Color, out var color))
                    color = Color.Transparent;
                outColor = Color.ToHsl(color);
                hsl.X += outColor.X;
                hsl.Y += outColor.Y;
                hsl.Z += outColor.Z;
            }
            hsl.X /= count;
            hsl.Y /= count;
            hsl.Z /= count;
            return Color.FromHsl(hsl);
        }
        private void OnGetState(EntityUid uid, DyeableComponent component, ref ComponentGetState args)
        {
            args.State = new DyeableComponentState()
            {
                CurrentColor = component.CurrentColor,
            };
        }

        #endregion
        #region Malfunction

        /// <summary>
        /// Handles the attempted washing of dryer lint
        /// </summary>
        /// <remarks>
        /// Returns false if the washing machine didn't explode, true if it exploded.
        /// </remarks>
        private void RollMalfunction(Entity<ActiveWashingMachineComponent, WashingMachineComponent> ent, GasMixture? tileMix)
        {
            if (ent.Comp1.MalfunctionTime == TimeSpan.Zero)
                return;

            if (ent.Comp1.MalfunctionTime > _gameTiming.CurTime)
                return;

            ent.Comp1.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(ent.Comp2.MalfunctionInterval);
            if (_random.Prob(ent.Comp2.ExplosionChance))
            {
                Explode((ent, ent.Comp2));
                return;  // washing machine broke, stop cycle
            }

            if (_random.Prob(ent.Comp2.SteamChance))
            {
                tileMix?.AdjustMoles(Gas.WaterVapor, ent.Comp2.BaseSteamOutput * ent.Comp2.MalfunctionSteamMultiplier);
            }
        }

        /// <summary>
        /// Explodes the washing machine internally, turning it into a broken state, destroying its board, and spitting out its machine parts
        /// </summary>
        /// <param name="ent"></param>
        public void Explode(Entity<WashingMachineComponent> ent)
        {
            ent.Comp.Broken = true; // Make broken so we stop processing stuff
            _explosion.TriggerExplosive(ent); // are microwaves explosive???
            if (TryComp<MachineComponent>(ent, out var machine))
            {
                _container.CleanContainer(machine.BoardContainer);
                _container.EmptyContainer(machine.PartContainer);
            }

            _adminLogger.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(ent)} exploded from unsafe washing!");
        }

        #endregion
        #region Helpers

        public bool HasContents(EntityUid uid)
        {
            if (TryComp<EntityStorageComponent>(uid, out var comp))
            {
                return comp.Contents.ContainedEntities.Any();
            }
            else return false;
        }

        public void SetAppearance(EntityUid uid, WashingMachineVisualState state, WashingMachineComponent? component = null, AppearanceComponent? appearanceComponent = null)
        {
            if (!Resolve(uid, ref component, ref appearanceComponent, false))
                return;
            var display = component.Broken ? WashingMachineVisualState.Broken : state;
            _appearance.SetData(uid, PowerDeviceVisuals.VisualState, display, appearanceComponent);
        }

        private void OnWashStart(Entity<ActiveWashingMachineComponent> ent, ref ComponentStartup args)
        {
            if (!TryComp<WashingMachineComponent>(ent, out var comp))
                return;
            SetAppearance(ent.Owner, WashingMachineVisualState.Active, comp);

            comp.PlayingStream =
                _audio.PlayPvs(comp.LoopingSound, ent, AudioParams.Default.WithLoop(true).WithMaxDistance(5))?.Entity;
        }

        private void OnWashStop(Entity<ActiveWashingMachineComponent> ent, ref ComponentShutdown args)
        {
            if (!TryComp<WashingMachineComponent>(ent, out var comp))
                return;

            SetAppearance(ent.Owner, WashingMachineVisualState.Idle, comp);
            comp.PlayingStream = _audio.Stop(comp.PlayingStream);
        }

        private void StopWashing(Entity<WashingMachineComponent> ent)
        {
            RemCompDeferred<ActiveWashingMachineComponent>(ent);
            if (TryComp<EntityStorageComponent>(ent, out var comp))
            {
                foreach (var item in comp.Contents.ContainedEntities)
                {
                    RemCompDeferred<ActivelyWashedComponent>(item);
                }
            }
        }

        // Stop items from transforming through constructiongraphs while being washed.
        // They might be reserved for dyes.
        private void OnConstructionTemp(Entity<ActivelyWashedComponent> ent, ref OnConstructionTemperatureEvent args)
        {
            args.Result = HandleResult.False;
        }

        private void OnEmagged(EntityUid uid, WashingMachineComponent _, ref GotEmaggedEvent args)
        {
            if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
                return;

            if (_emag.CheckFlag(uid, EmagType.Interaction))
                return;

            args.Handled = true;
        }

        #endregion
        #region Reagent Helpers
        public (FixedPoint2, FixedPoint2) GetReagents(EntityUid washing)
        {
            WashingMachineGetReagentsEvent getReagentsEvent = default;
            RaiseLocalEvent(washing, ref getReagentsEvent);
            return (getReagentsEvent.Water, getReagentsEvent.Cleaner);
        }

        private void ChemicalGetReagents(Entity<ChemicalWashingMachineAdapterComponent> entity, ref WashingMachineGetReagentsEvent args)
        {
            if (!_solutionContainer.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution, out var solution))
                return;

            FixedPoint2 water = 0;
            FixedPoint2 clean = 0;
            foreach (var (reagentId, multiplier) in entity.Comp.ReagentWater)
            {
                var reagent = solution.GetTotalPrototypeQuantity(reagentId);
                reagent += entity.Comp.FractionalReagents.GetValueOrDefault(reagentId) * FixedPoint2.Epsilon;

                water += reagent * multiplier;
            }
            foreach (var (reagentId, multiplier) in entity.Comp.ReagentCleaner)
            {
                var reagent = solution.GetTotalPrototypeQuantity(reagentId);
                reagent += entity.Comp.FractionalReagents.GetValueOrDefault(reagentId) * FixedPoint2.Epsilon;

                clean += reagent * multiplier;
            }

            args.Water = water;
            args.Cleaner = clean;
        }

        private void ChemicalUseReagent(Entity<ChemicalWashingMachineAdapterComponent> entity, ref WashingMachineUseReagent args)
        {
            if (!_solutionContainer.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution, out var solution))
                return;

            if (!TryComp<WashingMachineComponent>(entity, out var wash))
                return;

            var reagentUsed = entity.Comp.ReagentWater;
            var reagentRequired = wash.WaterRequired;
            if (args.Cleaning)
            {
                reagentUsed = entity.Comp.ReagentCleaner;
                reagentRequired = wash.CleanerRequired;
            }

            var totalReagent = 0f;
            foreach (var (reagentId, _) in reagentUsed)
            {
                totalReagent += solution.GetTotalPrototypeQuantity(reagentId).Float();
                totalReagent += entity.Comp.FractionalReagents.GetValueOrDefault(reagentId);
            }

            if (totalReagent == 0)
                return;

            foreach (var (reagentId, _) in reagentUsed)
            {
                _solutionContainer.RemoveReagent(entity.Comp.Solution.Value, reagentId, reagentRequired);
            }
        }

        /// <summary>
        /// Raised by <see cref="WashingMachineSystem"/> to calculate the amount of water &/or cleaning agent in the tank.
        /// </summary>
        [ByRefEvent]
        public record struct WashingMachineGetReagentsEvent(FixedPoint2 Water, FixedPoint2 Cleaner);

        /// <summary>
        /// Raised by <see cref="WashingMachineSystem"/> to draw water/cleaning agent from its adapters.
        /// </summary>
        /// <remarks>
        /// If Cleaning == false, water will be used.
        /// If Cleaning == true, cleaning agent will be used.
        /// </remarks>
        public record struct WashingMachineUseReagent(FixedPoint2 ReagentUsed, bool Cleaning);

        #endregion
    }
}
