using Content.Shared.Administration;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed partial class SetPronounCommand : ToolshedCommand
{
    private GrammarSystem? _grammar;
    [Dependency] private IPrototypeManager _protoMan = default!;

    [CommandImplementation]
    public IEnumerable<EntityUid> SetPronoun([PipedArgument] IEnumerable<EntityUid> targets, ProtoId<PronounGrammarPrototype> inflection, string pronoun)
    {
        _grammar ??= GetSys<GrammarSystem>();

        var inflectionProto = _protoMan.Index(inflection);
        var pair = new KeyValuePair<PronounGrammarPrototype, string>(inflectionProto, pronoun);

        foreach (var t in targets)
        {
            if (!TryComp<GrammarComponent>(t, out var grammar))
                continue;

            _grammar.SetPronoun((t, grammar), pair);
            yield return t;
        }
    }

    public void SetPronoun(IInvocationContext ctx, ProtoId<PronounGrammarPrototype> inflection, string pronoun)
    {
        _grammar ??= GetSys<GrammarSystem>();

        if (ExecutingEntity(ctx) is not { } ent)
        {
            if (ctx.Session is { } session)
                ctx.ReportError(new SessionHasNoEntityError(session));
            else
                ctx.ReportError(new NotForServerConsoleError());
            return;
        }

        var inflectionProto = _protoMan.Index(inflection);
        var pair = new KeyValuePair<PronounGrammarPrototype, string>(inflectionProto, pronoun);
        if (TryComp<GrammarComponent>(ent, out var grammar))
            _grammar.SetPronoun((ent, grammar), pair);
    }
}
