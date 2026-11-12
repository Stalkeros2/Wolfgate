using Content.Client._WF.Intimacy.UI;
using Content.Shared._WF.Intimacy;
using Content.Shared._WF.Intimacy.Events;
using Content.Shared._WF.Intimacy.Prototypes;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._WF.Intimacy;

/// <summary>
/// The interaction window: opened by the menu the server builds, and answered with requests the server validates.
/// </summary>
public sealed class ClientIntimacySystem : EntitySystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;

    private WFInteractionWindow? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<WFIntimacyMenuEvent>(OnMenu);
    }

    private void OnMenu(WFIntimacyMenuEvent menu)
    {
        if (_window is null)
        {
            _window = new WFInteractionWindow();
            _ui.WindowRoot.AddChild(_window);
            _window.OnStart += OnStart;
            _window.OnRefresh += OnRefresh;
            _window.OnStop += OnStop;
        }

        _window.Apply(menu);

        if (!_window.IsOpen)
            _window.OpenCentered();
    }

    private void OnStart(ProtoId<IntimacyActPrototype> act, WFIntimacyIntensity intensity, WFIntimacySpeed speed)
    {
        if (_window is null)
            return;

        RaisePredictiveEvent(new WFIntimacyStartRequestEvent(_window.Target, act.Id, intensity, speed));
    }

    private void OnRefresh()
    {
        if (_window is null)
            return;

        RaisePredictiveEvent(new WFIntimacyMenuRequestEvent(_window.Target));
    }

    private void OnStop()
    {
        RaisePredictiveEvent(new WFIntimacyStopRequestEvent());
    }
}