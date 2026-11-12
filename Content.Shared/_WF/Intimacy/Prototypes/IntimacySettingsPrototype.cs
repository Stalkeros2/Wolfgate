using Content.Shared._Common.Consent;
using Robust.Shared.Prototypes;

namespace Content.Shared._WF.Intimacy.Prototypes;

/// <summary>Global intimacy tuning. A single prototype, id WFIntimacyDefault.</summary>
[Prototype("intimacySettings")]
public sealed partial class IntimacySettingsPrototype : IPrototype
{
    /// <summary>Id of the settings prototype the game reads.</summary>
    public static readonly ProtoId<IntimacySettingsPrototype> DefaultId = "WFIntimacyDefault";

    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Distance in metres both bodies must stay within.</summary>
    [DataField]
    public float MaxRange = 1.5f;

    /// <summary>Seconds of no input before an idle session is torn down.</summary>
    [DataField]
    public float SessionIdleSeconds = 20f;

    /// <summary>How much lubrication raises arousal gains.</summary>
    [DataField]
    public float WetnessArousalWeight = 0.5f;

    /// <summary>How much lubrication raises pleasure gains.</summary>
    [DataField]
    public float WetnessPleasureWeight = 0.7f;

    /// <summary>Below this lubrication a groin act gains less and warns.</summary>
    [DataField]
    public byte DryThreshold = 20;

    /// <summary>Gain multiplier below <see cref="DryThreshold"/>.</summary>
    [DataField]
    public float DryPenalty = 0.55f;

    /// <summary>Arousal lost per second outside a session.</summary>
    [DataField]
    public float ArousalDecayPerSecond = 0.08f;

    /// <summary>Lubrication lost per second outside a session.</summary>
    [DataField]
    public float WetnessDecayPerSecond = 0.12f;

    /// <summary>Pleasure lost per second outside a session.</summary>
    [DataField]
    public float PleasureDecayPerSecond = 4f;

    /// <summary>Lubrication gained per second while aroused and in a session.</summary>
    [DataField]
    public float SelfLubricationPerSecond = 0.35f;

    /// <summary>Arousal gained per second with the auto-arousal toggle on.</summary>
    [DataField]
    public float AutoArousalPerSecond = 0.02f;

    /// <summary>Seconds of reduced gains after an orgasm.</summary>
    [DataField]
    public float AfterglowSeconds = 30f;

    /// <summary>Gain multiplier during afterglow.</summary>
    [DataField]
    public float AfterglowMultiplier = 0.4f;

    /// <summary>Arousal removed by an orgasm.</summary>
    [DataField]
    public byte OrgasmArousalDrop = 40;

    /// <summary>Toggle the receiver needs before another body may start an act. Unset refuses.</summary>
    [DataField]
    public ProtoId<ConsentTogglePrototype>? ReceiveConsent;

    /// <summary>Toggle the giver needs before starting an act. Unset refuses.</summary>
    [DataField]
    public ProtoId<ConsentTogglePrototype>? GiveConsent;
}