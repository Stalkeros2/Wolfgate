using Content.Shared._Common.Consent;
using Content.Shared._WF.Genitals;
using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Prototypes;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.Tests._WF.Intimacy;

/// <summary>The act gates: one refusal order, shared by the server and the menu.</summary>
[TestFixture]
[TestOf(typeof(WFIntimacyRules))]
public sealed class WFIntimacyRulesTest
{
    private static readonly IntimacySettingsPrototype Settings = new();

    /// <summary>An intimate act with an open groin and nothing else declared, so a test breaks one gate.</summary>
    private static IntimacyActPrototype Intimate()
    {
        return new IntimacyActPrototype
        {
            Kind = WFIntimacyKind.Manual,
            ReceiverRegion = GenitalRegion.Groin,
        };
    }

    private static IntimacyActPrototype Platonic()
    {
        return new IntimacyActPrototype { Kind = WFIntimacyKind.Platonic };
    }

    /// <summary>Nothing set means nothing refuses.</summary>
    [Test]
    public void CleanContextPasses()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext(), Intimate()), Is.Null);
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext(), Platonic()), Is.Null);
        });
    }

    /// <summary>A body cannot act on itself, and a body being deleted cannot act at all.</summary>
    [Test]
    public void SelfAndGoneAreRefused()
    {
        Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { SameBody = true }, Intimate()),
            Is.EqualTo(WFIntimacyRules.FailSelf));
    }

    /// <summary>The kill switch refuses everything, platonic acts included.</summary>
    [Test]
    public void TheKillSwitchRefusesEverything()
    {
        var ctx = new WFIntimacyRuleContext { AnatomyEnabled = false };

        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyRules.Failure(ctx, Intimate()), Is.EqualTo(WFIntimacyRules.FailDisabled));
            Assert.That(WFIntimacyRules.Failure(ctx, Platonic()), Is.EqualTo(WFIntimacyRules.FailDisabled));
        });
    }

    /// <summary>Death is checked before anything else a body might be missing.</summary>
    [Test]
    public void DeathComesFirst()
    {
        var ctx = new WFIntimacyRuleContext { ReceiverDead = true, GiverMasterConsent = false, HasAnatomy = false };

        Assert.That(WFIntimacyRules.Failure(ctx, Intimate()), Is.EqualTo(WFIntimacyRules.FailDead));
    }

    /// <summary>Distance refuses platonic acts too; a wave has to declare its own range to escape it.</summary>
    [Test]
    public void DistanceAppliesToPlatonicActs()
    {
        Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { InRange = false }, Platonic()),
            Is.EqualTo(WFIntimacyRules.FailRange));
    }

    /// <summary>A platonic act ignores every consent toggle, every organ and an occupied partner.</summary>
    [Test]
    public void PlatonicActsIgnoreConsentAndAnatomy()
    {
        var ctx = new WFIntimacyRuleContext
        {
            HasAnatomy = false,
            GiverMasterConsent = false,
            ReceiverAllows = false,
            GiverAllowsGiving = false,
            ReceiverAllowsReceiving = false,
            GiverHasActGroup = false,
            ReceiverHasActGroup = false,
            Busy = true,
        };

        Assert.That(WFIntimacyRules.Failure(ctx, Platonic()), Is.Null);
    }

    /// <summary>An intimate act needs both bodies to have anatomy.</summary>
    [Test]
    public void IntimateActsNeedAnatomy()
    {
        Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { HasAnatomy = false }, Intimate()),
            Is.EqualTo(WFIntimacyRules.FailNoAnatomy));
    }

    /// <summary>The master switch and the receiver's own allowance both have to be on.</summary>
    [Test]
    public void MasterConsentGatesTheAct()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { GiverMasterConsent = false }, Intimate()),
                Is.EqualTo(WFIntimacyRules.FailConsent));
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { ReceiverAllows = false }, Intimate()),
                Is.EqualTo(WFIntimacyRules.FailConsent));
        });
    }

    /// <summary>Giving has its own refusal, so the menu can say which side is closed.</summary>
    [Test]
    public void GivingNeedsItsOwnToggle()
    {
        Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { GiverAllowsGiving = false }, Intimate()),
            Is.EqualTo(WFIntimacyRules.FailGiveConsent));
    }

    /// <summary>Receiving has its own toggle, and it refuses as a consent problem.</summary>
    [Test]
    public void ReceivingNeedsItsOwnToggle()
    {
        Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { ReceiverAllowsReceiving = false }, Intimate()),
            Is.EqualTo(WFIntimacyRules.FailConsent));
    }

    /// <summary>An act that declares a group toggle needs it on for both sides.</summary>
    [Test]
    public void TheActGroupNeedsBothSides()
    {
        var act = Intimate();
        act.Consent = new ProtoId<ConsentTogglePrototype>("WFIntimacyManual");

        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { ReceiverHasActGroup = false }, act),
                Is.EqualTo(WFIntimacyRules.FailConsent));
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { GiverHasActGroup = false }, act),
                Is.EqualTo(WFIntimacyRules.FailConsent));
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext(), act), Is.Null, "Both on passes.");
        });
    }

    /// <summary>A group declared only for the giver is a give refusal.</summary>
    [Test]
    public void TheGiversGroupIsSeparate()
    {
        var act = Intimate();
        act.GiverConsent = new ProtoId<ConsentTogglePrototype>("WFIntimacyGive");

        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { GiverHasActGiverGroup = false }, act),
                Is.EqualTo(WFIntimacyRules.FailGiveConsent));
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { GiverHasActGroup = false }, act), Is.Null,
                "The receiver's group is not consulted when the act declares only a giver one.");
        });
    }

    /// <summary>A body already busy with someone else cannot take another act.</summary>
    [Test]
    public void ABusyBodyIsRefused()
    {
        Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { Busy = true }, Intimate()),
            Is.EqualTo(WFIntimacyRules.FailBusy));
    }

    /// <summary>Organs are refused per side, and only for acts that declare them.</summary>
    [Test]
    public void OrgansAreCheckedPerSide()
    {
        var act = Intimate();
        act.GiverOrgan = GenitalSlot.Penis;
        act.ReceiverOrgan = GenitalSlot.Vagina;

        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { GiverHasOrgan = false }, act),
                Is.EqualTo(WFIntimacyRules.FailGiverOrgan));
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { ReceiverHasOrgan = false }, act),
                Is.EqualTo(WFIntimacyRules.FailReceiverOrgan));
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext(), Intimate()), Is.Null,
                "An act that declares no organ ignores the flags.");
        });
    }

    /// <summary>Clothing is only a refusal over a region the act asks to be bare.</summary>
    [Test]
    public void ClothingIsCheckedPerRegion()
    {
        var giverOnly = new IntimacyActPrototype { Kind = WFIntimacyKind.Manual, GiverRegion = GenitalRegion.Groin };

        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { GiverRegionOpen = false }, giverOnly),
                Is.EqualTo(WFIntimacyRules.FailGiverCovered));
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { ReceiverRegionOpen = false }, giverOnly),
                Is.Null, "An act that asks nothing of the receiver ignores the receiver's region.");
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { ReceiverRegionOpen = false }, Intimate()),
                Is.EqualTo(WFIntimacyRules.FailCovered));
        });
    }

    /// <summary>Posture is refused per side.</summary>
    [Test]
    public void PostureIsCheckedPerSide()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { GiverPostureFits = false }, Intimate()),
                Is.EqualTo(WFIntimacyRules.FailGiverPosture));
            Assert.That(WFIntimacyRules.Failure(new WFIntimacyRuleContext { ReceiverPostureFits = false }, Intimate()),
                Is.EqualTo(WFIntimacyRules.FailPosture));
        });
    }

    /// <summary>A declared range wins, zero means the settings default and a negative one means no limit.</summary>
    [Test]
    public void RangeFallsBackToTheSettings()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WFIntimacyRules.EffectiveRange(Intimate(), Settings), Is.EqualTo(Settings.MaxRange));
            Assert.That(WFIntimacyRules.EffectiveRange(new IntimacyActPrototype { Range = 2f }, Settings),
                Is.EqualTo(2f));
            Assert.That(WFIntimacyRules.EffectiveRange(new IntimacyActPrototype { Range = -1f }, Settings),
                Is.EqualTo(0f), "No limit is reported as zero.");
        });
    }
}