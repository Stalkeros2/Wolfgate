using System.Numerics;
using Content.Client._WG.LockOn;
using Content.Shared._WG.LockOn;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Content.Client._Mono.FireControl.UI;

public sealed partial class FireControlConsoleBoundUserInterface
{
    private IInputManager? _wgLockInput;
    private IUserInterfaceManager? _wgLockUi;
    private WGLockOnOverlay? _wgLockOverlay;

    /// <summary>Adds the lock overlay to the gunnery radar and routes lock clicks to the server.</summary>
    private void WgLockOnOpen()
    {
        if (_window == null)
            return;

        _window.Radar.OnRadarClick += WgLockOnClick;
        _wgLockUi = IoCManager.Resolve<IUserInterfaceManager>();
        _wgLockInput = IoCManager.Resolve<IInputManager>();
        _wgLockInput.FirstChanceOnKeyEvent += WgLockOnKeyEvent;
        _window.OnClose += WgLockOnClose;

        _wgLockOverlay = new WGLockOnOverlay(Owner!, _window.Radar);
        _window.Radar.AddChild(_wgLockOverlay);
    }

    /// <summary>Releases the input hook when the gunnery window closes.</summary>
    private void WgLockOnClose()
    {
        if (_wgLockInput != null)
            _wgLockInput.FirstChanceOnKeyEvent -= WgLockOnKeyEvent;
        _wgLockInput = null;
        _wgLockUi = null;
        _wgLockOverlay?.Parent?.RemoveChild(_wgLockOverlay);
        _wgLockOverlay = null;
        if (_window != null)
            _window.Radar.OnRadarClick -= WgLockOnClick;
    }

    /// <summary>Middle click always locks; with no weapon selected, left click locks on release instead of firing.</summary>
    private void WgLockOnKeyEvent(KeyEventArgs args, KeyEventType type)
    {
        if (args.Key != Keyboard.Key.MouseMiddle || type != KeyEventType.Down || args.Handled || _window == null)
            return;

        if (!WgLockOnHovered() || _wgLockUi?.KeyboardFocused != null)
            return;

        WgLockOnSend(_wgLockUi!.MousePositionScaled.Position - _window.Radar.GlobalPosition);
        args.Handle();
    }

    private bool WgLockOnHovered()
    {
        if (_window == null || !_window.Radar.VisibleInTree)
            return false;

        var pointer = _wgLockUi!.MousePositionScaled;
        return pointer.IsValid &&
               pointer.Window == _window.Radar.Window?.Id &&
               _wgLockUi.MouseGetControl(pointer) == _window.Radar;
    }

    private void WgLockOnClick(EntityCoordinates coordinates)
    {
        if (_window == null || _window.Radar.WgLockOnMouseDown())
            return;

        WgLockOnRequest(coordinates);
    }

    private void WgLockOnSend(Vector2 relativePosition)
    {
        if (_window == null)
            return;

        var coordinates = _window.Radar.WgLockOnCoordinates(relativePosition);
        if (!coordinates.IsValid(EntMan))
            return;

        WgLockOnRequest(coordinates);
    }

    /// <summary>Asks the server to lock the contact nearest the cursor inside the radar's lens.</summary>
    private void WgLockOnRequest(EntityCoordinates coordinates)
    {
        if (_window == null)
            return;

        SendMessage(new WGLockOnRequestMessage(EntMan.GetNetCoordinates(coordinates), _window.Radar.WgLockOnPickRadius));
    }
}
