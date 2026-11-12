using System.Numerics;
using Content.Client.Shuttles.UI;
using Content.Shared._Mono.ShipGuns;
using Content.Shared._WF.LockOn;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Client._WF.LockOn;

/// <summary>
/// Draws the ship's lock bracket, the target's name and a lead cross for every selected gun over a radar
/// plot. It reads the networked <see cref="WFLockOnComponent"/> from the console's ship, so every console
/// aboard shows the same firing solution.
/// </summary>
public sealed class WFLockOnOverlay : Control
{
    private static readonly Color LockColor = Color.FromHex("#ff4a4a");
    private static readonly Color LeadColor = Color.FromHex("#ffd23f");

    private const float BracketRadius = 16f;
    private const float BracketArm = 7f;
    private const float CrossArm = 5f;
    private const float DedupeRadius = 3f;
    private const int MaxGuns = 64;
    private const float MaxExtrapolation = 0.5f;

    private readonly EntityUid _console;
    private readonly ShuttleNavControl _radar;
    private readonly Func<IReadOnlyList<NetEntity>> _weapons;
    private readonly IEntityManager _entities;
    private readonly SharedTransformSystem _transform;
    private readonly SharedPhysicsSystem _physics;
    private readonly IGameTiming _timing;
    private readonly IConfigurationManager _config;
    private readonly Font _font;

    private readonly List<(EntityUid Weapon, float Speed)> _guns = new();
    private readonly List<NetEntity> _selection = new();
    private readonly List<Vector2> _leads = new();

    public WFLockOnOverlay(EntityUid console, ShuttleNavControl radar, Func<IReadOnlyList<NetEntity>> weapons)
    {
        _console = console;
        _radar = radar;
        _weapons = weapons;
        _entities = IoCManager.Resolve<IEntityManager>();
        _transform = _entities.System<SharedTransformSystem>();
        _physics = _entities.System<SharedPhysicsSystem>();
        _timing = IoCManager.Resolve<IGameTiming>();
        _config = IoCManager.Resolve<IConfigurationManager>();
        _font = new VectorFont(
            IoCManager.Resolve<IResourceCache>().GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 12);
        MouseFilter = MouseFilterMode.Ignore;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!_entities.TryGetComponent(_console, out TransformComponent? consoleXform) ||
            consoleXform.GridUid is not { } ship ||
            !_entities.TryGetComponent(ship, out WFLockOnComponent? lockOn) ||
            lockOn.Target is not { } target ||
            !_entities.EntityExists(target) ||
            !_entities.TryGetComponent(target, out TransformComponent? targetXform) ||
            targetXform.MapID == MapId.Nullspace ||
            !_radar.WfLockOnTryGetWorldToView(out var worldToView, out var map) ||
            map != targetXform.MapID)
            return;

        // The server measures the clicked hull point; extrapolating it from the last measurement keeps the cross
        // smooth between refreshes without trusting the client's physics for someone else's grid.
        var targetWorld = lockOn.AimTime > 0f
            ? lockOn.AimPoint + lockOn.AimVelocity * Extrapolation(lockOn)
            : _transform.GetWorldPosition(targetXform);
        var targetView = Vector2.Transform(targetWorld, worldToView);
        DrawBracket(handle, targetView);

        if (_entities.TryGetComponent(target, out MetaDataComponent? meta) && !string.IsNullOrEmpty(meta.EntityName))
            handle.DrawString(_font, new Vector2(8, 8), Loc.GetString("wf-lockon-status", ("target", meta.EntityName)), LockColor);

        RefreshGuns();
        if (_guns.Count == 0)
            return;

        _leads.Clear();
        var debug = _config.GetCVar(WFLockOnCVars.DebugLead);

        foreach (var (weapon, speed) in _guns)
        {
            if (!_entities.TryGetComponent(weapon, out TransformComponent? weaponXform) ||
                weaponXform.MapID != targetXform.MapID)
                continue;

            // A shot leaves with the turn of the grid it is mounted on, as GunSystem computes it.
            var mount = weaponXform.GridUid ?? ship;
            var muzzle = _transform.GetWorldPosition(weaponXform);
            var muzzleVelocity = _physics.GetMapLinearVelocity(weapon) - _physics.GetMapLinearVelocity(mount);

            var lead = WFLockOnMath.Solve(muzzle, targetWorld, lockOn.AimVelocity - muzzleVelocity, speed);
            if (!lead.Valid)
                continue;

            var leadView = Vector2.Transform(lead.Point, worldToView);
            if (NearbyLead(leadView))
                continue;

            _leads.Add(leadView);
            handle.DrawLine(targetView, leadView, LeadColor.WithAlpha(0.45f));
            handle.DrawLine(leadView - new Vector2(CrossArm, 0), leadView + new Vector2(CrossArm, 0), LeadColor);
            handle.DrawLine(leadView - new Vector2(0, CrossArm), leadView + new Vector2(0, CrossArm), LeadColor);

            if (debug)
            {
                handle.DrawString(_font, leadView + new Vector2(CrossArm, -14f), Loc.GetString("wf-lockon-debug",
                    ("time", $"{lead.Time:0.0}"),
                    ("target", $"{lockOn.AimVelocity.Length():0}"),
                    ("muzzle", $"{muzzleVelocity.Length():0}"),
                    ("speed", $"{speed:0}"),
                    ("lead", $"{(lead.Point - targetWorld).Length():0}")), LeadColor);
            }
        }
    }

    /// <summary>Seconds since the server measured the aim point, capped so a stale lock never runs away.</summary>
    private float Extrapolation(WFLockOnComponent lockOn)
    {
        var elapsed = (float) _timing.CurTime.TotalSeconds - lockOn.AimTime;
        return Math.Clamp(elapsed, 0f, MaxExtrapolation);
    }

    /// <summary>Rebuilds the gun list when the selection changes; gun speeds come from the networked component.</summary>
    private void RefreshGuns()
    {
        var weapons = Weapons();
        if (SameSelection(weapons))
            return;

        _selection.Clear();
        _guns.Clear();
        for (var i = 0; i < weapons.Count && i < MaxGuns; i++)
        {
            var net = weapons[i];
            _selection.Add(net);
            if (!_entities.TryGetEntity(net, out var entity) ||
                !_entities.TryGetComponent(entity.Value, out GunComponent? gun) ||
                !_entities.TryGetComponent(entity.Value, out ShipGunTypeComponent? type) ||
                type.Type == ShipGunType.Missile)
                continue;

            var speed = _entities.HasComponent<HitscanBatteryAmmoProviderComponent>(entity.Value)
                ? 0f
                : gun.ProjectileSpeedModified > 0f ? gun.ProjectileSpeedModified : gun.ProjectileSpeed;
            _guns.Add((entity.Value, speed));
        }
    }

    /// <summary>The console's own selection, falling back to the cockpit's linked guns when the helm has none.</summary>
    private IReadOnlyList<NetEntity> Weapons()
    {
        var selected = _weapons();
        if (selected.Count > 0)
            return selected;
        return WFLockOnClientState.CockpitWeapons?.Invoke() ?? Array.Empty<NetEntity>();
    }

    private bool SameSelection(IReadOnlyList<NetEntity> weapons)
    {
        if (weapons.Count != _selection.Count)
            return false;
        for (var i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != _selection[i])
                return false;
        }
        return true;
    }

    private bool NearbyLead(Vector2 point)
    {
        foreach (var existing in _leads)
        {
            if ((existing - point).LengthSquared() < DedupeRadius * DedupeRadius)
                return true;
        }
        return false;
    }

    private static void DrawBracket(DrawingHandleScreen handle, Vector2 point)
    {
        var left = point.X - BracketRadius;
        var right = point.X + BracketRadius;
        var top = point.Y - BracketRadius;
        var bottom = point.Y + BracketRadius;

        handle.DrawLine(new Vector2(left, top), new Vector2(left + BracketArm, top), LockColor);
        handle.DrawLine(new Vector2(left, top), new Vector2(left, top + BracketArm), LockColor);
        handle.DrawLine(new Vector2(right, top), new Vector2(right - BracketArm, top), LockColor);
        handle.DrawLine(new Vector2(right, top), new Vector2(right, top + BracketArm), LockColor);
        handle.DrawLine(new Vector2(left, bottom), new Vector2(left + BracketArm, bottom), LockColor);
        handle.DrawLine(new Vector2(left, bottom), new Vector2(left, bottom - BracketArm), LockColor);
        handle.DrawLine(new Vector2(right, bottom), new Vector2(right - BracketArm, bottom), LockColor);
        handle.DrawLine(new Vector2(right, bottom), new Vector2(right, bottom - BracketArm), LockColor);
        handle.DrawCircle(point, 2f, LockColor);
    }
}
