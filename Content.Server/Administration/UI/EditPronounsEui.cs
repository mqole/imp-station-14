using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Server.Administration.UI;

/// <summary>
///     Admin Eui to view or edit an entity's pronouns.
/// </summary>
[UsedImplicitly]
public sealed partial class EditPronounsEui : BaseEui
{
    [Dependency] private IEntityManager _entityManager = default!;

    private readonly GrammarSystem _grammar = default!;

    private readonly EntityUid _target;

    public EditPronounsEui(EntityUid ent)
    {
        IoCManager.InjectDependencies(this);
        _grammar = _entityManager.System<GrammarSystem>();
        _target = ent;
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        if (!_grammar.TryGetPronouns(_target, out var pronouns))
            pronouns = [];

        return new EditPronounsEuiState(_entityManager.GetNetEntity(_target), pronouns);
    }
}
