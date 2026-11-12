using Robust.Shared.Serialization;

namespace Content.Shared._WF.Intimacy;

/// <summary>What an act is: the platonic fallbacks and the three intimate groups.</summary>
public enum WFIntimacyKind : byte
{
    Platonic,
    Manual,
    Oral,
    Penetrative,
}

/// <summary>Posture a body must be in for an act. Any means the posture does not matter.</summary>
[Flags]
public enum WFIntimacyPosture : byte
{
    Any = 0,
    Standing = 1 << 0,
    Buckled = 1 << 1,
    Lying = 1 << 2,
}

/// <summary>Why a session ended; carried by <see cref="Events.WFIntimacyStoppedEvent"/>.</summary>
public enum WFIntimacyStopReason : byte
{
    Stopped,
    Moved,
    Damaged,
    ConsentRevoked,
    Unconscious,
    Dead,
    PartnerGone,
    ActInvalid,
}

/// <summary>How hard an act is performed. Scales the gains; the top tier adds damage in a later phase.</summary>
[Serializable, NetSerializable]
public enum WFIntimacyIntensity : byte
{
    VeryGentle,
    Gentle,
    Normal,
    Hard,
    Rough,
}

/// <summary>How fast the automatic cadence repeats; scales the act interval.</summary>
[Serializable, NetSerializable]
public enum WFIntimacySpeed : byte
{
    Slow,
    Normal,
    Fast,
}
