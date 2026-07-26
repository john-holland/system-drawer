#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;

public sealed class NsmPrimeLemmaResolverTests
{
    [SetUp]
    public void SetUp()
    {
        NsmFuzzyVariableCache.Clear();
    }

    [Test]
    public void AllSixtyFivePrimes_HaveRegisteredHandlers()
    {
        Assert.AreEqual(65, NsmPrimeLemmaResolver.AllPrimeTerms.Count());
        foreach (var term in NsmPrimeLemmaResolver.AllPrimeTerms)
        {
            Assert.IsTrue(NsmPrimeLemmaResolver.IsKnownPrime(term), term);
            var props = NsmPrimeLemmaProperties.Defaults;
            props.term = term;
            props.op = NsmPrimeLemmaOp.Evaluate;
            props.grade = 0.6f;
            props.sessionId = "test";
            string status = NsmPrimeLemmaResolver.Execute(props);
            Assert.IsFalse(string.IsNullOrEmpty(status), term);
            Assert.IsFalse(status.Contains("unknown"), term + " -> " + status);
            Assert.IsFalse(status.Contains("unhandled"), term + " -> " + status);
        }
    }

    [Test]
    public void LessSkittish_LowersCachedGrade()
    {
        NsmFuzzyVariableCache.Set("cat", "pred:skittish", "predicate", 0.8f);
        var less = NsmFuzzyVariableCache.Adjust("cat", "pred:skittish", hedgeId: "less");
        Assert.Less(less.grade.Value, 0.8f);
    }

    [Test]
    public void LikeBefore_ResolvesPriorEvent()
    {
        NsmFuzzyVariableCache.RememberEvent("btn", "press_button", 0.9f);
        NsmFuzzyVariableCache.RememberEvent("btn", "press_button", 1f);
        var prior = NsmFuzzyVariableCache.FindPriorSimilar("btn", "press_button");
        Assert.IsNotNull(prior);
        Assert.AreEqual(0.9f, prior.grade.Value, 0.001f);

        var props = NsmPrimeLemmaProperties.Defaults;
        props.term = "like";
        props.eventKey = "press_button";
        props.hedgeId = "just-like";
        props.grade = 0.8f;
        props.sessionId = "btn";
        string status = NsmPrimeLemmaResolver.Execute(props);
        Assert.IsTrue(status.Contains("similarity"), status);
    }

    [Test]
    public void Fuzzy_SomewhatLessThanMostly()
    {
        float a = NsmPrimeLemmaResolver.EvaluateFuzzy("somewhat", 0.6f);
        float b = NsmPrimeLemmaResolver.EvaluateFuzzy("mostly", 0.6f);
        Assert.Less(a, b);
    }

    [Test]
    public void TimeBefore_UsesPriorEvent()
    {
        NsmFuzzyVariableCache.RememberEvent("t", "brush_nose", 0.7f);
        NsmFuzzyVariableCache.RememberEvent("t", "brush_nose", 1f);
        var props = NsmPrimeLemmaProperties.Defaults;
        props.term = "before";
        props.eventKey = "brush_nose";
        props.sessionId = "t";
        string status = NsmPrimeLemmaResolver.Execute(props);
        Assert.IsTrue(status.Contains("prior"), status);
    }
}
#endif
