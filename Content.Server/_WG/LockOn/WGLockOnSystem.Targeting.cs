using System.Numerics;
using Content.Shared.Ghost;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server._WG.LockOn;

public sealed partial class WGLockOnSystem
{
    /// <summary>Squared distance within which a free entity counts as being on the clicked tile.</summary>
    private const float FreeEntityTileToleranceSq = 4f;

    /// <summary>Widest cursor lens honoured, in metres, so a zoomed-out radar cannot lock across the plot.</summary>
    private const float MaxPickRadius = 256f;

    /// <summary>
    /// Finds what a radar click landed on. The pick fills the radar's cursor lens: something loose or a hull right
    /// under the cursor wins, and otherwise the contact nearest the lens centre does, so a click into a crowd locks
    /// the closest contact instead of whatever single tile it landed on. The ship's own grid and anything aboard it
    /// are never valid targets.
    /// </summary>
    public EntityUid? ResolveTarget(EntityUid console, EntityUid ship, MapCoordinates point, float radius = 0f)
    {
        if (point.MapId == MapId.Nullspace ||
            !float.IsFinite(point.Position.X) ||
            !float.IsFinite(point.Position.Y))
            return null;

        // A lens narrower than a tile would aim worse than the old click box; a wide one must not reach across the plot.
        var reach = Math.Clamp(radius > 0f ? radius : ClickRadius, ClickRadius, MaxPickRadius);
        var half = new Vector2(reach, reach);
        var box = new Box2(point.Position - half, point.Position + half);
        var found = new HashSet<EntityUid>();
        _lookup.GetEntitiesIntersecting(point.MapId, box, found,
            LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries);

        EntityUid? underCursor = null;
        var underCursorDistance = MathF.Sqrt(FreeEntityTileToleranceSq);
        EntityUid? nearest = null;
        var nearestDistance = float.MaxValue;
        var grids = new HashSet<EntityUid>();
        var loose = new List<(EntityUid Uid, Vector2 Position)>();

        foreach (var uid in found)
        {
            if (uid == console ||
                TerminatingOrDeleted(uid) ||
                !EntityManager.EntityExists(uid) ||
                HasComp<MapComponent>(uid) ||
                HasComp<GhostComponent>(uid))
                continue;

            if (!TryComp<TransformComponent>(uid, out var xform) || xform.MapID != point.MapId)
                continue;

            // Everything aboard the ship doing the locking is off limits, including its own hull.
            if (xform.GridUid == ship)
                continue;

            if (xform.GridUid is { } grid)
            {
                grids.Add(grid);
                continue;
            }

            var position = _transform.GetWorldPosition(xform);
            var distance = Vector2.Distance(position, point.Position);
            if (distance > reach)
                continue;

            if (distance < underCursorDistance)
            {
                underCursor = uid;
                underCursorDistance = distance;
            }

            loose.Add((uid, position));
        }

        // Something loose right under the cursor wins, as it did before the lens: rocks and debris lock as themselves.
        if (underCursor is { } onTile)
            return onTile;

        // A hull the click lands on need not come back from the lookup as an entity of its own.
        if (_map.TryFindGridAt(point, out var atPoint, out _) && atPoint != ship && !TerminatingOrDeleted(atPoint))
            grids.Add(atPoint);

        foreach (var grid in grids)
        {
            if (grid == ship || TerminatingOrDeleted(grid) || !EntityManager.EntityExists(grid))
                continue;

            var distance = ContactDistance(grid, point.Position);
            if (distance > reach || distance >= nearestDistance)
                continue;

            nearest = grid;
            nearestDistance = distance;
        }

        foreach (var (uid, position) in loose)
        {
            var distance = Vector2.Distance(position, point.Position);
            if (distance >= nearestDistance)
                continue;

            nearest = uid;
            nearestDistance = distance;
        }

        return nearest;
    }

    /// <summary>How far a contact lies from a point: nothing when the point is on its hull, else from its centre.</summary>
    private float ContactDistance(EntityUid uid, Vector2 point)
    {
        if (!HasComp<TransformComponent>(uid))
            return float.MaxValue;

        if (TryComp<MapGridComponent>(uid, out var grid))
        {
            var matrix = _transform.GetWorldMatrix(uid);
            if (Matrix3x2.Invert(matrix, out var inverse) &&
                grid.LocalAABB.Contains(Vector2.Transform(point, inverse)))
                return 0f;
        }

        var centre = CompOrNull<PhysicsComponent>(uid)?.LocalCenter ?? Vector2.Zero;
        return Vector2.Distance(Vector2.Transform(centre, _transform.GetWorldMatrix(uid)), point);
    }
}
