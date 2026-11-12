using Content.Server._WF.LockOn;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._Mono.FireControl;

public sealed partial class FireControlSystem
{
    [Dependency] private WFLockOnSystem _wfLockOn = default!;

    /// <summary>Records the ship's lock on the weapons a console is about to fire.</summary>
    internal void WfLockOnMarkFire(EntityUid console, List<NetEntity>? selected) =>
        _wfLockOn.MarkFire(console, selected);

    /// <summary>Points missiles a locked weapon just fired at the ship's locked target.</summary>
    internal void WfLockOnApply(EntityUid weapon, AmmoShotEvent args) => _wfLockOn.Apply(weapon, args);

    /// <summary>The locked target's live position for a cursor-guided missile, if the ship has a lock.</summary>
    internal EntityCoordinates? WfLockOnGuidedTarget(EntityUid console) => _wfLockOn.GuidedTarget(console);
}
