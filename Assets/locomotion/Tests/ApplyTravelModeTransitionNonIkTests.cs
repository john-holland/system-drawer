using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public class ApplyTravelModeTransitionNonIkTests
{
    [Test]
    public void PolicyContext_ActivePhrase_ResolvesNonIkFromPromptSpan()
    {
        var go = new GameObject("policy");
        var policy = go.AddComponent<AnimationPlaybackPolicyContext>();
        policy.activeScriptText = "go {P:walk|non-ik-animation=true} fast";
        policy.activePhrase = "walk";

        var segments = policy.GetSegmentsForActivePhrase();
        Assert.IsTrue(AnimationPlaybackPolicyResolver.ResolveNonIkForActivePhrase("walk", segments, null));

        policy.activePhrase = "run";
        segments = policy.GetSegmentsForActivePhrase();
        Assert.IsFalse(AnimationPlaybackPolicyResolver.ResolveNonIkForActivePhrase("run", segments, null));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void PolicyContext_LemmaProperty_ForActivePhraseBinding()
    {
        var go = new GameObject("policy");
        var policy = go.AddComponent<AnimationPlaybackPolicyContext>();
        policy.activePhrase = "walk";
        policy.SetPhraseBindings(new[]
        {
            new PlaybackPhraseBinding { eventIndex = 0, phrase = "walk", resolvedOrmKey = "urn:lemma:walk" }
        });
        policy.SetLemmaProperties(new[]
        {
            new ThesaurusEntryPropertyRecord { entryId = "urn:lemma:walk", propertyKey = "non-ik-animation", propertyValue = "true" }
        });

        Assert.IsTrue(policy.TryGetLemmaBoolForActivePhrase("non-ik-animation", out bool v) && v);
        Object.DestroyImmediate(go);
    }
}
