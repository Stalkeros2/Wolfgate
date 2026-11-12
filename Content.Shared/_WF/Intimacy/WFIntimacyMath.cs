using Content.Shared._WF.Genitals;
using Content.Shared._WF.Intimacy.Prototypes;

namespace Content.Shared._WF.Intimacy;

/// <summary>Closed-form intimacy maths, kept pure so it can be tested without a server.</summary>
public static class WFIntimacyMath
{
    /// <summary>Top of the pleasure and lubrication scales.</summary>
    public const byte MaxValue = 100;

    /// <summary>Interval multiplier at the slow cadence.</summary>
    public const float SlowSpeedFactor = 1.35f;

    /// <summary>Interval multiplier at the fast cadence.</summary>
    public const float FastSpeedFactor = 0.75f;

    /// <summary>Arousal raises pleasure: 1 at zero arousal, 2 at the maximum.</summary>
    public static float ArousalFactor(byte arousal)
    {
        return 1f + arousal / (float) MaxValue;
    }

    /// <summary>Lubrication scales a gain: 1 dry, 1 + weight at the maximum.</summary>
    public static float LubricationFactor(byte wetness, float weight)
    {
        return 1f + weight * wetness / MaxValue;
    }

    /// <summary>Below the threshold a groin act gains less.</summary>
    public static float DryFactor(byte wetness, byte threshold, float penalty)
    {
        return wetness < threshold ? penalty : 1f;
    }

    /// <summary>Reduced gains for a while after an orgasm.</summary>
    public static float AfterglowFactor(bool inAfterglow, float multiplier)
    {
        return inAfterglow ? multiplier : 1f;
    }

    /// <summary>Pleasure one motion adds to the receiver.</summary>
    public static float PulsePleasure(
        IntimacyActPrototype act, byte arousal, byte wetness, IntimacySettingsPrototype settings, bool inAfterglow)
    {
        var gain = act.PleasureGain
                   * ArousalFactor(arousal)
                   * LubricationFactor(wetness, settings.WetnessPleasureWeight)
                   * AfterglowFactor(inAfterglow, settings.AfterglowMultiplier);

        // Only an act that expects a bare region can be dry; a clothed act has nothing to lubricate.
        if (act.ReceiverRegion != GenitalRegion.None)
            gain *= DryFactor(wetness, settings.DryThreshold, settings.DryPenalty);

        return gain;
    }

    /// <summary>Arousal one motion adds to the receiver.</summary>
    public static float PulseArousal(
        IntimacyActPrototype act, byte wetness, IntimacySettingsPrototype settings, bool inAfterglow)
    {
        return act.ArousalGain
               * LubricationFactor(wetness, settings.WetnessArousalWeight)
               * AfterglowFactor(inAfterglow, settings.AfterglowMultiplier);
    }

    /// <summary>Arousal one motion adds to the giver: taking part counts, at the act's share.</summary>
    public static float PulseGiverArousal(
        IntimacyActPrototype act, byte giverWetness, IntimacySettingsPrototype settings, bool inAfterglow)
    {
        return PulseArousal(act, giverWetness, settings, inAfterglow) * act.GiverShare;
    }

    /// <summary>Adds a delta to a 0-100 value, saturating at both ends. A non-finite delta changes nothing.</summary>
    public static byte Add(byte value, float delta)
    {
        var sum = value + delta;
        if (!float.IsFinite(sum))
            return value;

        return (byte) Math.Clamp(sum, 0f, MaxValue);
    }

    /// <summary>Bleeds a 0-100 value down by a rate over a time step; never below zero.</summary>
    public static byte Decay(byte value, float perSecond, float seconds)
    {
        var loss = perSecond * seconds;
        if (!float.IsFinite(loss) || loss <= 0f)
            return value;

        return (byte) Math.Max(0f, value - loss);
    }

    /// <summary>True at the maximum pleasure: the body orgasms.</summary>
    public static bool ReachesOrgasm(byte pleasure)
    {
        return pleasure >= MaxValue;
    }

    /// <summary>Seconds between two motions of an act at a speed; never shorter than a tenth of a second.</summary>
    public static float Interval(IntimacyActPrototype act, WFIntimacySpeed speed)
    {
        var factor = speed switch
        {
            WFIntimacySpeed.Slow => SlowSpeedFactor,
            WFIntimacySpeed.Fast => FastSpeedFactor,
            _ => 1f,
        };

        return MathF.Max(0.1f, act.PulseInterval * factor);
    }
}