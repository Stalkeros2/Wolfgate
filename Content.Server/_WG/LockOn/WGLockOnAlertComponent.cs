namespace Content.Server._WG.LockOn;

/// <summary>Rate-limits lock warnings on a ship that is being locked, so a gunner sweeping targets can't spam it.</summary>
[RegisterComponent]
public sealed partial class WGLockOnAlertComponent : Component
{
    /// <summary>Earliest time the next warning may be announced.</summary>
    public TimeSpan NextAlert;
}