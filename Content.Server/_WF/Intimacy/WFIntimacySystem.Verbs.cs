using Content.Shared._WF.CCVar;
using Content.Shared._WF.Genitals;
using Content.Shared._WF.Genitals.Components;
using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;

namespace Content.Server._WF.Intimacy;

// The context menu: one verb that opens the window, and the controls a body has on itself.
public sealed partial class WFIntimacySystem
{
    /// <summary>Context menu category of the interaction verb.</summary>
    public static readonly VerbCategory InteractionsCategory =
        new("wf-verb-category-interactions", "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png");

    /// <summary>Context menu category of the session and clothing controls.</summary>
    public static readonly VerbCategory IntimacySelfCategory =
        new("wf-verb-category-intimacy-self", "/Textures/Interface/VerbIcons/refresh.svg.192dpi.png");

    /// <summary>wf.intimacy_debug: logs the menu, and treats unattended bodies as test subjects.</summary>
    private bool _debug;

    private void InitializeVerbs()
    {
        // Broadcast: the directed (GenitalsComponent, GetVerbsEvent<Verb>) pair belongs to SharedUndergarmentStripSystem.
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
        Subs.CVar(_cfg, WolfgateCVars.IntimacyDebug, value => _debug = value, true);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        if (args.User == args.Target)
        {
            AddClothingVerbs(args);
            AddSessionVerbs(args);
            return;
        }

        // Reach is not required: the window opens at any distance so a wave can carry, and each act refuses itself
        // with its own range. Only the user's own state keeps the verb away.
        if (!args.CanInteract)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("wf-intimacy-verb-interact"),
            Category = InteractionsCategory,
            Act = () =>
            {
                if (_debug)
                    Log.Info($"Intimacy: opened the menu for {ToPrettyString(args.User)} on {ToPrettyString(args.Target)}.");

                OpenMenu(args.User, args.Target);
            },
        });
    }

    /// <summary>Moves clothing aside over a region that is worn, or settles it again.</summary>
    private void AddClothingVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<GenitalsComponent>(args.Target, out var genitals))
            return;

        AddClothingVerb(args, genitals, GenitalRegion.Chest);
        AddClothingVerb(args, genitals, GenitalRegion.Groin);
    }

    private void AddClothingVerb(GetVerbsEvent<Verb> args, GenitalsComponent genitals, GenitalRegion region)
    {
        var uid = args.Target;
        var coverage = _coverage.GetCoverage((uid, genitals, CompOrNull<HumanoidAppearanceComponent>(uid)));
        var open = (genitals.Displaced & region) != 0;
        var worn = (coverage.Clothing & region) != 0;

        if (!worn && !open)
            return;

        var text = region == GenitalRegion.Chest
            ? open ? "wf-intimacy-verb-settle-chest" : "wf-intimacy-verb-displace-chest"
            : open ? "wf-intimacy-verb-settle-groin" : "wf-intimacy-verb-displace-groin";

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(text),
            Category = IntimacySelfCategory,
            Act = () => SetDisplaced(uid, genitals, region, !open),
        });
    }

    /// <summary>Stop, and the automatic cadence while this body is the one giving.</summary>
    private void AddSessionVerbs(GetVerbsEvent<Verb> args)
    {
        var uid = args.Target;
        if (!TryComp<WFIntimacyComponent>(uid, out var stats) || stats.Partner is not { } partner)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("wf-intimacy-verb-stop"),
            Category = IntimacySelfCategory,
            Act = () => EndSessionOf(uid, WFIntimacyStopReason.Stopped),
        });

        if (!stats.Thrusting || stats.Act is not { } actId || !_proto.TryIndex(actId, out var act) || !act.AutoThrustable)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(stats.AutoThrust ? "wf-intimacy-verb-autothrust-off" : "wf-intimacy-verb-autothrust-on"),
            Category = IntimacySelfCategory,
            Act = () => ToggleAutoThrust(uid, partner),
        });
    }
}