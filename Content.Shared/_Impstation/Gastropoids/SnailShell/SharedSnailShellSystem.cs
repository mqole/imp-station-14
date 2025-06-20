namespace Content.Shared._Impstation.Gastropoids.SnailShell;

public abstract partial class SharedSnailShellSystem : EntitySystem { }

/// <summary>
/// Relayed by <see cref="SharedSnailShellSystem"/> to toggle shell sprites when using the snail shell
/// </summary>
[ByRefEvent]
public record struct ToggleShellSpriteEvent;
