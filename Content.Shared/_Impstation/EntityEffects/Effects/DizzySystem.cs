using Content.Shared.Drunk;
using Content.Shared.Movement.Systems;

namespace Content.Shared._Impstation.EntityEffects.Effects;

public sealed class DizzySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDrunkSystem _drunkSystem = default!;
    [Dependency] private readonly SharedContentEyeSystem _eyeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DizzyComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DizzyComponent, ComponentShutdown>(OnShutdown);
    }

    public void MakeDizzy(EntityUid uid, float length)
    {
        var dizzy = EnsureComp<DizzyComponent>(uid);
        dizzy.TimeRemaining = length;
        dizzy.Dizzy = true;

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            UpdateAppearance(uid, dizzy, appearance);
    }
    private void OnStartup(EntityUid uid, DizzyComponent dizzy, ref ComponentStartup args)
    {
        if (dizzy.Dizzy)
            return;
        dizzy.Dizzy = true;
        if (TryComp<AppearanceComponent>(uid, out var appearance))
            UpdateAppearance(uid, dizzy, appearance);
    }

    public void UpdateAppearance(EntityUid uid, DizzyComponent? dizzy = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref dizzy, ref appearance))
            return;
        _appearance.SetData(uid, DizzyVisuals.Stars, dizzy.Dizzy, appearance);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DizzyComponent>();
        while (query.MoveNext(out var uid, out var dizzy))
        {
            dizzy.TimeRemaining -= frameTime;

            if (dizzy.TimeRemaining > 0)
            {
                // Multiplying by 2 is arbitrary but works for this case, it just prevents the time from running out
                _drunkSystem.TryApplyDrunkenness(uid, (float)dizzy.UpdateInterval.TotalSeconds * 2, applySlur: false);
                // storing the drunk time so we can remove it independently from other effects additions
                dizzy.StatusTime += dizzy.UpdateInterval * 2;
                continue;
            }

            // after time runs out
            RemCompDeferred<DizzyComponent>(uid);
        }
    }

    private void OnShutdown(EntityUid uid, DizzyComponent dizzy, ref ComponentShutdown args)
    {
        _drunkSystem.TryRemoveDrunkenessTime(uid, dizzy.StatusTime.TotalSeconds);
        dizzy.StatusTime = TimeSpan.Zero;

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            UpdateAppearance(uid, dizzy, appearance);
    }
}
