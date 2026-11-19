using System.Numerics;
using Content.Client.Shuttles.UI;
using Content.Shared._Mono.ShipGuns;
using Content.Shared._WG.LockOn;
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

namespace Content.Client._WG.LockOn;

/// <summary>
/// Draws the ship\'s lock bracket and target name over a radar plot.
/// </summary>
public sealed class WGLockOnOverlay : Control
{
    private static readonly Color LockColor = Color.FromHex("#ff4a4a");

    private const float BracketRadius = 16f;
    private const float BracketArm = 7f;

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
    private bool _wgGunsUnresolved;

    public WGLockOnOverlay(EntityUid console, ShuttleNavControl radar, Func<IReadOnlyList<NetEntity>> weapons)
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
            !_entities.TryGetComponent(ship, out WGLockOnComponent? lockOn) ||
            lockOn.Target is not { } target ||
            !_entities.EntityExists(target) ||
            !_radar.WgLockOnTryGetWorldToView(out var worldToView, out var map))
            return;

        TransformComponent? targetXform = null;
        _entities.TryGetComponent(target, out targetXform);
        if (targetXform != null && targetXform.MapID == MapId.Nullspace)
            return;
        if (targetXform != null && map != targetXform.MapID)
            return;

        var now = (float)_timing.CurTime.TotalSeconds;
        var aimPoint = lockOn.AimPoint;
        if (lockOn.AimTime > 0f && now - lockOn.AimTime < 0.5f)
            aimPoint += lockOn.AimVelocity * (now - lockOn.AimTime);

        var targetView = Vector2.Transform(aimPoint, worldToView);
        DrawBracket(handle, targetView);

        var targetName = _entities.GetComponent<MetaDataComponent>(target).EntityName;
        var textSize = _font.MeasureString(targetName);
        handle.DrawText(_font, targetName, targetView + new Vector2(0f, -BracketRadius - textSize.Y / 2f), LockColor);

        // Lead crosses for each selected gun.
        _guns.Clear();
        var availableWeapons = Weapons();
        foreach (var net in availableWeapons)
        {
            if (!_entities.TryGetEntity(net, out var weapon))
                continue;
            if (!_entities.TryGetComponent(weapon, out GunComponent? gun) ||
                !_entities.TryGetComponent(weapon, out ShipGunTypeComponent? type))
            {
                _wgGunsUnresolved = true;
                continue;
            }
            if (type.Type == ShipGunType.Missile)
                continue;
            var speed = _entities.HasComponent<HitscanBatteryAmmoProviderComponent>(weapon)
                ? 0f
                : gun.ProjectileSpeedModified > 0f ? gun.ProjectileSpeedModified : gun.ProjectileSpeed;
            _guns.Add((weapon, speed));
        }

        _selection.Clear();
        _selection.AddRange(availableWeapons);
        if (!SameSelection(availableWeapons))
        {
            _leads.Clear();
            _selection.Clear();
            _selection.AddRange(availableWeapons);
        }

        foreach (var (weapon, speed) in _guns)
        {
            var weaponXform = _entities.GetComponent<TransformComponent>(weapon);
            var muzzle = _transform.GetWorldPosition(weaponXform);
            var lead = WGLockOnMath.Solve(muzzle, aimPoint, lockOn.AimVelocity, speed);
            if (!lead.Valid) continue;
            var leadView = Vector2.Transform(lead.Point, worldToView);
            if (_leads.Count >= MaxGuns) break;
            if (NearbyLead(leadView)) continue;
            _leads.Add(leadView);
            DrawLeadCross(handle, leadView, LeadColor);
        }

        if (_config.GetCVar(WGLockOnCVars.DebugLead.Name))
        {
            foreach (var (weapon, speed) in _guns)
            {
                var weaponXform = _entities.GetComponent<TransformComponent>(weapon);
                var muzzle = _transform.GetWorldPosition(weaponXform);
                var lead = WGLockOnMath.Solve(muzzle, aimPoint, lockOn.AimVelocity, speed);
                if (!lead.Valid) continue;
                var debug = $"T {lead.Time:F2}s Vt {target} Vg {muzzle} S {speed:F0} L {(lead.Point - aimPoint).Length():F0}m";
                handle.DrawText(_font, debug, targetView + new Vector2(0f, BracketRadius + 2f), LockColor);
            }
        }
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

    private static void DrawLeadCross(DrawingHandleScreen handle, Vector2 point, Color color)
    {
        handle.DrawLine(new Vector2(point.X - CrossArm, point.Y), new Vector2(point.X + CrossArm, point.Y), color);
        handle.DrawLine(new Vector2(point.X, point.Y - CrossArm), new Vector2(point.X, point.Y + CrossArm), color);
    }
}
