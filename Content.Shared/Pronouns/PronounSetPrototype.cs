using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Pronouns;

/// <summary>
///     Defines a preset group of pronouns to use for an entity's
///     <see cref="GrammarComponent"/>.
/// </summary>
[Prototype]
public sealed partial class PronounSetPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Localized name of this prototype.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    ///     The pronoun to use for each inflection when this set is selected.
    ///     Use <see cref="GetPronouns" as accessor.
    /// </summary>
    /// <remarks>
    ///     TODO: Ideally this should be localized, but a better way to do it would probably be to sync pronouns to a culture and that sounds like an ordeal
    /// </remarks>
    [DataField]
    public Dictionary<ProtoId<PronounGrammarPrototype>, string> Pronouns = [];

    /// <summary>
    ///     Default form of conjugation to use for verbs (singular IS vs plural ARE, etc.)
    /// </summary>
    [DataField]
    public bool IsPlural;

    /// <summary>
    ///     List of pronouns that use generalized plural conjugation.
    ///     This is done so we can have a single checkbox 'plural' instead of a big list of verb conjugations.
    /// </summary>
    private readonly List<ProtoId<PronounGrammarPrototype>> _pluralPronouns = new() { "ConjugateBe", "ConjugateHave", "ConjugateBasic" };

    /// <summary>
    ///     Gets a dictionary of pronouns to use for this PronounSet, including inflections reliant on plurality.
    /// </summary>
    public Dictionary<ProtoId<PronounGrammarPrototype>, string> GetPronouns()
    {
        if (!IsPlural)
            return Pronouns;

        var newPronouns = Pronouns;
        foreach (var pronoun in _pluralPronouns)
            newPronouns[pronoun] = "epicene";

        return newPronouns;
    }
}
