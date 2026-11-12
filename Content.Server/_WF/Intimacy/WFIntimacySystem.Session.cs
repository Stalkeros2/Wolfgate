using Content.Shared._WF.Genitals;
using Content.Shared._WF.Genitals.Components;
using Content.Shared._WF.Genitals.Events;
using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Components;
using Content.Shared._WF.Intimacy.Events;
using Content.Shared._WF.Intimacy.Prototypes;
using Content.Shared.Database;

namespace Content.Server._WF.Intimacy;

// Opening, keeping and closing a session, and the clothing control the menu offers.
public sealed partial class WFIntimacySystem
{
    /// <summary>Opens a session for the pair, or keeps the one they already have. False when one of them is busy elsewhere.</summary>
    private bool EnsureSession(EntityUid giver, EntityUid target, IntimacyActPrototype act)
    {
        var giverStats = EnsureComp<WFIntimacyComponent>(giver);
        var targetStats = EnsureComp<WFIntimacyComponent>(target);

        if (giverStats.Partner == target && targetStats.Partner == giver)
        {
            giverStats.Act = act.ID;
            targetStats.Act = act.ID;
            return true;
        }

        if (giverStats.Partner != null || targetStats.Partner != null)
            return false;

        giverStats.Partner = target;
        giverStats.Thrusting = true;
        giverStats.Act = act.ID;
        giverStats.AutoThrust = false;

        targetStats.Partner = giver;
        targetStats.Thrusting = false;
        targetStats.Act = act.ID;
        targetStats.AutoThrust = false;

        RefreshSession(giver, target, act);

        var started = new WFIntimacyStartedEvent(giver, target, act.ID);
        RaiseLocalEvent(giver, ref started);
        RaiseLocalEvent(target, ref started);

        _adminLog.Add(LogType.WFIntimacy, LogImpact.Low,
            $"{ToPrettyString(giver):giver} started {act.ID} with {ToPrettyString(target):receiver}");
        return true;
    }

    /// <summary>Restarts the idle clock and lines the automatic cadence up with the last motion.</summary>
    private void RefreshSession(EntityUid giver, EntityUid receiver, IntimacyActPrototype act)
    {
        var now = _timing.CurTime;
        var idle = now + TimeSpan.FromSeconds(Settings.SessionIdleSeconds);

        if (TryComp<WFIntimacyComponent>(giver, out var giverStats))
        {
            giverStats.IdleUntil = idle;
            if (giverStats.AutoThrust)
                giverStats.NextPulse = now + TimeSpan.FromSeconds(WFIntimacyMath.Interval(act, giverStats.Speed));
        }

        if (TryComp<WFIntimacyComponent>(receiver, out var receiverStats))
            receiverStats.IdleUntil = idle;
    }

    /// <summary>Switches the automatic cadence on or off for the giver of an open session.</summary>
    private void ToggleAutoThrust(EntityUid uid, EntityUid partner)
    {
        var giver = TryComp<WFIntimacyComponent>(uid, out var stats) && stats.Thrusting ? uid : partner;
        if (!TryComp<WFIntimacyComponent>(giver, out var giverStats) || giverStats.Partner is null)
            return;

        giverStats.AutoThrust = !giverStats.AutoThrust;
        if (giverStats.AutoThrust && giverStats.Act is { } actId && _proto.TryIndex(actId, out var act))
            giverStats.NextPulse = _timing.CurTime + TimeSpan.FromSeconds(WFIntimacyMath.Interval(act, giverStats.Speed));
    }

    /// <summary>Ends the session a body is in, whichever side of it they are.</summary>
    private void EndSessionOf(EntityUid uid, WFIntimacyStopReason reason)
    {
        if (!TryComp<WFIntimacyComponent>(uid, out var stats) || stats.Partner is not { } partner)
            return;

        if (stats.Thrusting)
            EndSession(uid, partner, reason);
        else
            EndSession(partner, uid, reason);
    }

    /// <summary>Clears both sides and tells every listener once.</summary>
    private void EndSession(EntityUid giver, EntityUid receiver, WFIntimacyStopReason reason)
    {
        Clear(giver);
        Clear(receiver);

        var stopped = new WFIntimacyStoppedEvent(giver, receiver, reason);
        RaiseLocalEvent(giver, ref stopped);
        RaiseLocalEvent(receiver, ref stopped);
    }

    private void Clear(EntityUid uid)
    {
        if (!TryComp<WFIntimacyComponent>(uid, out var stats))
            return;

        stats.Partner = null;
        stats.Act = null;
        stats.Thrusting = false;
        stats.AutoThrust = false;
        stats.NextPulse = TimeSpan.Zero;
        stats.IdleUntil = TimeSpan.Zero;
    }

    /// <summary>Stores the chosen intensity and speed on one side of a session.</summary>
    private static void SetPace(WFIntimacyComponent stats, WFIntimacyIntensity intensity, WFIntimacySpeed speed)
    {
        stats.Intensity = intensity;
        stats.Speed = speed;
    }


    private void SetDisplaced(EntityUid uid, GenitalsComponent genitals, GenitalRegion region, bool open)
    {
        var displaced = open ? genitals.Displaced | region : genitals.Displaced & ~region;
        if (displaced == genitals.Displaced)
            return;

        genitals.Displaced = displaced;
        DirtyField(uid, genitals, nameof(GenitalsComponent.Displaced));

        var visuals = new GenitalsVisualsChangedEvent();
        RaiseLocalEvent(uid, ref visuals);
    }
}