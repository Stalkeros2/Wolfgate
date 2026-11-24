using Robust.Shared.GameStates;

namespace Content.Shared._WG.LockOn;

/// <summary>
/// One firing solution per ship, shared by every helm and gunnery console aboard. Any console can set or clear it;
/// missiles fired by any of them seek the locked target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WGLockOnComponent : Component
{
    /// <summary>The locked entity, or its grid when the click landed on a ship, asteroid or debris grid.</summary>
    [AutoNetworkedField, ViewVariables]
    public EntityUid? Target;

    /// <summary>Furthest distance from the ship's centre at which a lock is held, in metres.</summary>
    [DataField]
    public float MaxRange = 2560f;

    /// <summary>The console that acquired the current lock.</summary>
    [ViewVariables]
    public EntityUid? LockedBy;

    /// <summary>The player who acquired the current lock.</summary>
    [ViewVariables]
    public EntityUid? Operator;

    /// <summary>When the current target was acquired.</summary>
    [ViewVariables]
    public TimeSpan LockedAt;

    /// <summary>
    /// Earliest periodic validity check. A lock is held between checks without replicating anything.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextCheck;
}