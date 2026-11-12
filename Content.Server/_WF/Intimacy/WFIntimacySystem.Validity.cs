using Content.Shared._Common.Consent;
using Content.Shared._WF.Genitals;
using Content.Shared._WF.Genitals.Components;
using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._WF.Intimacy;

// Fills the shared rule context from the world. The refusals and their order live in WFIntimacyRules, so the menu
// can run the same checks the server will.
public sealed partial class WFIntimacySystem
{
    /// <summary>An unset toggle allows everyone; a set one has to be on for that body.</summary>
    private bool Allows(ProtoId<ConsentTogglePrototype>? toggle, EntityUid uid)
    {
        return toggle is not { } id || _consent.HasStrictConsent(uid, id);
    }

    /// <summary>What stops the act, as a locale id; null means it can start right now.</summary>
    private string? GetFailure(EntityUid user, EntityUid target, IntimacyActPrototype act)
    {
        var settings = Settings;
        var range = WFIntimacyRules.EffectiveRange(act, settings);
        var sameBody = user == target || TerminatingOrDeleted(user) || TerminatingOrDeleted(target);

        // wf.intimacy_debug: a body nobody is playing cannot consent or count as present, so for testing it is
        // treated as both, and given anatomy to act against.
        var testSubject = _debug && !HasComp<ActorComponent>(target);
        if (testSubject && (!TryComp<GenitalsComponent>(target, out var testGenitals) || !testGenitals.OrgansBuilt))
            _organs.CopyAnatomy(user, target);

        var ctx = new WFIntimacyRuleContext
        {
            AnatomyEnabled = _genitals.AnatomyEnabled,
            SameBody = sameBody,
            GiverDead = _mobState.IsDead(user),
            ReceiverDead = _mobState.IsDead(target),
            // Positional gates removed: any body can act on any other at any distance, in any posture.
            // Consent, anatomy and region checks still run below.
            InRange = true,
            GiverMasterConsent = _consent.HasMaster(user),
            ReceiverAllows = testSubject || _consent.TargetConsents(target),
            GiverAllowsGiving = Allows(settings.GiveConsent, user),
            ReceiverAllowsReceiving = testSubject || Allows(settings.ReceiveConsent, target),
            GiverHasActGroup = Allows(act.Consent, user),
            ReceiverHasActGroup = testSubject || Allows(act.Consent, target),
            GiverHasActGiverGroup = Allows(act.GiverConsent, user),
            Busy = IsBusy(user, target),
            GiverPostureFits = true,
            ReceiverPostureFits = true,
        };

        if (!TryComp<GenitalsComponent>(user, out var giverGenitals)
            || !TryComp<GenitalsComponent>(target, out var receiverGenitals))
        {
            // Either body without anatomy fails the pair, and nothing below can be judged without it.
            ctx.HasAnatomy = false;
            return WFIntimacyRules.Failure(ctx, act);
        }

        ctx.GiverHasOrgan = act.GiverOrgan is not { } giverSlot || giverGenitals.HasExternalOrgan(giverSlot);
        ctx.ReceiverHasOrgan = act.ReceiverOrgan is not { } receiverSlot
                               || receiverGenitals.HasExternalOrgan(receiverSlot);
        ctx.GiverRegionOpen = act.GiverRegion == GenitalRegion.None
                              || RegionOpen(user, giverGenitals, act.GiverRegion);
        ctx.ReceiverRegionOpen = act.ReceiverRegion == GenitalRegion.None
                                 || RegionOpen(target, receiverGenitals, act.ReceiverRegion);

        return WFIntimacyRules.Failure(ctx, act);
    }
}