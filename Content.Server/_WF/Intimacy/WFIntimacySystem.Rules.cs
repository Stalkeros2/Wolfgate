using Content.Shared._WF.Genitals;
using Content.Shared._WF.Genitals.Components;
using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Components;
using Content.Shared._WF.Intimacy.Prototypes;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.Humanoid;
using Content.Shared.Stunnable;

namespace Content.Server._WF.Intimacy;

// The smaller tests the act and session checks are built from.
public sealed partial class WFIntimacySystem
{
    /// <summary>Why an open session cannot go on; null while it can.</summary>
    private WFIntimacyStopReason? SessionFailure(EntityUid giver, WFIntimacyComponent stats, EntityUid partner)
    {
        if (!TryComp<WFIntimacyComponent>(partner, out var partnerStats) || partnerStats.Partner != giver)
            return WFIntimacyStopReason.PartnerGone;

        if (!TryComp<GenitalsComponent>(giver, out var giverGenitals)
            || !TryComp<GenitalsComponent>(partner, out var partnerGenitals))
            return WFIntimacyStopReason.PartnerGone;

        if (stats.Act is not { } actId || !_proto.TryIndex(actId, out var act))
            return WFIntimacyStopReason.ActInvalid;

        if (_mobState.IsDead(giver) || _mobState.IsDead(partner))
            return WFIntimacyStopReason.Dead;

        // Positional gates removed: an ongoing session is not broken by distance or movement.
        if (!_consent.HasMaster(giver) || !_consent.TargetConsents(partner))
            return WFIntimacyStopReason.ConsentRevoked;

        if (act.Consent is { } group
            && (!_consent.HasStrictConsent(partner, group) || !_consent.HasStrictConsent(giver, group)))
            return WFIntimacyStopReason.ConsentRevoked;

        if (act.ReceiverRegion != GenitalRegion.None && !RegionOpen(partner, partnerGenitals, act.ReceiverRegion))
            return WFIntimacyStopReason.ActInvalid;

        if (act.GiverRegion != GenitalRegion.None && !RegionOpen(giver, giverGenitals, act.GiverRegion))
            return WFIntimacyStopReason.ActInvalid;

        // Posture gates removed: standing, lying, buckled — any posture can take any act.
        return null;
    }

    /// <summary>Whether the body has the organ an act needs on the giving side; keeps useless verbs out of the menu.</summary>
    private bool UserFitsGiver(EntityUid user, IntimacyActPrototype act)
    {
        if (act.GiverOrgan is not { } slot)
            return true;

        return TryComp<GenitalsComponent>(user, out var genitals) && genitals.HasExternalOrgan(slot);
    }

    /// <summary>Open at the skin: clothing moved aside counts, as does wearing nothing.</summary>
    private bool RegionOpen(EntityUid uid, GenitalsComponent genitals, GenitalRegion region)
    {
        var coverage = _coverage.GetCoverage((uid, genitals, CompOrNull<HumanoidAppearanceComponent>(uid)));
        return _coverage.IsRegionOpen(genitals, region, coverage);
    }

    /// <summary>Someone else is already busy with a third body.</summary>
    private bool IsBusy(EntityUid user, EntityUid target)
    {
        if (TryComp<WFIntimacyComponent>(user, out var userStats)
            && userStats.Partner is { } userPartner && userPartner != target)
            return true;

        return TryComp<WFIntimacyComponent>(target, out var targetStats)
               && targetStats.Partner is { } targetPartner && targetPartner != user;
    }

    /// <summary>Buckled is its own posture; knocked down or asleep is lying.</summary>
    private WFIntimacyPosture Postures(EntityUid uid)
    {
        if (TryComp<BuckleComponent>(uid, out var buckle) && buckle.Buckled)
            return WFIntimacyPosture.Buckled;

        if (HasComp<KnockedDownComponent>(uid) || HasComp<SleepingComponent>(uid))
            return WFIntimacyPosture.Lying;

        return WFIntimacyPosture.Standing;
    }

    private static bool PostureFits(WFIntimacyPosture has, WFIntimacyPosture needs)
    {
        return needs == WFIntimacyPosture.Any || (needs & has) != 0;
    }
}