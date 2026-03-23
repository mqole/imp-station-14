using System.Numerics;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.DamageOverlays.Overlays;

public sealed class DamageOverlay2 : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

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

        foreach (var proto in _protoMan.EnumeratePrototypes<DamageOverlayPrototype>())
        {
            var shader = _protoMan.Index(proto.ShaderMask).InstanceUnique();

            var adjustedTime = time * proto.PulseRate;
            var pulse = MathF.Max(0f, MathF.Sin(adjustedTime));

            shader.SetParameter("time", pulse);
            shader.SetParameter("color", proto.Color);

            handle.UseShader(shader);
            handle.DrawRect(viewport, Color.White);
        }

        handle.UseShader(null);
    }
}
