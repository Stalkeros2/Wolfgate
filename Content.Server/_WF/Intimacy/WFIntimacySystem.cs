using Content.Server._WF.Genitals;
using Content.Shared._WF.Genitals.Components;
using Content.Shared._WF.Genitals.Events;
using Content.Shared._WF.Genitals.Systems;
using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Components;
using Content.Shared._WF.Intimacy.Prototypes;
using Content.Shared.Administration.Logs;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._WF.Intimacy;

/// <summary>Sessions, drift and every gate an intimate act has to pass.</summary>
public sealed partial class WFIntimacySystem : EntitySystem
{
    /// <summary>Seconds between drift updates.</summary>
    private const float TickInterval = 1f;

    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedGenitalsSystem _genitals = default!;
    [Dependency] private GenitalOrganSystem _organs = default!;
    [Dependency] private SharedArousalSystem _arousal = default!;
    [Dependency] private GenitalConsentSystem _consent = default!;
    [Dependency] private GenitalCoverageSystem _coverage = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;

    private float _accumulator;

    /// <summary>The WFIntimacyDefault settings prototype.</summary>
    public IntimacySettingsPrototype Settings => _proto.Index(IntimacySettingsPrototype.DefaultId);

    public override void Initialize()
    {
        base.Initialize();

        InitializeVerbs();
        InitializeMenu();

        SubscribeLocalEvent<WFIntimacyComponent, ComponentGetStateAttemptEvent>(OnGetStateAttempt);
        SubscribeLocalEvent<WFIntimacyComponent, WFIntimacyActDoAfterEvent>(OnActDoAfter);
        SubscribeLocalEvent<WFIntimacyComponent, DoAfterAttemptEvent<WFIntimacyActDoAfterEvent>>(OnActAttempt);
        SubscribeLocalEvent<WFIntimacyComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<GenitalConsentChangedEvent>(OnConsentChanged);
    }

    /// <summary>Pleasure and lubrication reach only the body's own player.</summary>
    private void OnGetStateAttempt(Entity<WFIntimacyComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player != null && args.Player.AttachedEntity != ent.Owner)
            args.Cancelled = true;
    }

    /// <summary>Death ends whatever the body was in.</summary>
    private void OnMobStateChanged(Entity<WFIntimacyComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            EndSessionOf(ent.Owner, WFIntimacyStopReason.Dead);
    }

    /// <summary>A body that lost the master switch drops out of everything.</summary>
    private void OnConsentChanged(ref GenitalConsentChangedEvent ev)
    {
        if (!_consent.HasMaster(ev.Body))
            EndSessionOf(ev.Body, WFIntimacyStopReason.ConsentRevoked);
    }
}