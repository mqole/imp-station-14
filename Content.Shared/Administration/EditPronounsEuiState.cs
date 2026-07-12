using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class EditPronounsEuiState(NetEntity target, Dictionary<ProtoId<PronounGrammarPrototype>, string> pronouns) : EuiStateBase
{
    /// <summary>
    ///     Entity to open the pronouns of.
    /// </summary>
    public readonly NetEntity Target = target;

    /// <summary>
    ///     Pronouns belonging to the target entity.
    /// </summary>
    public readonly Dictionary<ProtoId<PronounGrammarPrototype>, string> Pronouns = pronouns;
}
