using System.Linq;
using Content.Shared._WF.Genitals.Components;
using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Components;
using Content.Shared._WF.Intimacy.Events;
using Content.Shared._WF.Intimacy.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._WF.Intimacy;

// The window: one menu built from every act and its refusal, and the requests the client can send back.
public sealed partial class WFIntimacySystem
{
    private void InitializeMenu()
    {
        SubscribeNetworkEvent<WFIntimacyMenuRequestEvent>(OnMenuRequest);
        SubscribeNetworkEvent<WFIntimacyStartRequestEvent>(OnStartRequest);
        SubscribeNetworkEvent<WFIntimacyStopRequestEvent>(OnStopRequest);
    }

    /// <summary>Sends the menu to the body's player, aimed at the target.</summary>
    private void OpenMenu(EntityUid user, EntityUid target)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        RaiseNetworkEvent(BuildMenu(user, target), actor.PlayerSession);
    }

    /// <summary>Every act with its refusal, and the session the two bodies are already in.</summary>
    private WFIntimacyMenuEvent BuildMenu(EntityUid user, EntityUid target)
    {
        var acts = _proto.EnumeratePrototypes<IntimacyActPrototype>().OrderBy(a => a.Priority).ToList();
        var entries = new WFIntimacyMenuEntry[acts.Count];
        for (var i = 0; i < acts.Count; i++)
            entries[i] = new WFIntimacyMenuEntry(acts[i].ID, GetFailure(user, target, acts[i]));

        var flags = WFIntimacyMenuFlags.None;
        if (_mobState.IsDead(target))
            flags |= WFIntimacyMenuFlags.TargetDead;

        if (!HasComp<ActorComponent>(target))
            flags |= WFIntimacyMenuFlags.TargetUnattended;

        if (_consent.HasMaster(user))
            flags |= WFIntimacyMenuFlags.YourMasterConsent;

        if (Settings.GiveConsent is not { } give || _consent.HasStrictConsent(user, give))
            flags |= WFIntimacyMenuFlags.YourGivingConsent;

        string? sessionAct = null;
        var intensity = WFIntimacyIntensity.Normal;
        var speed = WFIntimacySpeed.Normal;

        if (TryComp<WFIntimacyComponent>(user, out var stats) && stats.Partner == target)
        {
            flags |= WFIntimacyMenuFlags.SessionOpen;
            if (stats.AutoThrust)
                flags |= WFIntimacyMenuFlags.AutoThrust;

            sessionAct = stats.Act?.Id;
            intensity = stats.Intensity;
            speed = stats.Speed;
        }

        // Both bodies' current stats travel with the menu so the window can draw bars.
        byte giverArousal = TryComp<GenitalsComponent>(user, out var giverGenitals) ? giverGenitals.Arousal : (byte)0;
        byte receiverArousal = TryComp<GenitalsComponent>(target, out var receiverGenitals) ? receiverGenitals.Arousal : (byte)0;
        byte giverPleasure = TryComp<WFIntimacyComponent>(user, out var giverStats) ? giverStats.Pleasure : (byte)0;
        byte receiverPleasure = TryComp<WFIntimacyComponent>(target, out var receiverStats) ? receiverStats.Pleasure : (byte)0;
        byte giverWetness = TryComp<WFIntimacyComponent>(user, out var giverW) ? giverW.Wetness : (byte)0;
        byte receiverWetness = TryComp<WFIntimacyComponent>(target, out var receiverW) ? receiverW.Wetness : (byte)0;

        return new WFIntimacyMenuEvent(
            GetNetEntity(target), entries, flags, sessionAct, intensity, speed,
            giverArousal, receiverArousal, giverPleasure, receiverPleasure, giverWetness, receiverWetness);
    }

    private void OnMenuRequest(WFIntimacyMenuRequestEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        OpenMenu(user, GetEntity(ev.Target));
    }

    private void OnStartRequest(WFIntimacyStartRequestEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var target = GetEntity(ev.Target);
        var actId = new ProtoId<IntimacyActPrototype>(ev.ActId);
        if (!_proto.TryIndex(actId, out var act))
            return;

        TryStart(user, target, act, ev.Intensity, ev.Speed);

        // A fresh menu turns into the session view, or lights up the reason the act refused.
        OpenMenu(user, target);
    }

    private void OnStopRequest(WFIntimacyStopRequestEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        EndSessionOf(user, WFIntimacyStopReason.Stopped);
    }
}