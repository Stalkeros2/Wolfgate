using Content.Server.Chat.Systems;
using Content.Server._WG.ShipPa;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server._WG.LockOn;

public sealed partial class WGLockOnSystem
{
    /// <summary>Least time between two lock warnings on the same ship.</summary>
    public static readonly TimeSpan AlertCooldown = TimeSpan.FromSeconds(10);

    /// <summary>The warning chime, taken from the console's alarm until a dedicated one is written.</summary>
    public static readonly SoundPathSpecifier WarningSound = new("/Audio/_WG/CombatConsole/Retro/warning.ogg");

    /// <summary>Whether <see cref="WarningSound"/> can be measured, checked once so a bad asset warns only once.</summary>
    private static bool? _warningSoundLoads;

    private static readonly Color LockColor = Color.FromHex("#ff3030");

    /// <summary>
    /// Warns a ship that something has a firing solution on it, through its PA when it has working speakers and
    /// in its crew's chat when it does not.
    /// </summary>
    private void Alert(EntityUid targetGrid, EntityUid lockerGrid)
    {
        if (targetGrid == lockerGrid || TerminatingOrDeleted(targetGrid) || TerminatingOrDeleted(lockerGrid))
            return;

        var alert = EnsureComp<WGLockOnAlertComponent>(targetGrid);
        if (alert.NextAlert > _timing.CurTime)
            return;
        alert.NextAlert = _timing.CurTime + AlertCooldown;

        var message = Loc.GetString("wg-lockon-alert", ("ship", _pa.GetShipName(lockerGrid)));
        var sender = Loc.GetString("wg-lockon-alert-sender");
        if (!_pa.Announce(targetGrid, message, WarningSound, sender, LockColor))
        {
            var crew = Filter.Empty().AddWhereAttachedEntity(entity => Transform(entity).GridUid == targetGrid);
            _chat.DispatchFilteredAnnouncement(crew, message, source: targetGrid, sender: sender,
                playSound: false, announcementSound: null, colorOverride: LockColor);
            PlayWarning(crew);
        }

        var ev = new WGLockOnAlertEvent(targetGrid, lockerGrid);
        RaiseLocalEvent(ref ev);
    }

    /// <summary>
    /// Plays the warning chime for a ship with no working speakers. The audio system throws for a sound it cannot
    /// measure, and that throw would escape the lock request and take down a server built without exception
    /// tolerance, so an unreadable asset is logged once and the chime skipped.
    /// </summary>
    private void PlayWarning(Filter crew)
    {
        var sound = _audio.ResolveSound(WarningSound);
        if (_warningSoundLoads == null)
        {
            try
            {
                _audio.GetAudioLength(sound);
                _warningSoundLoads = true;
            }
            catch (Exception e)
            {
                _warningSoundLoads = false;
                Log.Warning($"Couldn't load the lock warning sound {WarningSound.Path}: {e.Message}");
            }
        }

        if (_warningSoundLoads != true)
            return;

        _audio.PlayGlobal(sound, crew, true, AudioParams.Default.WithVolume(-2f));
    }
}

/// <summary>Raised when a ship is warned that another ship has locked it.</summary>
[ByRefEvent]
public readonly record struct WGLockOnAlertEvent(EntityUid TargetGrid, EntityUid LockerGrid);