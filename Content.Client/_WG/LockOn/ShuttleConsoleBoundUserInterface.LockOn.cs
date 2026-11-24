using System.Numerics;
using Content.Client.Shuttles.UI;
using Content.Client._WG.LockOn;
using Content.Shared._WG.LockOn;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Content.Client.Shuttles.BUI;

public sealed partial class ShuttleConsoleBoundUserInterface
{
    private IInputManager? _wgLockInput;
    private IUserInterfaceManager? _wgLockUi;
    private ShuttleNavControl? _wgLockRadar;
    private WGLockOnOverlay? _wgLockOverlay;

    /// <summary>Adds the lock overlay to the helm radar and routes lock clicks to the server.</summary>
    private void WgLockOnOpen()
    {
        if (_window == null)
            return;

        _wgLockRadar = _window.FindControl<NavScreen>("NavContainer")?.FindControl<ShuttleNavControl>("NavRadar");
        if (_wgLockRadar == null)
            return;

        _wgLockRadar.OnRadarClick += WgLockOnClick;
        _wgLockUi = IoCManager.Resolve<IUserInterfaceManager>();
        _wgLockInput = IoCManager.Resolve<IInputManager>();
        _wgLockInput.FirstChanceOnKeyEvent += WgLockOnKeyEvent;
        _window.OnClose += WgLockOnClose;

        _wgLockOverlay = new WGLockOnOverlay(Owner!, _wgLockRadar);
        _wgLockRadar.AddChild(_wgLockOverlay);
    }

    /// <summary>Releases the input hook when the helm window closes.</summary>
    private void WgLockOnClose()
    {
        if (_wgLockInput != null)
            _wgLockInput.FirstChanceOnKeyEvent -= WgLockOnKeyEvent;
        _wgLockInput = null;
        _wgLockUi = null;
        _wgLockRadar?.OnRadarClick -= WgLockOnClick;
        _wgLockOverlay?.Parent?.RemoveChild(_wgLockOverlay);
        _wgLockOverlay = null;
        _wgLockRadar = null;
    }

    /// <summary>Middle click always locks; the helm has no guns, so left click locks on release.</summary>
    private void WgLockOnKeyEvent(KeyEventArgs args, KeyEventType type)
    {
        if (args.Key != Keyboard.Key.MouseMiddle || type != KeyEventType.Down || args.Handled)
            return;

        if (!WgLockOnHovered() || _wgLockUi?.KeyboardFocused != null)
            return;

        WgLockOnSend(_wgLockUi!.MousePositionScaled.Position - _wgLockRadar!.GlobalPosition);
        args.Handle();
    }

    private bool WgLockOnHovered()
    {
        if (_wgLockRadar == null || !_wgLockRadar.VisibleInTree)
            return false;

        var pointer = _wgLockUi!.MousePositionScaled;
        return pointer.IsValid &&
               pointer.Window == _wgLockRadar.Window?.Id &&
               _wgLockUi.MouseGetControl(pointer) == _wgLockRadar;
    }

    private void WgLockOnClick(EntityCoordinates coordinates)
    {
        if (_wgLockRadar == null || _wgLockRadar.WgLockOnMouseDown())
            return;

        WgLockOnRequest(coordinates);
    }

    private void WgLockOnSend(Vector2 relativePosition)
    {
        if (_wgLockRadar == null)
            return;

        var coordinates = _wgLockRadar.WgLockOnCoordinates(relativePosition);
        if (!coordinates.IsValid(EntMan))
            return;

        WgLockOnRequest(coordinates);
    }

    /// <summary>Asks the server to lock the contact nearest the cursor inside the radar's lens.</summary>
    private void WgLockOnRequest(EntityCoordinates coordinates)
    {
        if (_wgLockRadar == null)
            return;

        SendMessage(new WGLockOnRequestMessage(EntMan.GetNetCoordinates(coordinates), _wgLockRadar.WgLockOnPickRadius));
    }
}
