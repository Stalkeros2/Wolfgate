using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._WG.LockOn;

/// <summary>
/// Asks the console's ship to lock whatever sits under a radar click, or to clear the lock when the click found
/// nothing. Sent by the helm and gunnery consoles alike.
/// </summary>
[Serializable, NetSerializable]
public sealed class WGLockOnRequestMessage(NetCoordinates coordinates, float radius = 0f) : BoundUserInterfaceMessage
{
    /// <summary>The click, in the map the console's radar plotted.</summary>
    public NetCoordinates Coordinates = coordinates;

    /// <summary>
    /// Radius in metres of the radar's cursor lens, so the pick fills the circle the radar draws. Zero falls back to
    /// the server's own click radius.
    /// </summary>
    public float Radius = radius;
}