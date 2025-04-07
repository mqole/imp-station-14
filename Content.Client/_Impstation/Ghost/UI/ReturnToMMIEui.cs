using Content.Client.Eui;
using Content.Shared._Impstation.Ghost;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._Impstation.Ghost.UI;

[UsedImplicitly]
public sealed class ReturnToMMIEui : BaseEui
{
    private readonly ReturnToMMIMenu _menu;

    public ReturnToMMIEui()
    {
        _menu = new ReturnToMMIMenu();

        _menu.DenyButton.OnPressed += _ =>
        {
            SendMessage(new ReturnToMMIMessage(false));
            _menu.Close();
        };

        _menu.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new ReturnToMMIMessage(true));
            _menu.Close();
        };
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        SendMessage(new ReturnToMMIMessage(false));
        _menu.Close();
    }

}
