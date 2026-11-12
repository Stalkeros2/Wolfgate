using Content.Shared._WF.Intimacy.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._WF.Intimacy.Components;

/// <summary>Per-body intimacy state: pleasure, lubrication and the session this body is in.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class WFIntimacyComponent : Component
{
    /// <summary>Pleasure and lubrication go only to the owner; no bystander ever receives them.</summary>
    public override bool SessionSpecific => true;

    /// <summary>Pleasure 0-100. At the maximum the body orgasms.</summary>
    [DataField, AutoNetworkedField]
    public byte Pleasure;

    /// <summary>Lubrication 0-100: raises arousal and pleasure gains and avoids the dry penalty.</summary>
    [DataField, AutoNetworkedField]
    public byte Wetness;

    /// <summary>Fractional values waiting to become whole points; server only.</summary>
    [ViewVariables]
    public float ArousalCarry;

    [ViewVariables]
    public float WetnessCarry;

    [ViewVariables]
    public float PleasureCarry;

    /// <summary>No further dry warning before this time, so a session does not spam chat. Server only.</summary>
    [ViewVariables]
    public TimeSpan DryWarnedUntil;

    /// <summary>The body this one is in a session with. Server only.</summary>
    [ViewVariables]
    public EntityUid? Partner;

    /// <summary>The act in progress. Server only.</summary>
    [ViewVariables]
    public ProtoId<IntimacyActPrototype>? Act;

    /// <summary>How hard the current act is performed. Server only for now.</summary>
    [ViewVariables]
    public WFIntimacyIntensity Intensity = WFIntimacyIntensity.Normal;

    /// <summary>How fast the automatic cadence repeats. Server only for now.</summary>
    [ViewVariables]
    public WFIntimacySpeed Speed = WFIntimacySpeed.Normal;

    /// <summary>This body is the giver of the current session. Server only.</summary>
    [ViewVariables]
    public bool Thrusting;

    /// <summary>The giver's automatic pulse toggle. Server only.</summary>
    [ViewVariables]
    public bool AutoThrust;

    /// <summary>When the next automatic pulse is due. Server only.</summary>
    [ViewVariables]
    public TimeSpan NextPulse;

    /// <summary>Reduced gains until this time. Server only.</summary>
    [ViewVariables]
    public TimeSpan AfterglowUntil;

    /// <summary>No further orgasm before this time. Server only.</summary>
    [ViewVariables]
    public TimeSpan OrgasmCooldownUntil;

    /// <summary>A session with no input for long enough is torn down. Server only.</summary>
    [ViewVariables]
    public TimeSpan IdleUntil;
}