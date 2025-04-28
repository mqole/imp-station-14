using Content.Server._Impstation.WashingMachine.Components;
using Content.Server.Administration.Logs;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Kitchen.Components;
using Content.Server.Lightning;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Storage.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._Impstation.WashingMachine.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Server.Crayon;

namespace Content.Server._Impstation.WashingMachine.EntitySystems
{
    public sealed class WashingMachineSystem : EntitySystem
    {
        [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly LightningSystem _lightning = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
        [Dependency] private readonly RecipeManager _recipeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly SharedStackSystem _stack = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly TemperatureSystem _temperature = default!;
        [Dependency] private readonly AtmosphereSystem _atmos = default!;

        [ValidatePrototypeId<EntityPrototype>]
        private const string MalfunctionSpark = "Spark";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WashingMachineComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<WashingMachineComponent, GetVerbsEvent<ActivationVerb>>(AddWashVerb);

            SubscribeLocalEvent<WashingMachineComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<WashingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<WashingMachineComponent, SignalReceivedEvent>(OnSignalReceived);

            SubscribeLocalEvent<ActiveWashingMachineComponent, ComponentStartup>(OnWashStart);
            SubscribeLocalEvent<ActiveWashingMachineComponent, ComponentShutdown>(OnWashStop);
            SubscribeLocalEvent<ActivelyWashedComponent, OnConstructionTemperatureEvent>(OnConstructionTemp);
        }

        #region TODO

        private void SubtractContents(MicrowaveComponent component, FoodRecipePrototype recipe) // LATER
        {
            // TODO Turn recipe.IngredientsReagents into a ReagentQuantity[]

            var totalReagentsToRemove = new Dictionary<string, FixedPoint2>(recipe.IngredientsReagents);

            // this is spaghetti ngl
            foreach (var item in component.Storage.ContainedEntities)
            {
                // use the same reagents as when we selected the recipe
                if (!_solutionContainer.TryGetDrainableSolution(item, out var solutionEntity, out var solution))
                    continue;

                foreach (var (reagent, _) in recipe.IngredientsReagents)
                {
                    // removed everything
                    if (!totalReagentsToRemove.ContainsKey(reagent))
                        continue;

                    var quant = solution.GetTotalPrototypeQuantity(reagent);

                    if (quant >= totalReagentsToRemove[reagent])
                    {
                        quant = totalReagentsToRemove[reagent];
                        totalReagentsToRemove.Remove(reagent);
                    }
                    else
                    {
                        totalReagentsToRemove[reagent] -= quant;
                    }

                    _solutionContainer.RemoveReagent(solutionEntity.Value, reagent, quant);
                }
            }

            foreach (var recipeSolid in recipe.IngredientsSolids)
            {
                for (var i = 0; i < recipeSolid.Value; i++)
                {
                    foreach (var item in component.Storage.ContainedEntities)
                    {
                        string? itemID = null;

                        // If an entity has a stack component, use the stacktype instead of prototype id
                        if (TryComp<StackComponent>(item, out var stackComp))
                        {
                            itemID = _prototype.Index<StackPrototype>(stackComp.StackTypeId).Spawn;
                        }
                        else
                        {
                            var metaData = MetaData(item);
                            if (metaData.EntityPrototype == null)
                            {
                                continue;
                            }
                            itemID = metaData.EntityPrototype.ID;
                        }

                        if (itemID != recipeSolid.Key)
                        {
                            continue;
                        }

                        if (stackComp is not null)
                        {
                            if (stackComp.Count == 1)
                            {
                                _container.Remove(item, component.Storage);
                            }
                            _stack.Use(item, 1, stackComp);
                            break;
                        }
                        else
                        {
                            _container.Remove(item, component.Storage);
                            Del(item);
                            break;
                        }
                    }
                }
            }
        }

        #endregion
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

                // if it has a body, make it take a lil damage

                var activeWashedComp = AddComp<ActivelyWashedComponent>(item);
                activeWashedComp.WashingMachine = uid;

                // recipe shit goes here probably
            }

            // Check recipes

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
                    AddTemperature(wash, storage, frameTime);
                    tileMix?.AdjustMoles(Gas.WaterVapor, wash.BaseSteamOutput);
                    continue;
                }

                //this means the cycle has finished.
                AddTemperature(wash, storage, Math.Max(frameTime + active.WashTimeRemaining, 0)); //Though there's still a little bit more heat to pump out

                DoRecipes(uid, wash, storage);

                StopWashing((uid, wash));
                storage.Openable = true;
                _container.EmptyContainer(storage.Contents);
                wash.CurrentWashTimeEnd = TimeSpan.Zero;
                _audio.PlayPvs(wash.CycleDoneSound, uid);
            }
        }

        /// <summary>
        ///     Adds temperature to every item in the washing machine,
        ///     based on the time it took to wash.
        /// </summary>
        /// <param name="washComp">The washing machine that is heating up.</param>
        /// <param name="storeComp">The entity storage attached to the washing machine.</param>
        /// <param name="time">The time on the washing machine, in seconds.</param>
        private void AddTemperature(WashingMachineComponent washComp, EntityStorageComponent storeComp, float time) //done, but these 2 trycomps get called pretty often
        {
            var heatToAdd = time * washComp.BaseHeatMultiplier;
            foreach (var entity in storeComp.Contents.ContainedEntities)
            {
                if (TryComp<TemperatureComponent>(entity, out var tempComp))
                    _temperature.ChangeHeat(entity, heatToAdd * washComp.ObjectHeatMultiplier, false, tempComp);

                if (!TryComp<SolutionContainerManagerComponent>(entity, out var solutions))
                    continue;
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((entity, solutions)))
                {
                    var solution = soln.Comp.Solution;
                    if (solution.Temperature > washComp.TemperatureUpperThreshold)
                        continue;

                    _solutionContainer.AddThermalEnergy(soln, heatToAdd);
                }
            }
        }

        #endregion
        #region Recipe Shit

        /// <summary>
        /// Crayon + Dyeable item = profit
        /// </summary>
        private void DoRecipes(EntityUid uid, WashingMachineComponent wash, EntityStorageComponent store)
        {
            var contents = new List<EntityUid>(store.Contents.ContainedEntities);
            var recipeContents = new Dictionary<EntityUid, Component>();

            // thui-wa in hell
            bool containsDye = false;
            bool containsDyeable = false;
            bool containsCleaner = false;
            bool containsDyed = false;

            foreach (var item in contents)
            {
                if (TryComp<CrayonComponent>(item, out var dye))
                {
                    if (!recipeContents.TryAdd(item, dye))
                        recipeContents[item] = dye;
                    containsDyeable = true;
                }
                if (TryComp<DyeableComponent>(item, out var dyeable))
                {
                    if (!recipeContents.TryAdd(item, dyeable))
                        recipeContents[item] = dyeable;
                    containsDyeable = true;
                }
                if (TryComp<DyeCleanerComponent>(item, out var cleaner))
                {
                    if (!recipeContents.TryAdd(item, cleaner))
                        recipeContents[item] = cleaner;
                    containsCleaner = true;
                }
                if (TryComp<DyedComponent>(item, out var dyed))
                {
                    if (!recipeContents.TryAdd(item, dyed))
                        recipeContents[item] = dyed;
                    containsDyed = true;
                }
            }

            // THE DYEING PART
            if (containsDye && containsDyeable && !containsCleaner)
            {
                var uniqueRecipe = false;
                foreach (var dyeableItem in recipeContents)
                {
                    if (TryComp<DyeableComponent>(dyeableItem.Key, out var dyeable)) // theres gotta be a way to check value instead
                    {
                        // check for unique recipes first
                        foreach (var colorname in dyeable.Recipes.Keys)
                        {
                            if (uniqueRecipe)
                                break;
                            Color recipeColor = Color.FromName(colorname);// fuuuck this wont fucking work for rainbow!!! it needs to be a string!!! FUCK!!

                            foreach (var dyeItem in recipeContents)
                            {
                                if (TryComp<CrayonComponent>(dyeItem.Key, out var dye)) // just kill me now
                                {
                                    if (dye.Color == recipeColor)
                                    {
                                        _container.Remove(dyeableItem.Key, store.Contents);
                                        Del(dyeableItem.Key);
                                        Spawn(dyeable.Recipes[colorname], Transform(uid).Coordinates);
                                        uniqueRecipe = true;
                                        break;
                                    }
                                }
                            }
                        }
                        // not dying something that's been hit by unique recipe beam
                        if (!uniqueRecipe)
                            if (dyeable.AcceptAnyColor)
                            {
                                var crayonTally = 0;
                                foreach (var dyeItem in recipeContents)
                                    if (TryComp<CrayonComponent>(dyeItem.Key, out var dye))
                                        crayonTally += 1;

                                if (crayonTally > 1)
                                    MixColors(recipeContents, crayonTally)
                                // run the colour conversion to turn crayon dye into colour
                                // add new key layer to entity with that colour
                                // add component 'dyed' and register the original prototype, if it doesnt exist already
                            }
                    }
                }
            }

            // THE CLEANING PART
            // we handle this second bc if you put bleach in the washing machine as well as a red crayon it makes no sense to get a dyed thing out.

            if (containsCleaner && containsDyed)
            {
            }
            // make a fallback in case theorem gets 'dyed' added to himself and tries to undye
        }

        private void MixColors(Dictionary<EntityUid, Component> contents, int count)
        {

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

        #endregion
    }
}
