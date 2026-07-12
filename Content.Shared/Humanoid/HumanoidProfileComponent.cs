using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Speech.Components;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Dictates what species and age this character "looks like"
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(HumanoidProfileSystem))]
public sealed partial class HumanoidProfileComponent : Component
{
    [DataField, AutoNetworkedField]
    public Gender Gender;

    /// <summary>
    ///     Optional list of custom pronouns for an entity, as well as the pronounGrammar inflection they belong to.
    ///     If this list does not contain a pronoun for a desired inflection,
    ///     the gender's pronoun will be used as a fallback.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<PronounGrammarPrototype>, string> Pronouns = [];

    /// <summary>
    /// Holds the EmoteSoundsPrototype that the humanoid will use to speak with
    /// To change in-game, you still have to use the <see cref="VoiceChangedEvent"/>
    /// or edit the <see cref="VocalComponent"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<EmoteSoundsPrototype> Voice = HumanoidCharacterProfile.DefaultVoice;

    [DataField, AutoNetworkedField]
    public Sex Sex;

    [DataField, AutoNetworkedField]
    public int Age = 18;

    [DataField, AutoNetworkedField]
    public ProtoId<SpeciesPrototype> Species = HumanoidCharacterProfile.DefaultSpecies;
}
