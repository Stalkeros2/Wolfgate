using Content.Shared._WF.Intimacy;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._WF.Intimacy.Events;

/// <summary>One act in the menu, with the locale id of the reason it cannot run (null when it can).</summary>
[Serializable, NetSerializable]
public sealed class WFIntimacyMenuEntry(string actId, string? failure)
{
    public readonly string ActId = actId;
    public readonly string? Failure = failure;
}

/// <summary>Everything about the pair the window needs beyond the act list.</summary>
[Flags, Serializable, NetSerializable]
public enum WFIntimacyMenuFlags : ushort
{
    None = 0,
    TargetDead = 1 << 0,
    TargetUnattended = 1 << 1,
    YourMasterConsent = 1 << 2,
    YourGivingConsent = 1 << 3,
    SessionOpen = 1 << 4,
    AutoThrust = 1 << 5,
}

/// <summary>The menu the server built: every act, every refusal, and the session the pair is in.</summary>
[Serializable, NetSerializable]
public sealed class WFIntimacyMenuEvent(
    NetEntity target,
    WFIntimacyMenuEntry[] entries,
    WFIntimacyMenuFlags flags,
    string? sessionAct,
    WFIntimacyIntensity intensity,
    WFIntimacySpeed speed,
    byte giverArousal,
    byte receiverArousal,
    byte giverPleasure,
    byte receiverPleasure,
    byte giverWetness,
    byte receiverWetness) : EntityEventArgs
{
    public readonly NetEntity Target = target;
    public readonly WFIntimacyMenuEntry[] Entries = entries;
    public readonly WFIntimacyMenuFlags Flags = flags;
    public readonly string? SessionAct = sessionAct;
    public readonly WFIntimacyIntensity Intensity = intensity;
    public readonly WFIntimacySpeed Speed = speed;
    public readonly byte GiverArousal = giverArousal;
    public readonly byte ReceiverArousal = receiverArousal;
    public readonly byte GiverPleasure = giverPleasure;
    public readonly byte ReceiverPleasure = receiverPleasure;
    public readonly byte GiverWetness = giverWetness;
    public readonly byte ReceiverWetness = receiverWetness;
}

/// <summary>Asks for the menu for a target; also used to refresh it.</summary>
[Serializable, NetSerializable]
public sealed class WFIntimacyMenuRequestEvent(NetEntity target) : EntityEventArgs
{
    public readonly NetEntity Target = target;
}

/// <summary>Starts an act with the chosen intensity and speed.</summary>
[Serializable, NetSerializable]
public sealed class WFIntimacyStartRequestEvent(
    NetEntity target,
    string actId,
    WFIntimacyIntensity intensity,
    WFIntimacySpeed speed) : EntityEventArgs
{
    public readonly NetEntity Target = target;
    public readonly string ActId = actId;
    public readonly WFIntimacyIntensity Intensity = intensity;
    public readonly WFIntimacySpeed Speed = speed;
}

/// <summary>Ends the sender's session.</summary>
[Serializable, NetSerializable]
public sealed class WFIntimacyStopRequestEvent : EntityEventArgs;