using Content.Shared._WF.Genitals;
using Content.Shared._WF.Genitals.Components;
using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Components;
using Content.Shared._WF.Intimacy.Events;
using Content.Shared._WF.Intimacy.Prototypes;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;

namespace Content.Server._WF.Intimacy;

// One motion: arousal, pleasure and lubrication, then a climax if the scale tops out.
public sealed partial class WFIntimacySystem
{
    private void ApplyPulse(EntityUid giver, EntityUid receiver, IntimacyActPrototype act)
    {
        if (!TryComp<WFIntimacyComponent>(giver, out var giverStats)
            || !TryComp<WFIntimacyComponent>(receiver, out var receiverStats)
            || !TryComp<GenitalsComponent>(giver, out var giverGenitals)
            || !TryComp<GenitalsComponent>(receiver, out var receiverGenitals))
            return;

        var settings = Settings;
        var now = _timing.CurTime;
        var receiverAfterglow = receiverStats.AfterglowUntil > now;
        var giverAfterglow = giverStats.AfterglowUntil > now;

        var arousal = WFIntimacyMath.PulseArousal(act, receiverStats.Wetness, settings, receiverAfterglow);
        var giverArousal = WFIntimacyMath.PulseGiverArousal(act, giverStats.Wetness, settings, giverAfterglow);

        if (settings.ReceiveConsent is { } receiveToggle)
            _arousal.TryAdjustArousal((receiver, receiverGenitals), (int) MathF.Round(arousal), receiveToggle, giver);

        if (settings.GiveConsent is { } giveToggle)
            _arousal.TryAdjustArousal((giver, giverGenitals), (int) MathF.Round(giverArousal), giveToggle, receiver);

        var dry = act.ReceiverRegion != GenitalRegion.None && receiverStats.Wetness < settings.DryThreshold;
        var pleasure = receiverStats.OrgasmCooldownUntil > now
            ? 0f
            : WFIntimacyMath.PulsePleasure(act, receiverGenitals.Arousal, receiverStats.Wetness, settings, receiverAfterglow);

        if (act.InvertPleasure)
        {
            // Role reversal: the giver is the one receiving the primary stimulation.
            SetPleasure(receiver, receiverStats, WFIntimacyMath.Add(receiverStats.Pleasure, pleasure * act.GiverShare), giver);
            SetPleasure(giver, giverStats, WFIntimacyMath.Add(giverStats.Pleasure, pleasure), receiver);
        }
        else
        {
            SetPleasure(receiver, receiverStats, WFIntimacyMath.Add(receiverStats.Pleasure, pleasure), giver);
            SetPleasure(giver, giverStats, WFIntimacyMath.Add(giverStats.Pleasure, pleasure * act.GiverShare), receiver);
        }
        SetWetness(receiver, receiverStats, WFIntimacyMath.Add(receiverStats.Wetness, act.WetnessGain));

        if (dry && now >= receiverStats.DryWarnedUntil)
        {
            receiverStats.DryWarnedUntil = now + TimeSpan.FromSeconds(30);
            _popup.PopupEntity(Loc.GetString("wf-intimacy-dry-target"), receiver, receiver, PopupType.SmallCaution);
            _popup.PopupEntity(
                Loc.GetString("wf-intimacy-dry-user", ("target", Identity.Entity(receiver, EntityManager))),
                giver, giver, PopupType.SmallCaution);
        }

        var pulse = new WFIntimacyPulseEvent(giver, receiver, act.ID);
        RaiseLocalEvent(giver, ref pulse);
        RaiseLocalEvent(receiver, ref pulse);

        var inside = act.Kind == WFIntimacyKind.Penetrative;

        if (WFIntimacyMath.ReachesOrgasm(receiverStats.Pleasure))
            Orgasm(receiver, receiverStats, giver, act, inside, null);

        if (WFIntimacyMath.ReachesOrgasm(giverStats.Pleasure))
            Orgasm(giver, giverStats, receiver, act, inside, null);
    }

    /// <summary>Pleasure tops out: reset it, start the afterglow and tell the modules listening.</summary>
    private void Orgasm(EntityUid uid, WFIntimacyComponent stats, EntityUid? partner, IntimacyActPrototype act,
        bool inside, string? where)
    {
        var settings = Settings;
        var now = _timing.CurTime;

        SetPleasure(uid, stats, 0, partner);
        stats.AfterglowUntil = now + TimeSpan.FromSeconds(settings.AfterglowSeconds);
        stats.OrgasmCooldownUntil = stats.AfterglowUntil;

        if (TryComp<GenitalsComponent>(uid, out var genitals) && settings.ReceiveConsent is { } toggle)
            _arousal.TryAdjustArousal((uid, genitals), -settings.OrgasmArousalDrop, toggle, partner);

        // The body that reached the climax: inside or on the partner.
        var selfKey = inside
            ? (where is not null ? "wf-intimacy-climax-self-inside-where" : "wf-intimacy-climax-self-inside")
            : (where is not null ? "wf-intimacy-climax-self-outside-where" : "wf-intimacy-climax-self-outside");
        var selfArgs = new List<(string, object)> { ("partner", Identity.Entity(partner ?? uid, EntityManager)) };
        if (where is not null)
            selfArgs.Add(("where", where));
        _popup.PopupEntity(Loc.GetString(selfKey, selfArgs.ToArray()), uid, uid, PopupType.Medium);

        // The other body: someone finished inside or on them.
        if (partner is { } other)
        {
            var otherKey = inside
                ? (where is not null ? "wf-intimacy-climax-partner-inside-where" : "wf-intimacy-climax-partner-inside")
                : (where is not null ? "wf-intimacy-climax-partner-outside-where" : "wf-intimacy-climax-partner-outside");
            var otherArgs = new List<(string, object)> { ("user", Identity.Entity(uid, EntityManager)) };
            if (where is not null)
                otherArgs.Add(("where", where));
            _popup.PopupEntity(Loc.GetString(otherKey, otherArgs.ToArray()), other, other, PopupType.Medium);
        }

        var ev = new WFIntimacyOrgasmEvent(uid, partner, act.ID);
        RaiseLocalEvent(uid, ref ev);

        _adminLog.Add(LogType.WFIntimacy, LogImpact.Low, $"{ToPrettyString(uid):body} reached a climax");
    }
}