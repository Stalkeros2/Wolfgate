using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;

namespace Content.Server._Mono.FireControl;

public sealed partial class FireControlSystem
{
    /// <summary>
    /// Points the projectiles a locked weapon just fired at the ship's locked target.
    /// </summary>
    private void WgLockOnApply(EntityUid uid, AmmoShotEvent args)
    {
        _wgLockOn.Apply(uid, args);
    }

    /// <summary>
    /// The locked target's live position for a cursor-guided missile fired by this console's ship.
    /// </summary>
    private EntityCoordinates? WgLockOnGuidedTarget(EntityUid consoleUid)
    {
        return _wgLockOn.GuidedTarget(consoleUid);
    }

    /// <summary>
    /// Records the ship's lock on each weapon a console is about to fire, so their projectiles can be
    /// pointed at the locked target before their first guidance tick.
    /// </summary>
    private void WgLockOnMarkFire(EntityUid console, List<NetEntity>? selected)
    {
        _wgLockOn.MarkFire(console, selected);
    }
}