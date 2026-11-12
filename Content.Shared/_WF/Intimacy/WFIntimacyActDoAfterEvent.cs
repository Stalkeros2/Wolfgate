using Content.Shared._WF.Intimacy.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._WF.Intimacy;

/// <summary>One manual motion of an act, applied when its interval elapses.</summary>
[Serializable, NetSerializable]
public sealed partial class WFIntimacyActDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public ProtoId<IntimacyActPrototype> Act;

    public override DoAfterEvent Clone()
    {
        return new WFIntimacyActDoAfterEvent { Act = Act };
    }
}