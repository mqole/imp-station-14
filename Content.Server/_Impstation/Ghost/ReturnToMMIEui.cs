using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost;
using Content.Shared.Mind;

namespace Content.Server._Impstation.Ghost;

public sealed class ReturnToMindEui : BaseEui
{
    private readonly SharedMindSystem _mindSystem;

    private readonly MindComponent _mind;

    public ReturnToMindEui(MindComponent mind, SharedMindSystem mindSystem)
    {
        _mind = mind;
        _mindSystem = mindSystem;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not ReturnToBodyMessage choice ||
            !choice.Accepted)
        {
            Close();
            // DISSOLVE IT!
            return;
        }

        _mindSystem.UnVisit(_mind.Session);
        //May need to call a 'return to mind' event here. idk if unvisit will work.

        Close();
    }
}
