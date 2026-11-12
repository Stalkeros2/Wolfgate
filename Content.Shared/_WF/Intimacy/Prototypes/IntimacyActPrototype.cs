using Content.Shared._Common.Consent;
using Content.Shared._WF.Genitals;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._WF.Intimacy.Prototypes;

/// <summary>One thing two adjacent bodies can do, from a wave to a penetrative act.</summary>
[Prototype("intimacyAct")]
public sealed partial class IntimacyActPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Menu label.</summary>
    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public WFIntimacyKind Kind = WFIntimacyKind.Manual;

    /// <summary>
    /// Metres the two bodies must stay within. Zero uses the settings default and a negative value has no limit
    /// at all, which is what lets a wave reach a body across the room.
    /// </summary>
    [DataField]
    public float Range;

    /// <summary>The giver's organ that must be present; null needs none.</summary>
    [DataField]
    public GenitalSlot? GiverOrgan;

    /// <summary>The receiver's organ that must be present; null needs none.</summary>
    [DataField]
    public GenitalSlot? ReceiverOrgan;

    /// <summary>
    /// Region of the receiver that must be open, whether the garment is removed or only moved aside. None works
    /// over clothing, which is what the clothed acts use.
    /// </summary>
    [DataField]
    public GenitalRegion ReceiverRegion = GenitalRegion.None;

    /// <summary>Region of the giver that must be open; None needs none.</summary>
    [DataField]
    public GenitalRegion GiverRegion = GenitalRegion.None;

    [DataField]
    public WFIntimacyPosture GiverPosture = WFIntimacyPosture.Any;

    [DataField]
    public WFIntimacyPosture ReceiverPosture = WFIntimacyPosture.Any;

    /// <summary>Toggle the receiver must have on.</summary>
    [DataField]
    public ProtoId<ConsentTogglePrototype>? Consent;

    /// <summary>Toggle the giver must have on.</summary>
    [DataField]
    public ProtoId<ConsentTogglePrototype>? GiverConsent;

    /// <summary>Arousal one motion adds to the receiver.</summary>
    [DataField]
    public float ArousalGain = 4f;

    /// <summary>Pleasure one motion adds to the receiver.</summary>
    [DataField]
    public float PleasureGain = 4f;

    /// <summary>Share of the receiver's arousal and pleasure gains the giver also gets.</summary>
    [DataField]
    public float GiverShare = 0.35f;

    /// <summary>Lubrication one motion adds to the receiver.</summary>
    [DataField]
    public float WetnessGain = 2f;

    /// <summary>Seconds one motion takes, both for the manual DoAfter and for the automatic cadence.</summary>
    [DataField]
    public float PulseInterval = 1.4f;

    /// <summary>When true, the giver gets the primary pleasure and the receiver gets the share instead.</summary>
    [DataField]
    public bool InvertPleasure;

    /// <summary>Whether the giver may let it repeat on its own.</summary>
    [DataField]
    public bool AutoThrustable = true;

    /// <summary>Emote the giver plays, if any.</summary>
    [DataField]
    public LocId? Emote;

    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>Menu order; lower comes first.</summary>
    [DataField]
    public int Priority;
}