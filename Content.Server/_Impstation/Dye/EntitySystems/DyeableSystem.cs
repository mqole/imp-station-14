using Content.Server._Impstation.Dye.Components;
using Content.Server._Impstation.WashingMachine.Components;
using Content.Server._Impstation.WashingMachine.EntitySystems;
using Content.Server.Storage.Components;
using Content.Shared._Impstation.Dye;
using Robust.Shared.GameStates;

namespace Content.Server._Impstation.Dye.EntitySystems
{
    public sealed class DyeableSystem : EntitySystem
    {
        [Dependency] private readonly WashingMachineSystem _washing = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntityStorageComponent, StorageWashEvent>(OnStorageWashed);

            SubscribeLocalEvent<DyeableComponent, GotDyedEvent>(OnDyed);
            SubscribeLocalEvent<DyedComponent, GotCleanedEvent>(OnCleaned);
            SubscribeLocalEvent<DyedComponent, ComponentGetState>(OnGetState);
        }

        private void OnStorageWashed(Entity<EntityStorageComponent> ent, ref StorageWashEvent args)
        {
            var contents = new List<EntityUid>(ent.Comp.Contents.ContainedEntities);

            var dyes = new List<Entity<DyeComponent>>();
            var dyeables = new List<Entity<DyeableComponent>>();
            var cleaners = new List<Entity<DyeCleanerComponent>>();
            var dyeds = new List<Entity<DyedComponent>>();

            // making a list. checking it twice
            // theres gotta be a better way to do this but whatever
            foreach (var item in contents)
            {
                if (TryComp<DyeComponent>(item, out var dye))
                {
                    dyes.Add((item, dye));
                }
                if (TryComp<DyeableComponent>(item, out var dyeable))
                {
                    dyeables.Add((item, dyeable));
                }
                if (TryComp<DyeCleanerComponent>(item, out var cleaner))
                {
                    cleaners.Add((item, cleaner));
                }
                if (TryComp<DyedComponent>(item, out var dyed))
                {
                    dyeds.Add((item, dyed));
                }
            }

            foreach (var item in dyeables)
            {
                RaiseLocalEvent(item, new GotDyedEvent(dyes));
            }

            TryComp<WashingMachineComponent>(ent.Owner, out var wash);
            foreach (var item in dyeds)
            {
                // cancel if we have no cleaning agents or reagents
                if (wash != null && _washing.GetReagents(ent.Owner).Item2 < wash.CleanerRequired && cleaners.Count == 0)
                    break;

                RaiseLocalEvent(item, new GotCleanedEvent());

                // subtract cleaners if eligible
                if (wash != null && _washing.GetReagents(ent.Owner).Item2 >= wash.CleanerRequired)
                    _washing.ChemicalUseReagent(ent.Owner, true);

            }
        }

        private void OnDyed(Entity<DyeableComponent> ent, ref GotDyedEvent args)
        {
            // check for unique recipes first
            foreach (var (colorname, _) in ent.Comp.Recipes)
            {
                foreach (var dye in args.Dyes)
                {
                    if (dye.Comp.Color == colorname)
                    {
                        var spawned = Spawn(ent.Comp.Recipes[colorname], Transform(ent).Coordinates);
                        var dyed = EnsureComp<DyedComponent>(spawned);
                        dyed.CurrentColor = Color.White;
                        dyed.OriginalEntity = MetaData(ent.Owner).EntityPrototype?.ID;

                        Del(ent.Owner);
                        return;
                    }
                }
            }
            // not dying something that's been hit by unique recipe beam
            if (ent.Comp.AcceptAnyColor)
            {
                var colorString = "";
                var dyeTally = 0;
                var dyes = new List<DyeComponent>();
                foreach (var dye in args.Dyes)
                {
                    colorString = dye.Comp.Color;
                    dyes.Add(dye);
                    dyeTally += 1;
                }
                if (!Color.TryFromName(colorString, out var color))
                    color = Color.Transparent;
                if (dyeTally > 1)

                    color = MixColors(dyes, dyeTally);

                EnsureComp<DyedComponent>(ent, out var dyed);
                dyed.CurrentColor = color;
                Dirty(ent.Owner, dyed);
            }
        }

        private void OnCleaned(Entity<DyedComponent> ent, ref GotCleanedEvent _)
        {
            if (ent.Comp.OriginalEntity != null)
            {
                Spawn(ent.Comp.OriginalEntity, Transform(ent).Coordinates);
                Del(ent.Owner);
            }
            RemCompDeferred<DyedComponent>(ent);
            Dirty(ent);
        }

        private static Color MixColors(List<DyeComponent> contents, int count)
        {
            Vector4 hsl = new(0, 0, 0, 255);
            Vector4 outColor;
            foreach (var dye in contents)
            {
                if (!Color.TryFromName(dye.Color, out var color))
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
        private void OnGetState(Entity<DyedComponent> ent, ref ComponentGetState args)
        {
            args.State = new DyedComponentState()
            {
                CurrentColor = ent.Comp.CurrentColor,
            };
        }

        /// <summary>
        /// Raised by <see cref="DyeableSystem"/> to dye an item.
        /// </summary>
        public record struct GotDyedEvent(List<Entity<DyeComponent>> Dyes);

        /// <summary>
        /// Raised by <see cref="DyeableSystem"/> to clean a dyed item.
        /// </summary>
        public record struct GotCleanedEvent;
    }
}
