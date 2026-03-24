using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.DamageOverlays;

[UsedImplicitly]
public sealed class DamageOverlayUiController : UIController
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    [UISystemDependency] private readonly DamageableSystem _damageable = default!;
    [UISystemDependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [UISystemDependency] private readonly StatusEffectsSystem _statusEffects = default!;

    private DamageOverlay _overlay = default!;

    // TODO: unfortunately i cant find any feasible way to un-hardcode damage overlays. if someone wants to give it a shot, good fucking luck
    private readonly string _critShader = "CritShader";
    private readonly string _bruteShader = "BruteShader";
    private readonly string _oxygenShader = "OxygenShader";

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new DamageOverlay();
        _overlay.CacheOverlays();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MobThresholdChecked>(OnThresholdCheck);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<DamageOverlayPrototype>())
        {
            _overlay.CacheOverlays();
        }
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent args)
    {
        ClearOverlay();

        // in the case that a player is attaching to a dead entity (usually by warping to their body),
        // we don't really need to apply any overlays to them.
        if (!EntityManager.TryGetComponent<MobStateComponent>(args.Entity, out var mobState))
            return;

        if (mobState.CurrentState != MobState.Dead)
            UpdateOverlays(args.Entity, mobState);

        _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
        ClearOverlay();
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.Target != _playerManager.LocalEntity)
            return;

        UpdateOverlays(args.Target, args.Component);
    }

    private void OnThresholdCheck(ref MobThresholdChecked args)
    {
        if (args.Target != _playerManager.LocalEntity)
            return;

        UpdateOverlays(
            args.Target,
            args.MobState,
            args.Damageable,
            args.Threshold);
    }

    /// <summary>
    ///     Updates the damage overlays shown for the client of a specific entity,
    ///     based on the damage that entity has sustained.
    /// </summary>
    /// <remarks>
    ///     This is horribly hardcoded.
    ///     Unfortunately there's not really a feasible way to unhardcode it.
    /// </remarks>
    private void UpdateOverlays(
        EntityUid entity,
        MobStateComponent? mobState,
        DamageableComponent? damageable = null,
        MobThresholdsComponent? thresholds = null)
    {
        ClearOverlay();

        if (mobState == null && !EntityManager.TryGetComponent(entity, out mobState) ||
        thresholds == null && !EntityManager.TryGetComponent(entity, out thresholds) ||
        damageable == null && !EntityManager.TryGetComponent(entity, out damageable))
            return;

        // this entity cannot die or crit
        if (!_mobThresholdSystem.TryGetIncapThreshold(entity, out var foundThreshold, thresholds))
            return;

        // this entity intentionally has no overlays
        if (!thresholds.ShowOverlays)
            return;

        // The logic for shaders here is important:
        // If a mob is in crit, they should ONLY have the crit shader.
        // If they are dead, they should have NO shader.
        // Otherwise, shaders are applied reliant on current damage sustained per group.
        switch (mobState.CurrentState)
        {
            // any shader
            case MobState.Alive:
                {
                    HandleAliveShaders(entity, damageable, foundThreshold.Value);
                    break;
                }

            // crit shader only
            case MobState.Critical:
                {
                    var damage = _damageable.GetTotalDamage((entity, damageable));

                    if (!_mobThresholdSystem.TryGetDeadPercentage
                        (entity, FixedPoint2.Max(0.0, damage), out var critLevel))
                    {
                        return;
                    }

                    SetOverlayIntensity(_critShader, critLevel.Value.Float());
                    break;
                }

            // no shader
            case MobState.Dead:
                break;
        }
    }

    private void HandleAliveShaders(
        EntityUid entity,
        DamageableComponent damageable,
        FixedPoint2 critThreshold)
    {
        var damagePerGroup = _damageable.GetDamagePerGroup((entity, damageable));
        var damagePerType = _damageable.GetDamages(damagePerGroup, damageable);

        foreach (var shader in _overlay.OverlayCache.Keys)
        {
            if (shader.HideOnPainNumbness &&
                _statusEffects.TryEffectsWithComp<PainNumbnessStatusEffectComponent>(entity, out _))
            {
                break;
            }

            FixedPoint2 totalDamage = 0;

            foreach (var type in shader.DamageTypes)
            {
                if (damagePerType.TryGetValue(type, out var damage))
                {
                    totalDamage += damage;
                }
            }

            var intensity = FixedPoint2.Min(1f, totalDamage / critThreshold).Float();
            SetOverlayIntensity(shader, intensity);
        }
    }

    #region Helpers

    /// <summary>
    ///     Iterates over each cached overlay and resets the intensity of the shaders.
    /// </summary>
    private void ClearOverlay()
    {
        foreach (var shader in _overlay.OverlayCache.Keys)
        {
            _overlay.ShaderIntensity[shader] = 0f;
        }
    }

    /// <summary>
    ///     Modifies the intensity of the shader registered to the corresponding <see cref="DamageOverlayPrototype"/>.
    /// </summary>
    /// <param name="proto">The <see cref="DamageOverlayPrototype"/> to modify the intensity of.</param>
    private void SetOverlayIntensity(DamageOverlayPrototype proto, float intensity)
    {
        _overlay.ShaderIntensity[proto] = intensity;
    }

    /// <summary>
    ///     Modifies the intensity of the shader registered to the corresponding <see cref="DamageOverlayPrototype"/>.
    /// </summary>
    /// <param name="protoDtring">The <see cref="DamageOverlayPrototype"/> to modify the intensity of, as a string.</param>

    private void SetOverlayIntensity(string protoString, float intensity)
    {
        if (!_protoMan.TryIndex<DamageOverlayPrototype>(protoString, out var proto))
            return;

        SetOverlayIntensity(proto, intensity);
    }

    #endregion
}
