using Content.Shared._WG.ShipMissiles;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._WG.ShipMissiles;

/// <summary>Captures a snapshot of a fire-control-designated target for smart-missile descriptor acquisition.</summary>
public static class TargetDescriptorCapture
{
    public static void FromEntity(IEntityManager entities, SharedPhysicsSystem physics,
        WGMissileFlightComponent flight, EntityUid target)
    {
        flight.DescriptorCaptured = true;

        if (!entities.TryGetComponent(target, out TransformComponent? xform))
            return;

        flight.DescriptorPosition = xform.MapPosition.Position;
        flight.DescriptorVelocity = physics.GetMapLinearVelocity(target, entities.GetComponent<PhysicsComponent>(target), xform);
        flight.DescriptorGrid = xform.GridUid;

        // Approximate radius from the grid's AABB if on a grid, else a small default.
        if (xform.GridUid is { } grid && entities.TryGetComponent(grid, out MapGridComponent? gridComp))
        {
            var size = gridComp.LocalAABB.Size;
            flight.DescriptorRadius = MathF.Max(size.X, size.Y) * 0.5f;
        }
        else
        {
            flight.DescriptorRadius = 8f;
        }
    }
}