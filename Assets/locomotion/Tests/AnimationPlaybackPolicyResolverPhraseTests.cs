using Locomotion.Narrative;
using NUnit.Framework;

public class AnimationPlaybackPolicyResolverPhraseTests
{
    [Test]
    public void ResolveForActivePhrase_OnlyMatchingPlaceholder()
    {
        var all = PromptSpanParser.Parse("run {P:walk|non-ik-animation=true} then {P:sprint}");
        var scoped = new System.Collections.Generic.List<PromptSegment>();
        foreach (var seg in all)
        {
            if (seg.isPlaceholder && seg.placeholderName == "walk")
                scoped.Add(seg);
        }
        Assert.IsTrue(AnimationPlaybackPolicyResolver.ResolveNonIkForActivePhrase("walk", scoped, null));
    }

    [Test]
    public void ResolveForActivePhrase_IgnoresOtherPlaceholder()
    {
        var all = PromptSpanParser.Parse("run {P:walk|non-ik-animation=true} then {P:sprint}");
        var scoped = new System.Collections.Generic.List<PromptSegment>();
        foreach (var seg in all)
        {
            if (seg.isPlaceholder && seg.placeholderName == "sprint")
                scoped.Add(seg);
        }
        Assert.IsFalse(AnimationPlaybackPolicyResolver.ResolveNonIkForActivePhrase("sprint", scoped, null));
    }

    [Test]
    public void ResolveForActivePhrase_FallsBackToClipConfig()
    {
        var cfg = new ABTClipConfig { nonIkAnimation = true };
        Assert.IsTrue(AnimationPlaybackPolicyResolver.ResolveNonIkForActivePhrase("", null, null, cfg));
    }
}
