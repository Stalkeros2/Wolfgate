using System.Numerics;
using Content.Shared._WF.ShipMissiles;
using NUnit.Framework;

namespace Content.Tests._WF.ShipMissiles;

/// <summary>The proportional-navigation maths: every case is a closed-form geometry the flight model relies on.</summary>
[TestFixture]
[TestOf(typeof(MissileGuidance))]
public sealed class MissileGuidanceTest
{
    /// <summary>A missile 200 m/s down +x and a target 100 m ahead moving 10 m/s across it.</summary>
    private static readonly Vector2 Crossing = new Vector2(0f, 10f) - new Vector2(200f, 0f);

    /// <summary>Dead ahead at 200 m/s: the line of sight does not move, so neither should the missile.</summary>
    [Test]
    public void LevelsOffOnACollisionCourse()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MissileGuidance.LosRate(new Vector2(100f, 0f), new Vector2(-200f, 0f)), Is.EqualTo(0f).Within(1e-6f));
            Assert.That(MissileGuidance.ProportionalNavigation(0f, 200f, 4f, 110f), Is.EqualTo(0f).Within(1e-6f));
        });
    }

    /// <summary>A crossing target turns the line of sight, and the command leads the way it is turning.</summary>
    [Test]
    public void LeadsACrossingTarget()
    {
        var relPos = new Vector2(100f, 0f);

        Assert.Multiple(() =>
        {
            Assert.That(MissileGuidance.LosRate(relPos, Crossing), Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(MissileGuidance.ClosingRate(relPos, Crossing), Is.EqualTo(200f).Within(1e-4f));
            // N * Vc * losRate = 4 * 200 * 0.1, well inside the authority.
            Assert.That(MissileGuidance.ProportionalNavigation(0.1f, 200f, 4f, 500f), Is.EqualTo(80f).Within(1e-4f));
            // The target drifts to +y, so the normal and the turn both point at +y.
            var normal = MissileGuidance.LosNormal(relPos);
            Assert.That(normal.X, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(normal.Y, Is.EqualTo(1f).Within(1e-6f));
        });
    }

    /// <summary>Closure is positive head-on, negative while opening and zero on a pure beam.</summary>
    [Test]
    public void SignsTheClosingRate()
    {
        var relPos = new Vector2(100f, 0f);

        Assert.Multiple(() =>
        {
            Assert.That(MissileGuidance.ClosingRate(relPos, new Vector2(-50f, 0f)), Is.EqualTo(50f).Within(1e-4f));
            Assert.That(MissileGuidance.ClosingRate(relPos, new Vector2(50f, 0f)), Is.EqualTo(-50f).Within(1e-4f));
            Assert.That(MissileGuidance.ClosingRate(relPos, new Vector2(0f, 20f)), Is.EqualTo(0f).Within(1e-4f));
        });
    }

    /// <summary>The command never exceeds the diverters, in either direction.</summary>
    [Test]
    public void ClampsToTheDiverters()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MissileGuidance.ProportionalNavigation(1f, 200f, 4f, 110f), Is.EqualTo(110f));
            Assert.That(MissileGuidance.ProportionalNavigation(-1f, 200f, 4f, 110f), Is.EqualTo(-110f));
            // A non-finite command is dropped rather than flown.
            Assert.That(MissileGuidance.ProportionalNavigation(float.NaN, 200f, 4f, 110f), Is.EqualTo(0f));
        });
    }

    /// <summary>Beaming is fast and square to the seeker; head-on and slow flight are neither.</summary>
    [Test]
    public void FindsTheNotch()
    {
        var relPos = new Vector2(100f, 0f);
        const float band = 4f;

        Assert.Multiple(() =>
        {
            Assert.That(MissileGuidance.IsBeaming(relPos, new Vector2(0f, 20f), band), Is.True, "Beam is a notch.");
            Assert.That(MissileGuidance.IsBeaming(relPos, Crossing, band), Is.False, "Head-on is not a notch.");
            Assert.That(MissileGuidance.IsBeaming(relPos, new Vector2(0f, 5f), band), Is.False, "Too slow to notch.");
        });
    }

    /// <summary>Time to go counts down on closure and does not explode on a receding target.</summary>
    [Test]
    public void EstimatesTimeToGo()
    {
        var relPos = new Vector2(100f, 0f);

        Assert.Multiple(() =>
        {
            Assert.That(MissileGuidance.TimeToGo(relPos, new Vector2(-200f, 0f)), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(MissileGuidance.TimeToGo(relPos, new Vector2(100f, 0f)), Is.EqualTo(100f).Within(1e-3f));
        });
    }
}
