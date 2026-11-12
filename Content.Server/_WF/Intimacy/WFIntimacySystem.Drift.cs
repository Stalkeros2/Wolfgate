using Content.Shared._WF.Genitals;
using Content.Shared._WF.Genitals.Components;
using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Components;
using Content.Shared._WF.Intimacy.Prototypes;

namespace Content.Server._WF.Intimacy;

// The clock: decay outside a session, self-lubrication and auto-arousal inside one, and the automatic cadence.
public sealed partial class WFIntimacySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_genitals.AnatomyEnabled)
        {
            _accumulator = 0f;
            return;
        }

        _accumulator += frameTime;
        if (_accumulator < TickInterval)
            return;

        var dt = _accumulator;
        _accumulator = 0f;

        Drift(dt);
        Sessions();
    }

    /// <summary>Whole points only: fractional drift waits in the carry fields.</summary>
    private void Drift(float dt)
    {
        var settings = Settings;
        var query = EntityQueryEnumerator<GenitalsComponent>();
        while (query.MoveNext(out var uid, out var genitals))
        {
            // Every body with anatomy gets its intimacy state the first time the clock reaches it.
            var stats = EnsureComp<WFIntimacyComponent>(uid);

            if (_mobState.IsDead(uid))
            {
                stats.ArousalCarry = 0f;
                stats.WetnessCarry = 0f;
                stats.PleasureCarry = 0f;
                SetPleasure(uid, stats, 0, null);
                SetWetness(uid, stats, 0);
                continue;
            }

            var inSession = stats.Partner != null;

            // Pleasure bleeds away fast once nothing is happening.
            if (inSession)
            {
                stats.PleasureCarry = 0f;
            }
            else
            {
                stats.PleasureCarry += settings.PleasureDecayPerSecond * dt;
                var pleasureLost = Take(ref stats.PleasureCarry);
                if (pleasureLost > 0)
                    SetPleasure(uid, stats, WFIntimacyMath.Add(stats.Pleasure, -pleasureLost), null);
            }

            // Lubrication rises while aroused and busy, and dries out otherwise.
            var aroused = _arousal.GetState((uid, genitals)) != ArousalState.None;
            var wetRate = inSession && aroused ? settings.SelfLubricationPerSecond : -settings.WetnessDecayPerSecond;
            stats.WetnessCarry += wetRate * dt;
            var wetDelta = Take(ref stats.WetnessCarry);
            if (wetDelta != 0)
                SetWetness(uid, stats, WFIntimacyMath.Add(stats.Wetness, wetDelta));

            // Arousal: the auto-arousal toggle builds it, an idle body loses it.
            if (_genitals.Settings.AutoArousalConsent is { } autoToggle && _consent.HasStrictConsent(uid, autoToggle))
            {
                stats.ArousalCarry += settings.AutoArousalPerSecond * dt;
                var gained = Take(ref stats.ArousalCarry);
                if (gained > 0)
                    _arousal.TryAdjustArousal((uid, genitals), gained, autoToggle, null);
            }
            else if (!inSession)
            {
                stats.ArousalCarry += settings.ArousalDecayPerSecond * dt;
                var decayed = Take(ref stats.ArousalCarry);
                if (decayed > 0)
                    _arousal.DecayArousal((uid, genitals), decayed);
            }
        }
    }

    /// <summary>The giver drives: validity, the idle clock and the automatic cadence.</summary>
    private void Sessions()
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WFIntimacyComponent>();
        while (query.MoveNext(out var uid, out var stats))
        {
            if (stats.Partner is not { } partner || !stats.Thrusting)
                continue;

            if (SessionFailure(uid, stats, partner) is { } failure)
            {
                EndSession(uid, partner, failure);
                continue;
            }

            if (now >= stats.IdleUntil)
            {
                EndSession(uid, partner, WFIntimacyStopReason.Stopped);
                continue;
            }

            if (!stats.AutoThrust || now < stats.NextPulse)
                continue;

            if (stats.Act is not { } actId || !_proto.TryIndex(actId, out var act))
                continue;

            stats.NextPulse = now + TimeSpan.FromSeconds(WFIntimacyMath.Interval(act, stats.Speed));
            ApplyPulse(uid, partner, act);
        }
    }

    /// <summary>Takes the whole points out of a carry and leaves the fraction behind; works for decay too.</summary>
    private static int Take(ref float carry)
    {
        var whole = (int) carry;
        carry -= whole;
        return whole;
    }
}