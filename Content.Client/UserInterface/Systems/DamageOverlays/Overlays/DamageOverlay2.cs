using System.Numerics;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.DamageOverlays;

public sealed class DamageOverlay2 : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    /// <summary>
    ///     Cache of the overlay prototypes we've loaded and their corresponding active shader instances.
    /// </summary>
    public Dictionary<DamageOverlayPrototype, ShaderInstance> OverlayCache = [];

    /// <summary>
    ///     Dictionary of overlays with the current intensity of each overlay.
    ///     Modify this to change the intensity of the shader.
    /// </summary>
    public Dictionary<DamageOverlayPrototype, float> ShaderIntensity = [];

    /// <summary>
    ///     Dictionary of overlays with the current intensity of each overlay on the PREVIOUS frame.
    ///     We interpolate between our two dictionaries to create smooth transitions.
    /// </summary>
    private Dictionary<DamageOverlayPrototype, float> _lastShaderIntensity = [];

    // <summary>
    ///     Reinitializes the cached dictionary of damage overlay prototypes and shaders.
    /// </summary>
    public void CacheOverlays()
    {
        OverlayCache.Clear();
        OverlayCache = GetDamageOverlays();
    }

    /// <summary>
    ///     Iterates through all DamageOverlayPrototypes using IProtoManager
    ///     and creates unique shader instances for them.
    /// </summary>
    /// <returns>A dictionary of unique shader instances for each prototype.</returns>
    public Dictionary<DamageOverlayPrototype, ShaderInstance> GetDamageOverlays()
    {
        IoCManager.InjectDependencies(this);
        Dictionary<DamageOverlayPrototype, ShaderInstance> overlays = [];

        foreach (var proto in _protoMan.EnumeratePrototypes<DamageOverlayPrototype>())
        {
            var shader = _protoMan.Index(proto.ShaderMask).InstanceUnique();
            overlays.Add(proto, shader);
        }

        return overlays;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return;

        if (args.Viewport.Eye != eyeComp.Eye)
            return;

        var viewport = args.WorldAABB;
        var handle = args.WorldHandle;
        var distance = args.ViewportBounds.Width;

        var time = (float)_timing.RealTime.TotalSeconds;
        var lastFrameTime = (float)_timing.FrameTime.TotalSeconds;

        // shaders should smoothly transition between states.
        // we can use lerping (linear interpolation) to achieve this.
        foreach (var overlay in OverlayCache)
        {
            var proto = overlay.Key;

            _lastShaderIntensity[proto] = UpdateIntensity(proto, lastFrameTime);

            var adjustedTime = time * proto.PulseRate;
            var pulse = MathF.Max(0f, MathF.Sin(adjustedTime));

            var outerMax = proto.OuterMaxLevel * distance;
            var outerMin = proto.OuterMinLevel * distance;
            var innerMax = proto.InnerMaxLevel * distance;
            var innerMin = proto.InnerMinLevel * distance;

            var level = _lastShaderIntensity[proto];
            var outerRadius = outerMax - level * (outerMax - outerMin);
            var innerRadius = innerMax - level * (innerMax - innerMin);

            var shader = overlay.Value;

            shader.SetParameter("time", pulse);
            shader.SetParameter("color", proto.Color);

            // darknessAlphaOuter is the maximum alpha for anything outside of the larger circle
            // darknessAlphaInner (on the shader) is the alpha for anything inside the smallest circle
            shader.SetParameter("darknessAlphaOuter", proto.DarknessAlphaOuter);

            // outerCircleRadius is what we end at for max level for the outer circle
            shader.SetParameter("outerCircleRadius", outerRadius);
            // outerCircleMaxRadius is what we start at for 0 level for the outer circle
            shader.SetParameter("outerCircleMaxRadius", outerRadius * distance);

            // innerCircleRadius is what we end at for max level for the inner circle
            shader.SetParameter("innerCircleRadius", innerRadius);
            // innerCircleMaxRadius is what we start at for 0 level for the inner circle
            shader.SetParameter("innerCircleMaxRadius", innerRadius * distance);

            handle.UseShader(shader);
            handle.DrawRect(viewport, Color.White);
        }

        handle.UseShader(null);
    }

    // TODO comments that explain what these do. theyre copied from old code, and i dont fully understand them

    private float UpdateIntensity(DamageOverlayPrototype proto, float lastFrameTime)
    {
        var intensity = ShaderIntensity[proto];
        var lastIntensity = _lastShaderIntensity[proto];

        if (!MathHelper.CloseTo(lastIntensity, intensity, 0.001f))
        {
            var diff = intensity - lastIntensity;
            lastIntensity += GetDiff(diff, lastFrameTime);

            return lastIntensity;
        }

        return intensity;
    }

    private float GetDiff(float value, float lastFrameTime)
    {
        var adjustment = value * 5f * lastFrameTime;

        if (value < 0f)
            adjustment = Math.Clamp(adjustment, value, -value);
        else
            adjustment = Math.Clamp(adjustment, -value, value);

        return adjustment;
    }
}
