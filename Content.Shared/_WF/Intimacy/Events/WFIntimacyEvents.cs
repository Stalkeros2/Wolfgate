using Content.Shared._WF.Intimacy.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._WF.Intimacy.Events;

/// <summary>Raised on both bodies when a session starts.</summary>
[ByRefEvent]
public readonly record struct WFIntimacyStartedEvent(EntityUid Giver, EntityUid Receiver, ProtoId<IntimacyActPrototype> Act);

/// <summary>Raised on both bodies when a session ends, for whatever reason.</summary>
[ByRefEvent]
public readonly record struct WFIntimacyStoppedEvent(EntityUid Giver, EntityUid Receiver, WFIntimacyStopReason Reason);

/// <summary>Raised on both bodies after one motion.</summary>
[ByRefEvent]
public readonly record struct WFIntimacyPulseEvent(EntityUid Giver, EntityUid Receiver, ProtoId<IntimacyActPrototype> Act);

/// <summary>Raised on the body that reached the maximum pleasure.</summary>
[ByRefEvent]
public readonly record struct WFIntimacyOrgasmEvent(EntityUid Body, EntityUid? Partner, ProtoId<IntimacyActPrototype>? Act);

/// <summary>Raised on a body after its pleasure changed.</summary>
[ByRefEvent]
public readonly record struct WFPleasureChangedEvent(byte Old, byte New, EntityUid? Source);