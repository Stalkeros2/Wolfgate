using System.Numerics;

namespace Content.Shared._WF.ShipMissiles;

/// <summary>Closed-form guidance maths for ship missiles, kept pure so it can be tested without a server.</summary>
public static class MissileGuidance
{
    /// <summary>Line-of-sight rotation rate in radians per second, positive anticlockwise.</summary>
    public static float LosRate(Vector2 relPos, Vector2 relVel)
    {
        var distanceSquared = relPos.LengthSquared();
        if (distanceSquared <= 0f)
            return 0f;

        return (relPos.X * relVel.Y - relPos.Y * relVel.X) / distanceSquared;
    }

    /// <summary>Rate the range is shrinking at, in metres per second. Negative while the target opens.</summary>
    public static float ClosingRate(Vector2 relPos, Vector2 relVel)
    {
        var distance = relPos.Length();
        if (distance <= 0f)
            return 0f;

        return -Vector2.Dot(relPos, relVel) / distance;
    }

    /// <summary>Unit vector across the line of sight, pointing the way the line of sight is turning.</summary>
    public static Vector2 LosNormal(Vector2 relPos)
    {
        var distance = relPos.Length();
        if (distance <= 0f)
            return Vector2.Zero;

        return new Vector2(-relPos.Y / distance, relPos.X / distance);
    }

    /// <summary>Proportional-navigation lateral command in m/s², clamped to <paramref name="authority"/>.</summary>
    public static float ProportionalNavigation(float losRate, float closingRate, float navigationConstant, float authority)
    {
        var command = navigationConstant * closingRate * losRate;
        if (!float.IsFinite(command))
            return 0f;

        return Math.Clamp(command, -authority, authority);
    }

    /// <summary>
    /// True while the target crosses the seeker squarely and quickly: the closure rate is near zero while the
    /// relative velocity is not, which is the geometry that leaves a proportional-navigation missile nothing to steer on.
    /// </summary>
    public static bool IsBeaming(Vector2 relPos, Vector2 relVel, float band)
    {
        return relVel.LengthSquared() > 4f * band * band
               && Math.Abs(ClosingRate(relPos, relVel)) < band;
    }

    /// <summary>Seconds until the range closes, counting an opening target as <paramref name="minimumClosing"/>.</summary>
    public static float TimeToGo(Vector2 relPos, Vector2 relVel, float minimumClosing = 1f)
    {
        return relPos.Length() / MathF.Max(ClosingRate(relPos, relVel), minimumClosing);
    }
}
