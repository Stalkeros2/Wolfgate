using System.Numerics;
using Content.Client.Shuttles.UI;
using Content.Shared._WG.LockOn;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._WG.LockOn;

/// <summary>
/// Draws the ship's lock bracket and target name over a radar plot.
/// </summary>
public sealed class WGLockOnOverlay : Control
{
    private static readonly Color LockColor = Color.FromHex("#ff4a4a");

    private const float BracketRadius = 16f;
    private const float BracketArm = 7f;
    private const float RingRadius = 8f;
    private const float RingArm = 6f;

    private readonly EntityUid _console;
    private readonly ShuttleNavControl _radar;
    private readonly IEntityManager _entities;
    private readonly SharedTransformSystem _transform;
    private readonly IGameTiming _timing;
    private readonly Font _font;

    public WGLockOnOverlay(EntityUid console, ShuttleNavControl radar)
    {
        _console = console;
        _radar = radar;
        _entities = IoCManager.Resolve<IEntityManager>();
        _transform = _entities.System<SharedTransformSystem>();
        _timing = IoCManager.Resolve<IGameTiming>();
        _font = new VectorFont(
            IoCManager.Resolve<IResourceCache>().GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 12);
        MouseFilter = MouseFilterMode.Ignore;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!_entities.TryGetComponent(_console, out TransformComponent? consoleXform) ||
            consoleXform.GridUid is not { } ship ||
            !_entities.TryGetComponent(ship, out WGLockOnComponent? lockOn) ||
            lockOn.Target is not { } target ||
            !_entities.EntityExists(target) ||
            !_radar.WgLockOnTryGetWorldToView(out var worldToView, out var _))
            return;

        TransformComponent? targetXform = null;
        _entities.TryGetComponent(target, out targetXform);
        if (targetXform is null || targetXform.MapID == MapId.Nullspace)
            return;

        var targetPos = _transform.GetWorldPosition(targetXform);
        var targetView = Vector2.Transform(targetPos, worldToView);

        // Ring bracket around the target position — shows which ship/grid is locked.
        DrawRingBracket(handle, targetView);

        // Target name above the indicator.
        var targetName = _entities.GetComponent<MetaDataComponent>(target).EntityName;
        var lineHeight = _font.GetLineHeight(1);
        handle.DrawString(_font, targetView + new Vector2(0f, -BracketRadius - lineHeight / 2f), targetName, LockColor);
    }

    /// <summary>
    /// Draws a ring bracket around the target position on the radar.
    /// </summary>
    private static void DrawRingBracket(DrawingHandleScreen handle, Vector2 point)
    {
        handle.DrawCircle(point, RingRadius, LockColor, false);
        handle.DrawLine(new Vector2(point.X - RingArm, point.Y - RingArm), new Vector2(point.X - RingRadius, point.Y - RingRadius), LockColor);
        handle.DrawLine(new Vector2(point.X + RingArm, point.Y - RingArm), new Vector2(point.X + RingRadius, point.Y - RingRadius), LockColor);
        handle.DrawLine(new Vector2(point.X - RingArm, point.Y + RingArm), new Vector2(point.X - RingRadius, point.Y + RingRadius), LockColor);
        handle.DrawLine(new Vector2(point.X + RingArm, point.Y + RingArm), new Vector2(point.X + RingRadius, point.Y + RingRadius), LockColor);
    }
}
