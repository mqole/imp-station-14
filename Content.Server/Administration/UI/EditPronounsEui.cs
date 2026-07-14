using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.UI;

/// <summary>
///     Admin Eui to view or edit an entity's pronouns.
/// </summary>
[UsedImplicitly]
public sealed partial class EditPronounsEui : BaseEui
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;

    private readonly GrammarSystem _grammar = default!;

    private readonly EntityUid _target;

    public EditPronounsEui(EntityUid ent)
    {
        IoCManager.InjectDependencies(this);
        _grammar = _entMan.System<GrammarSystem>();
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

        return new EditPronounsEuiState(_entMan.GetNetEntity(_target), pronouns);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not EditPronounsSaveMessage saveMessage ||
            !_entMan.TryGetEntity(saveMessage.Target, out var uid) ||
            !_entMan.TryGetComponent<GrammarComponent>(uid, out var grammar))
        {
            return;
        }

        foreach (var pronoun in saveMessage.Pronouns)
        {
            if (!_protoMan.TryIndex(pronoun.Key, out var proto))
                return;
            var pair = new KeyValuePair<PronounGrammarPrototype, string>(proto, pronoun.Value);
            _grammar.SetPronoun((uid.Value, grammar), pair);
        }
        StateDirty();
    }

}
