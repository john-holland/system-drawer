using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>Syncs narrative interpreter output into <see cref="AnimationPlaybackPolicyContext"/>.</summary>
public static class AnimationPlaybackPolicyBridge
{
    public static void ApplyFromInterpret(NarrativeLSTMPromptInterpreter interpreter, NarrativePromptAsset asset)
    {
        if (interpreter == null || asset == null)
            return;

        AnimationPlaybackPolicyContext ctx = FindContext(interpreter);
        if (ctx == null)
            return;

        ctx.ApplyFromPromptAsset(asset, ConvertBindings(interpreter.lastBindings));
        ctx.activeScriptText = asset.GetActivePromptText();
        ScriptPlaybackCursor.Update(ctx.GetActiveScriptText(), ctx.activePhrase, ctx.activeEventIndex);
    }

    public static void ApplyFromInterpret(NarrativeLSTMPromptInterpreter interpreter, string promptText)
    {
        if (interpreter == null)
            return;

        AnimationPlaybackPolicyContext ctx = FindContext(interpreter);
        if (ctx == null)
            return;

        ctx.activeScriptText = promptText ?? "";
        ScriptPlaybackCursor.Update(ctx.GetActiveScriptText(), ctx.activePhrase, ctx.activeEventIndex);
        ctx.SetPhraseBindings(ConvertBindings(interpreter.lastBindings));
    }

    public static void PushLemmaPropertiesToTravelAgent(AnimationPlaybackPolicyContext ctx, TravelAgent agent)
    {
        if (ctx == null || agent == null)
            return;

        var target = agent.GetComponent<AnimationPlaybackPolicyContext>()
                     ?? agent.GetComponentInChildren<AnimationPlaybackPolicyContext>();
        if (target == null)
            return;

        target.SetLemmaProperties(ctx.LemmaProperties);
        target.SetClauseBindings(ctx.ClauseBindings);
        target.activeScriptText = ctx.GetActiveScriptText();
        target.activePrompt = ctx.activePrompt;
    }

    static IEnumerable<PlaybackPhraseBinding> ConvertBindings(IReadOnlyList<InterpretedEventBinding> bindings)
    {
        if (bindings == null)
            yield break;
        foreach (var b in bindings)
        {
            yield return new PlaybackPhraseBinding
            {
                eventIndex = b.eventIndex,
                phrase = b.phrase,
                resolvedOrmKey = b.resolvedOrmKey,
                builtInEntryId = b.builtInEntryId
            };
        }
    }

    static AnimationPlaybackPolicyContext FindContext(Component root)
    {
        if (root == null)
            return null;
        return root.GetComponent<AnimationPlaybackPolicyContext>()
               ?? root.GetComponentInParent<AnimationPlaybackPolicyContext>()
               ?? root.GetComponentInChildren<AnimationPlaybackPolicyContext>();
    }
}
