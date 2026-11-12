using Content.Shared._WF.Genitals;
using Content.Shared._WF.Intimacy.Prototypes;

namespace Content.Shared._WF.Intimacy;

/// <summary>
/// The act gates, as one pure function over <see cref="WFIntimacyRuleContext"/>, so the server's refusal and the
/// menu's greyed entries can never disagree.
/// </summary>
public static class WFIntimacyRules
{
    /// <summary>Locale id of a refusal, in the order a player hits them.</summary>
    public const string FailSelf = "wf-intimacy-fail-self";
    public const string FailDisabled = "wf-intimacy-fail-disabled";
    public const string FailDead = "wf-intimacy-fail-dead";
    public const string FailRange = "wf-intimacy-fail-range";
    public const string FailNoAnatomy = "wf-intimacy-fail-no-anatomy";
    public const string FailConsent = "wf-intimacy-fail-consent";
    public const string FailGiveConsent = "wf-intimacy-fail-give-consent";
    public const string FailBusy = "wf-intimacy-fail-busy";
    public const string FailGiverOrgan = "wf-intimacy-fail-giver-organ";
    public const string FailReceiverOrgan = "wf-intimacy-fail-receiver-organ";
    public const string FailGiverCovered = "wf-intimacy-fail-giver-covered";
    public const string FailCovered = "wf-intimacy-fail-covered";
    public const string FailGiverPosture = "wf-intimacy-fail-giver-posture";
    public const string FailPosture = "wf-intimacy-fail-posture";

    /// <summary>
    /// Metres the bodies must stay within. Zero means no distance limit at all, which is what lets a wave reach a
    /// body across the room while a handshake does not.
    /// </summary>
    public static float EffectiveRange(IntimacyActPrototype act, IntimacySettingsPrototype settings)
    {
        if (act.Range < 0f)
            return 0f;

        return act.Range > 0f ? act.Range : settings.MaxRange;
    }

    /// <summary>The first reason the act cannot run right now, as a locale id; null when it can.</summary>
    public static string? Failure(WFIntimacyRuleContext ctx, IntimacyActPrototype act)
    {
        if (ctx.SameBody)
            return FailSelf;

        if (!ctx.AnatomyEnabled)
            return FailDisabled;

        if (ctx.GiverDead || ctx.ReceiverDead)
            return FailDead;

        if (!ctx.InRange)
            return FailRange;

        // A platonic act asks for nothing past the distance and the bodies being alive.
        if (act.Kind == WFIntimacyKind.Platonic)
            return null;

        if (!ctx.HasAnatomy)
            return FailNoAnatomy;

        if (!ctx.GiverMasterConsent || !ctx.ReceiverAllows)
            return FailConsent;

        if (!ctx.GiverAllowsGiving)
            return FailGiveConsent;

        if (!ctx.ReceiverAllowsReceiving)
            return FailConsent;

        // Taking part is taking part: the act's own toggle has to be on for both sides.
        if (act.Consent is not null && (!ctx.GiverHasActGroup || !ctx.ReceiverHasActGroup))
            return FailConsent;

        if (act.GiverConsent is not null && !ctx.GiverHasActGiverGroup)
            return FailGiveConsent;

        if (ctx.Busy)
            return FailBusy;

        if (act.GiverOrgan is not null && !ctx.GiverHasOrgan)
            return FailGiverOrgan;

        if (act.ReceiverOrgan is not null && !ctx.ReceiverHasOrgan)
            return FailReceiverOrgan;

        if (act.GiverRegion != GenitalRegion.None && !ctx.GiverRegionOpen)
            return FailGiverCovered;

        if (act.ReceiverRegion != GenitalRegion.None && !ctx.ReceiverRegionOpen)
            return FailCovered;

        if (!ctx.GiverPostureFits)
            return FailGiverPosture;

        if (!ctx.ReceiverPostureFits)
            return FailPosture;

        return null;
    }
}

/// <summary>
/// What <see cref="WFIntimacyRules.Failure"/> needs to know. Every field that can only fail starts satisfied, so a
/// caller fills in what it can see and a test breaks exactly the gate it is about.
/// </summary>
public sealed class WFIntimacyRuleContext
{
    public bool AnatomyEnabled = true;
    public bool SameBody;
    public bool GiverDead;
    public bool ReceiverDead;
    public bool InRange = true;
    public bool HasAnatomy = true;
    public bool GiverMasterConsent = true;
    public bool ReceiverAllows = true;
    public bool GiverAllowsGiving = true;
    public bool ReceiverAllowsReceiving = true;
    public bool GiverHasActGroup = true;
    public bool ReceiverHasActGroup = true;
    public bool GiverHasActGiverGroup = true;
    public bool Busy;
    public bool GiverHasOrgan = true;
    public bool ReceiverHasOrgan = true;
    public bool GiverRegionOpen = true;
    public bool ReceiverRegionOpen = true;
    public bool GiverPostureFits = true;
    public bool ReceiverPostureFits = true;
}