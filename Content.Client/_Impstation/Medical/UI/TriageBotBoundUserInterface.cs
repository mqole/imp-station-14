using Content.Shared._Impstation.Medical.UI;

namespace Content.Client._Impstation.Medical.UI;

public sealed class TriageBotBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TriageBotMenu? _menu;

    public TriageBotBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new();
        _menu.OnModeChanged += ModeChanged;

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    public void ModeChanged(string mode)
    {
        if (_menu != null)
        {
            _menu.CurrentMode = mode;
            SendMessage(new TriageBotModeChangedMessage(mode));
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not TriageBotInterfaceState triageBotState)
            return;

        if (_menu != null)
            _menu.CurrentMode = triageBotState.CurrentMode;
    }
}
