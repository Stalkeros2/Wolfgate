using Content.Shared._WF.Intimacy.Components;
using Content.Shared._WF.Intimacy.Events;

namespace Content.Server._WF.Intimacy;

// The only write paths for the two networked stats.
public sealed partial class WFIntimacySystem
{
    /// <summary>Sets pleasure, dirties it and tells every listener.</summary>
    private void SetPleasure(EntityUid uid, WFIntimacyComponent stats, byte value, EntityUid? source)
    {
        if (stats.Pleasure == value)
            return;

        var old = stats.Pleasure;
        stats.Pleasure = value;
        DirtyField(uid, stats, nameof(WFIntimacyComponent.Pleasure));

        var ev = new WFPleasureChangedEvent(old, value, source);
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>Sets lubrication and dirties it.</summary>
    private void SetWetness(EntityUid uid, WFIntimacyComponent stats, byte value)
    {
        if (stats.Wetness == value)
            return;

        stats.Wetness = value;
        DirtyField(uid, stats, nameof(WFIntimacyComponent.Wetness));
    }
}