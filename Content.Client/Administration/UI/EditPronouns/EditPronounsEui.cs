using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.EditPronouns;

/// <summary>
///     Admin Eui to view or edit an entity's pronouns.
/// </summary>
[UsedImplicitly]
public sealed class EditPronounsEui : BaseEui
{
    private readonly EditPronounsWindow _window;

    public EditPronounsEui()
    {
        _window = new EditPronounsWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not EditPronounsEuiState s)
            return;

        _window.SetTargetEntity(s.Target);
        _window.SetPronouns(s.Pronouns);
    }
}
