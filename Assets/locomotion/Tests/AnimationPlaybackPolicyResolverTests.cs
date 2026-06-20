using Locomotion.Narrative;
using NUnit.Framework;

public class AnimationPlaybackPolicyResolverTests
{
    [Test]
    public void Resolve_DefaultFalse()
    {
        Assert.IsFalse(AnimationPlaybackPolicyResolver.ResolveNonIkAnimation(null));
    }

    [Test]
    public void Resolve_TrueFromPromptParam()
    {
        var segments = PromptSpanParser.Parse("{P:walk|non-ik-animation=true}");
        Assert.IsTrue(AnimationPlaybackPolicyResolver.ResolveNonIkAnimation(segments));
    }

    [Test]
    public void Resolve_TrueFromClauseBinding()
    {
        var bindings = new[]
        {
            new LocalizationClauseBindingRecord
            {
                propertyKey = "non-ik-animation",
                propertyValue = "true"
            }
        };
        Assert.IsTrue(AnimationPlaybackPolicyResolver.ResolveNonIkAnimation(null, bindings));
    }

    [Test]
    public void Resolve_TrueFromClipConfig()
    {
        var cfg = new ABTClipConfig { nonIkAnimation = true };
        Assert.IsTrue(AnimationPlaybackPolicyResolver.ResolveNonIkAnimation(null, null, cfg));
    }

    [Test]
    public void ResolveEffectiveBool_PromptBeforeLemma()
    {
        var segments = PromptSpanParser.Parse("{P:walk|non-ik-animation=false}");
        var lemmas = new[]
        {
            new ThesaurusEntryPropertyRecord { propertyKey = "non-ik-animation", propertyValue = "true" }
        };
        Assert.IsFalse(AnimationPlaybackPolicyResolver.ResolveEffectiveBool(
            "non-ik-animation", segments, null, lemmas, "true"));
    }

    [Test]
    public void ResolveEffectiveBool_ClauseBeforeLemma()
    {
        var bindings = new[]
        {
            new LocalizationClauseBindingRecord
            {
                propertyKey = "non-ik-animation",
                propertyValue = "true",
                bindingKind = LocalizationBindingKinds.Property
            }
        };
        var lemmas = new[]
        {
            new ThesaurusEntryPropertyRecord { propertyKey = "non-ik-animation", propertyValue = "false" }
        };
        Assert.IsTrue(AnimationPlaybackPolicyResolver.ResolveEffectiveBool(
            "non-ik-animation", null, bindings, lemmas, "false"));
    }
}
