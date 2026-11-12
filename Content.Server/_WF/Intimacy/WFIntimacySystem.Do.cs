using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Components;
using Content.Shared._WF.Intimacy.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;

namespace Content.Server._WF.Intimacy;

// Starting a motion: checks, session, DoAfter, then one pulse when the interval elapses.
public sealed partial class WFIntimacySystem
{
    private void TryStart(EntityUid user, EntityUid target, IntimacyActPrototype act,
        WFIntimacyIntensity intensity = WFIntimacyIntensity.Normal, WFIntimacySpeed speed = WFIntimacySpeed.Normal)
    {
        if (GetFailure(user, target, act) is { } failure)
        {
            _popup.PopupEntity(Loc.GetString(failure), user, user);
            return;
        }

        if (act.Kind == WFIntimacyKind.Platonic)
        {
            DoPlatonic(user, target, act);
            return;
        }

        if (!EnsureSession(user, target, act))
            return;

        // Both sides remember the pace so the automatic cadence and the window agree.
        if (TryComp<WFIntimacyComponent>(user, out var giverStats))
            SetPace(giverStats, intensity, speed);

        if (TryComp<WFIntimacyComponent>(target, out var receiverStats))
            SetPace(receiverStats, intensity, speed);

        var ev = new WFIntimacyActDoAfterEvent { Act = act.ID };
        var args = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(WFIntimacyMath.Interval(act, speed)), ev, target, target)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = act.Kind is WFIntimacyKind.Manual or WFIntimacyKind.Penetrative,
            RequireCanInteract = false,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        var actName = Loc.GetString(act.Name);
        _popup.PopupEntity(
            Loc.GetString("wf-intimacy-start-user", ("act", actName), ("target", Identity.Entity(target, EntityManager))),
            user, user);

        _popup.PopupEntity(
            Loc.GetString("wf-intimacy-start-target", ("act", actName), ("user", Identity.Entity(user, EntityManager))),
            target, target, PopupType.Medium);
    }

    /// <summary>Platonic acts need no session: a line each way and nothing else.</summary>
    private void DoPlatonic(EntityUid user, EntityUid target, IntimacyActPrototype act)
    {
        var actName = Loc.GetString(act.Name);

        _popup.PopupEntity(
            Loc.GetString("wf-intimacy-act-user", ("act", actName), ("target", Identity.Entity(target, EntityManager))),
            user, user);

        _popup.PopupEntity(
            Loc.GetString("wf-intimacy-act-target", ("act", actName), ("user", Identity.Entity(user, EntityManager))),
            target, target, PopupType.Medium);
    }

    /// <summary>Every tick: cancels once the act stops being allowed.</summary>
    private void OnActAttempt(Entity<WFIntimacyComponent> ent, ref DoAfterAttemptEvent<WFIntimacyActDoAfterEvent> args)
    {
        if (!_proto.TryIndex(args.Event.Act, out var act) || GetFailure(args.DoAfter.Args.User, ent.Owner, act) is not null)
            args.Cancel();
    }

    /// <summary>One motion, then the session's idle clock restarts.</summary>
    private void OnActDoAfter(Entity<WFIntimacyComponent> ent, ref WFIntimacyActDoAfterEvent ev)
    {
        if (ev.Cancelled || !_proto.TryIndex(ev.Act, out var act))
            return;

        var user = ev.Args.User;
        if (GetFailure(user, ent.Owner, act) is not null)
            return;

        ApplyPulse(user, ent.Owner, act);
        RefreshSession(user, ent.Owner, act);
    }
}