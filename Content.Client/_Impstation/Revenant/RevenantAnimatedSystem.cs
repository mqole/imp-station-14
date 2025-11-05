using Content.Shared._Impstation.Revenant;
using Content.Shared._Impstation.Revenant.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._Impstation.Revenant;

public sealed class RevenantAnimatedSystem : SharedRevenantAnimatedSystem
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantAnimatedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RevenantAnimatedComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<RevenantAnimatedComponent>();

        while (enumerator.MoveNext(out _, out var comp))
        {
            if (comp.LightOverlay == null ||
                TryComp<PointLightComponent>(comp.LightOverlay, out var light))
                continue;
            comp.Accumulator += frameTime;
            _lights.SetEnergy(comp.LightOverlay.Value, 2f * Math.Abs((float)Math.Sin(0.25 * Math.PI * comp.Accumulator)), light);
        }
    }

    private void OnStartup(Entity<RevenantAnimatedComponent> ent, ref ComponentStartup args)
    {
        var lightEnt = Spawn(null, new EntityCoordinates(ent, 0, 0));
        EnsureComp<PointLightComponent>(lightEnt, out var light);

        ent.Comp.LightOverlay = lightEnt;

        _lights.SetEnabled(lightEnt, true, light);
        _lights.SetColor(lightEnt, ent.Comp.LightColor, light);
        _lights.SetRadius(lightEnt, ent.Comp.LightRadius, light);
    }

    private void OnShutdown(EntityUid uid, RevenantAnimatedComponent comp, ComponentShutdown args)
    {
        if (comp.LightOverlay != null)
            Del(comp.LightOverlay);
    }
}
