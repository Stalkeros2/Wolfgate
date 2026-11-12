using Content.Shared._WF.Genitals;
using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Prototypes;
using NUnit.Framework;

namespace Content.Tests._WF.Intimacy;

/// <summary>The pulse maths every act and the automatic cadence rely on.</summary>
[TestFixture]
[TestOf(typeof(WFIntimacyMath))]
public sealed class WFIntimacyMathTest
{
    private static readonly IntimacySettingsPrototype Settings = new();

    /// <summary>A bare groin act with round numbers, so each factor can be checked on its own.</summary>
    private static IntimacyActPrototype Bare()
    {
        return new IntimacyActPrototype
        {
            ArousalGain = 10f,
            PleasureGain = 10f,
            GiverShare = 0.5f,
            ReceiverRegion = GenitalRegion.Groin,
        };
    }

    /// <summary>Arousal doubles pleasure at the top of the scale and does nothing at the bottom.</summary>
    [Test]
    public void ArousalRaisesPleasure()
    {
        var act = Bare();

        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyMath.ArousalFactor(0), Is.EqualTo(1f));
            Assert.That(WFIntimacyMath.ArousalFactor(100), Is.EqualTo(2f));
            Assert.That(WFIntimacyMath.PulsePleasure(act, 100, 100, Settings, false),
                Is.EqualTo(2f * WFIntimacyMath.PulsePleasure(act, 0, 100, Settings, false)).Within(1e-4f),
                "Full arousal is worth exactly twice no arousal.");
        });
    }

    /// <summary>Lubrication raises arousal gains by its weight, and raises pleasure gains by a larger one.</summary>
    [Test]
    public void LubricationRaisesTheGains()
    {
        // Region None so the dry penalty cannot mask the lubrication factor.
        var clothed = new IntimacyActPrototype { ArousalGain = 10f, PleasureGain = 10f };

        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyMath.PulseArousal(clothed, 100, Settings, false)
                        / WFIntimacyMath.PulseArousal(clothed, 0, Settings, false),
                Is.EqualTo(1f + Settings.WetnessArousalWeight).Within(1e-4f), "Arousal uses the arousal weight.");

            Assert.That(WFIntimacyMath.PulsePleasure(clothed, 0, 100, Settings, false)
                        / WFIntimacyMath.PulsePleasure(clothed, 0, 0, Settings, false),
                Is.EqualTo(1f + Settings.WetnessPleasureWeight).Within(1e-4f), "Pleasure uses the pleasure weight.");
        });
    }

    /// <summary>A bare act is penalised while dry; a clothed act has nothing to be dry about.</summary>
    [Test]
    public void DryActsGainLess()
    {
        var bare = Bare();
        var clothed = new IntimacyActPrototype
        {
            ArousalGain = 10f,
            PleasureGain = 10f,
            ReceiverRegion = GenitalRegion.None,
        };

        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyMath.DryFactor(0, Settings.DryThreshold, Settings.DryPenalty),
                Is.EqualTo(Settings.DryPenalty), "Below the threshold is dry.");
            Assert.That(WFIntimacyMath.DryFactor(Settings.DryThreshold, Settings.DryThreshold, Settings.DryPenalty),
                Is.EqualTo(1f), "At the threshold is not dry.");
            Assert.That(WFIntimacyMath.PulsePleasure(clothed, 0, 0, Settings, false),
                Is.EqualTo(10f).Within(1e-4f), "A clothed act ignores the dry penalty.");
        });
    }

    /// <summary>Afterglow cuts both gains by the same multiplier.</summary>
    [Test]
    public void AfterglowCutsTheGains()
    {
        var act = Bare();
        var normal = WFIntimacyMath.PulsePleasure(act, 50, 50, Settings, false);
        var after = WFIntimacyMath.PulsePleasure(act, 50, 50, Settings, true);

        Assert.Multiple(() =>
        {
            Assert.That(after / normal, Is.EqualTo(Settings.AfterglowMultiplier).Within(1e-4f));
            Assert.That(WFIntimacyMath.PulseArousal(act, 50, Settings, true) / WFIntimacyMath.PulseArousal(act, 50, Settings, false),
                Is.EqualTo(Settings.AfterglowMultiplier).Within(1e-4f));
        });
    }

    /// <summary>The giver's participation is a share of the receiver's arousal gain.</summary>
    [Test]
    public void TheGiverGetsAShare()
    {
        var act = Bare();

        Assert.That(WFIntimacyMath.PulseGiverArousal(act, 40, Settings, false),
            Is.EqualTo(WFIntimacyMath.PulseArousal(act, 40, Settings, false) * act.GiverShare).Within(1e-5f));
    }

    /// <summary>Adds never leave the 0-100 scale, and a bad delta is dropped.</summary>
    [Test]
    public void AddSaturates()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyMath.Add(95, 10f), Is.EqualTo(100), "Clamped at the top.");
            Assert.That(WFIntimacyMath.Add(5, -10f), Is.EqualTo(0), "Clamped at the bottom.");
            Assert.That(WFIntimacyMath.Add(50, 12.6f), Is.EqualTo(63), "Rounded to the nearest whole value.");
            Assert.That(WFIntimacyMath.Add(50, float.NaN), Is.EqualTo(50), "A non-finite delta changes nothing.");
        });
    }

    /// <summary>Decay stops at zero and ignores a non-positive rate.</summary>
    [Test]
    public void DecayStopsAtZero()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyMath.Decay(10, 5f, 10f), Is.EqualTo(0));
            Assert.That(WFIntimacyMath.Decay(10, 0f, 10f), Is.EqualTo(10));
            Assert.That(WFIntimacyMath.Decay(10, -1f, 10f), Is.EqualTo(10));
            Assert.That(WFIntimacyMath.Decay(10, float.PositiveInfinity, 1f), Is.EqualTo(10), "A bad rate changes nothing.");
        });
    }

    /// <summary>Only the top of the scale is an orgasm.</summary>
    [Test]
    public void OrgasmNeedsTheMaximum()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyMath.ReachesOrgasm(99), Is.False);
            Assert.That(WFIntimacyMath.ReachesOrgasm(WFIntimacyMath.MaxValue), Is.True);
        });
    }

    /// <summary>The cadence scales with the chosen speed and never stalls on a zero interval.</summary>
    [Test]
    public void SpeedScalesTheInterval()
    {
        var act = new IntimacyActPrototype { PulseInterval = 2f };

        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyMath.Interval(act, WFIntimacySpeed.Slow),
                Is.EqualTo(2f * WFIntimacyMath.SlowSpeedFactor).Within(1e-4f));
            Assert.That(WFIntimacyMath.Interval(act, WFIntimacySpeed.Normal),
                Is.EqualTo(2f).Within(1e-4f));
            Assert.That(WFIntimacyMath.Interval(act, WFIntimacySpeed.Fast),
                Is.EqualTo(2f * WFIntimacyMath.FastSpeedFactor).Within(1e-4f));
            Assert.That(WFIntimacyMath.Interval(new IntimacyActPrototype { PulseInterval = 0f }, WFIntimacySpeed.Fast),
                Is.EqualTo(0.1f), "A zero interval still waits a tenth of a second.");
        });
    }
}