using Content.Server._Impstation.WashingMachine.Components;
using Content.Server.Administration.Logs;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.DeviceLinking.Events;
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
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
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

// The goal here, for mq:
//
// WASHING MACHINE
// - is a crate you can put literally anything in
// - on activate, locks the open function and washes things in it
//      crayons dye 'dyeable' entities
//      maybe it also converts some entities into other entities
// what happens to a player in the washing machine? well they get fucking washed.
//      heat damage? blunt damage? not enough to ash paper. but a BIT
//      emag: it does More Damage Lots
// probably spawns a little vapor at the end (maybe adds heat? idk man)
//
// microwaves need water & power to function.
//      a certain amount of water will be used up by the microwave.
// soap, bleach, space cleaner... anything with a dyeremover component, maybe? can remove colours from entities.
// washing something will remove dna and fingerprints, and add 'lint' fibers
//
// no ui. if i end up stabbing myself in the foot with this one, so be it.
//
// GAMEPLAY GOALS
// - dyeing shit, obviously
// - putting nukies in the tumble dryer
// - hiding place for sneakthieves
// - "why is this air alarm going off" "someone left the dryer on"
// - washing machine microwavable egg
//
// CHECKLIST
// [] WashingMachineComponent
// [] ActiveWashingMachineComponent
// [X] ActivelyWashedComponent
// [] WashingMachineSystem
// [X] SharedWashingMachine
// [] DyeableComponent
// [] DyeRemover tag
// [] 'Dizzy' status effect
// [] YAML edits
//
// NOTES ON HOW CRATES WORK
//
// crate == uid.EntityStorage.Contents

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

            SubscribeLocalEvent<FoodRecipeProviderComponent, GetSecretRecipesEvent>(OnGetSecretRecipes);
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

        /// <summary>
        /// This event tries to get secret recipes that the microwave might be capable of.
        /// Currently, we only check the microwave itself, but in the future, the user might be able to learn recipes.
        /// </summary>
        private void OnGetSecretRecipes(Entity<FoodRecipeProviderComponent> ent, ref GetSecretRecipesEvent args)
        {
            foreach (ProtoId<FoodRecipePrototype> recipeId in ent.Comp.ProvidedRecipes)
            {
                if (_prototype.TryIndex(recipeId, out var recipeProto))
                {
                    args.Recipes.Add(recipeProto);
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

                    StartWash(uid, comp, args.User);
                },
                Impact = LogImpact.Low,
            };
            washVerb.Impact = LogImpact.Low;
            args.Verbs.Add(washVerb);
        }

        private void OnPowerChanged(Entity<WashingMachineComponent> ent, ref PowerChangedEvent args) //done
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
            if (!HasContents(uid) || HasComp<ActiveWashingMachineComponent>(uid) || !(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
                return;

            var solidsDict = new Dictionary<string, int>(); // yeah later issue for me
            var reagentDict = new Dictionary<string, FixedPoint2>();
            var malfunctioning = false;
            // TODO use lists of Reagent quantities instead of reagent prototype ids.
            EnsureComp<EntityStorageComponent>(uid, out var storeComp); // if we got this far we should already have the thing
            foreach (var item in storeComp.Contents.ContainedEntities.ToArray())
            {
                var ev = new BeingWashedEvent(uid, user);
                RaiseLocalEvent(item, ev);

                if (_tag.HasTag(item, "Lint"))
                {
                    malfunctioning = true;
                }

                if (_tag.HasTag(item, "Plastic"))
                {
                    var junk = Spawn(component.BadRecipeEntityId, Transform(uid).Coordinates);
                    _container.Insert(junk, component.Storage);
                    Del(item);
                    continue;
                }

                var activeWashedComp = AddComp<ActivelyWashedComponent>(item);
                activeWashedComp.WashingMachine = uid;

                string? solidID = null;
                int amountToAdd = 1;

                // If a microwave recipe uses a stacked item, use the default stack prototype id instead of prototype id
                if (TryComp<StackComponent>(item, out var stackComp))
                {
                    solidID = _prototype.Index<StackPrototype>(stackComp.StackTypeId).Spawn;
                    amountToAdd = stackComp.Count;
                }
                else
                {
                    var metaData = MetaData(item); //this simply begs for cooking refactor
                    if (metaData.EntityPrototype is not null)
                        solidID = metaData.EntityPrototype.ID;
                }

                if (solidID is null)
                    continue;

                if (!solidsDict.TryAdd(solidID, amountToAdd))
                    solidsDict[solidID] += amountToAdd;

                // only use reagents we have access to
                // you have to break the eggs before we can use them!
                if (!_solutionContainer.TryGetDrainableSolution(item, out var _, out var solution))
                    continue;

                foreach (var (reagent, quantity) in solution.Contents)
                {
                    if (!reagentDict.TryAdd(reagent.Prototype, quantity))
                        reagentDict[reagent.Prototype] += quantity;
                }
            }

            // Check recipes
            var getRecipesEv = new GetSecretRecipesEvent();
            RaiseLocalEvent(uid, ref getRecipesEv);

            List<FoodRecipePrototype> recipes = getRecipesEv.Recipes;
            recipes.AddRange(_recipeManager.Recipes);
            var portionedRecipe = recipes.Select(r =>
                CanSatisfyRecipe(component, r, solidsDict, reagentDict)).FirstOrDefault(r => r.Item2 > 0);

            _audio.PlayPvs(component.StartCookingSound, uid);
            var activeComp = AddComp<ActiveMicrowaveComponent>(uid); //microwave is now cooking
            activeComp.CookTimeRemaining = component.CurrentCookTimerTime * component.CookTimeMultiplier;
            activeComp.TotalTime = component.CurrentCookTimerTime; //this doesn't scale so that we can have the "actual" time
            activeComp.PortionedRecipe = portionedRecipe;
            //Scale tiems with cook times
            component.CurrentCookTimeEnd = _gameTiming.CurTime + TimeSpan.FromSeconds(component.CurrentCookTimerTime * component.CookTimeMultiplier);
            if (malfunctioning)
                activeComp.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.MalfunctionInterval);
            UpdateUserInterfaceState(uid, component);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveWashingMachineComponent, WashingMachineComponent, EntityStorageComponent>();

            while (query.MoveNext(out var uid, out var active, out var wash, out var storage)) //so this wont trigger if someone manually makes something a washing machine without also giving it entitystorage. if they try to wash them, then im pretty sure they'll be stuck making washing noises forever. things to consider.
            {
                active.WashTimeRemaining -= frameTime;
                RollMalfunction((uid, active, wash));

                //check if there's still wash time left
                if (active.WashTimeRemaining > 0)
                {
                    AddTemperature(wash, storage, frameTime);
                    continue;
                }

                //this means the cycle has finished.
                AddTemperature(wash, storage, Math.Max(frameTime + active.WashTimeRemaining, 0)); //Though there's still a little bit more heat to pump out

                if (active.PortionedRecipe.Item1 != null) // do crayon recipes
                {
                    var coords = Transform(uid).Coordinates;
                    for (var i = 0; i < active.PortionedRecipe.Item2; i++)
                    {
                        SubtractContents(microwave, active.PortionedRecipe.Item1);
                        Spawn(active.PortionedRecipe.Item1.Result, coords);
                        //remove 200u from solution
                    }
                }

                _container.EmptyContainer(storage.Contents);
                wash.CurrentWashTimeEnd = TimeSpan.Zero;
                _audio.PlayPvs(wash.CycleDoneSound, uid);
                StopWashing((uid, wash));
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
        #region Malfunction

        /// <summary>
        /// Handles the attempted washing of dryer lint
        /// </summary>
        /// <remarks>
        /// Returns false if the washing machine didn't explode, true if it exploded.
        /// </remarks>
        private void RollMalfunction(Entity<ActiveWashingMachineComponent, WashingMachineComponent> ent)
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
                _lightning.ShootRandomLightnings(ent, 1.0f, 2, MalfunctionSpark, triggerLightningEvents: false); // needs to be steam
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
