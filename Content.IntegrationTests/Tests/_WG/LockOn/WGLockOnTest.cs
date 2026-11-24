#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Server._Mono.Projectiles.TargetSeeking;
using Content.Server._WG.LockOn;
using Content.Server._WG.ShipMissiles;
using Content.Server._WG.ShipPa;
using Content.Shared._WG.LockOn;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._WG.LockOn;

/// <summary>
/// The ship's lock: a radar click resolves to a contact, the whole hull shares one lock, the locked ship is
/// warned, and missiles fired while locked seek that target.
/// </summary>
[TestFixture]
public sealed class WGLockOnTest
{
    [Test]
    public async Task ResolvesClicksToTheRightTarget()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var em = server.EntMan;
        var xform = em.System<SharedTransformSystem>();
        var maps = em.System<SharedMapSystem>();
        var lockOn = em.System<WGLockOnSystem>();
        var ship = map.Grid.Owner;

        await server.WaitAssertion(() =>
        {
            var console = em.SpawnEntity(null, map.GridCoords);
            var secondConsole = em.SpawnEntity(null, map.GridCoords);
            var other = AddContact(maps, xform, map.MapId, map.Tile.Tile, new Vector2(50f, 50f));
            var wall = em.SpawnEntity("WallSolid", new EntityCoordinates(other, 0.5f, 0.5f));
            var point = xform.GetWorldPosition(em.GetComponent<TransformComponent>(wall));

            Assert.That(lockOn.RequestLock(console, console, Net(em, xform, map.MapId, point)), Is.True);
            Assert.That(em.GetComponent<WGLockOnComponent>(ship).Target, Is.EqualTo(other),
                "A click on a hull must lock its grid, not the wall under the cursor.");
            Assert.That(em.GetComponent<TransformComponent>(secondConsole).GridUid, Is.EqualTo(ship),
                "Both consoles must sit on the same hull.");
            Assert.That(em.HasComponent<WGLockOnComponent>(console), Is.False,
                "The lock belongs to the ship, so every console reads the same one.");

            // A free entity on the clicked tile wins over the grid beneath it.
            var free = em.SpawnEntity(null, new MapCoordinates(point, map.MapId));
            Assert.That(lockOn.RequestLock(console, console, Net(em, xform, map.MapId, point)), Is.True);
            Assert.That(lockOn.GetTarget(ship), Is.EqualTo(free), "Debris on the tile must lock as itself.");

            // Clicking the ship's own hull clears the lock instead of locking it.
            var own = xform.GetWorldPosition(em.GetComponent<TransformComponent>(console));
            Assert.That(lockOn.RequestLock(console, console, Net(em, xform, map.MapId, own)), Is.True);
            Assert.That(lockOn.GetTarget(ship), Is.Null, "A ship can never lock itself.");

            // Nothing under the click clears it too.
            Assert.That(lockOn.RequestLock(console, console, Net(em, xform, map.MapId, new Vector2(-500f, -500f))), Is.True);
            Assert.That(lockOn.GetTarget(ship), Is.Null);
        });

        await pair.CleanReturnAsync();
    }
    /// <summary>The locked ship is warned once, and a re-lock inside the cooldown stays quiet.</summary>
    [Test]
    public async Task WarnsTheLockedShipOncePerCooldown()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var em = server.EntMan;
        var xform = em.System<SharedTransformSystem>();
        var maps = em.System<SharedMapSystem>();
        var lockOn = em.System<WGLockOnSystem>();
        var ship = map.Grid.Owner;

        await server.WaitAssertion(() =>
        {
            var console = em.SpawnEntity(null, map.GridCoords);
            var other = AddContact(maps, xform, map.MapId, map.Tile.Tile, new Vector2(50f, 50f));
            var net = Net(em, xform, map.MapId, xform.GetWorldPosition(em.GetComponent<TransformComponent>(other)));

            Assert.That(em.System<ShipPaSystem>().CountSpeakers(other).Online, Is.Zero,
                "A contact with no speakers must warn through the chat-and-chime fallback, not the PA.");

            Assert.That(lockOn.RequestLock(console, console, net), Is.True);
            var alert = em.GetComponent<WGLockOnAlertComponent>(other);
            var first = alert.NextAlert;
            Assert.That(first, Is.GreaterThan(TimeSpan.Zero), "The locked ship must have been warned.");

            lockOn.ClearLock(ship);
            Assert.That(lockOn.RequestLock(console, console, net), Is.True);
            Assert.That(em.GetComponent<WGLockOnAlertComponent>(other).NextAlert, Is.EqualTo(first),
                "A gunner re-locking the same ship must not re-announce inside the cooldown.");
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// The alert's chime must be a shipped asset the audio system can measure: the fallback plays it through
    /// PlayGlobal, which throws for a sound it cannot read, and that throw escapes the lock request and kills a
    /// server built without exception tolerance. A prefix move that leaves the old path behind fails here instead.
    /// </summary>
    [Test]
    public async Task TheWarningSoundIsShipped()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var em = server.EntMan;

        await server.WaitAssertion(() =>
        {
            var path = WGLockOnSystem.WarningSound.Path;
            Assert.That(server.ResolveDependency<IResourceManager>().ContentFileExists(path), Is.True,
                $"The lock warning {path} must exist under Resources.");

            var audio = em.System<SharedAudioSystem>();
            var sound = audio.ResolveSound(WGLockOnSystem.WarningSound);
            Assert.DoesNotThrow(() =>
            {
                audio.GetAudioLength(sound);
            }, "PlayGlobal measures the sound before it plays, so the alert needs a readable asset.");
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>A missile handed the lock seeks the locked grid before its first guidance tick.</summary>
    [Test]
    public async Task AMissileFiredUnderLockSeeksTheTarget()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var em = server.EntMan;
        var xform = em.System<SharedTransformSystem>();
        var maps = em.System<SharedMapSystem>();
        var lockOn = em.System<WGLockOnSystem>();

        await server.WaitAssertion(() =>
        {
            var console = em.SpawnEntity(null, map.GridCoords);
            var other = AddContact(maps, xform, map.MapId, map.Tile.Tile, new Vector2(50f, 50f));
            var wall = em.SpawnEntity("WallSolid", new EntityCoordinates(other, 0.5f, 0.5f));
            var point = xform.GetWorldPosition(em.GetComponent<TransformComponent>(wall));
            Assert.That(lockOn.RequestLock(console, console, Net(em, xform, map.MapId, point)), Is.True);

            var gun = em.SpawnEntity(null, map.GridCoords);
            em.AddComponent<GunComponent>(gun);
            var gunXform = em.GetComponent<TransformComponent>(gun);
            var gunComp = em.GetComponent<GunComponent>(gun);
            gunComp.ShootCoordinates = gunXform.Coordinates;
            var shot = em.AddComponent<WGLockOnShotComponent>(gun);
            shot.Target = other;

            var missile = em.SpawnEntity(null, map.GridCoords);
            em.AddComponent<TargetSeekingComponent>(missile);
            em.AddComponent<WGMissileFlightComponent>(missile);

            em.EventBus.RaiseLocalEvent(gun, new AmmoShotEvent
            {
                FiredProjectiles = new List<EntityUid> { missile },
            });

            Assert.That(em.GetComponent<TargetSeekingComponent>(missile).CurrentTarget, Is.EqualTo(other),
                "A missile fired under the ship's lock must seek that target before its first tick.");
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>Creates a one-tile grid at a world position, as a rock or another hull would sit.</summary>
    private static EntityUid AddContact(SharedMapSystem maps, SharedTransformSystem xform,
        MapId mapId, Tile tile, Vector2 position)
    {
        var grid = maps.CreateGridEntity(mapId);
        maps.SetTile(grid, Vector2i.Zero, tile);
        xform.SetWorldPosition(grid.Owner, position);
        return grid.Owner;
    }

    private static NetCoordinates Net(IEntityManager em, SharedTransformSystem xform, MapId mapId, Vector2 point)
    {
        return em.GetNetCoordinates(xform.ToCoordinates(new MapCoordinates(point, mapId)));
    }
}