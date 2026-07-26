using System;

/// <summary>Property keys and DTOs for {P:nsm|...} semantic-prime lemma painting.</summary>
public static class NsmLemmaPropertyKeys
{
    public const string PlaceholderName = "nsm";

    public const string Term = "term";
    public const string Op = "op";
    public const string Session = "session";
    public const string VarKey = "var";
    public const string Grade = "x";
    public const string Hedge = "hedge";
    public const string EventKey = "event";
    public const string Delta = "delta";

    public const string SpecPrime = "nsm-prime";
    public const string SpecGroup = "nsm-group";
    public const string SpecDefinition = "nsm-definition";
    public const string SpecLogicalForm = "nsm-logical-form";
    public const string SpecCausalityRole = "nsm-causality-role";
    public const string SpecTemporalRole = "nsm-temporal-role";
    public const string SpecFuzzyHedge = "nsm-fuzzy-hedge";
    public const string SpecFuzzyCurve = "nsm-fuzzy-curve";
    public const string SpecCausalityTree = "causality-tree";

    public static readonly string[] AllKeys =
    {
        Term, Op, Session, VarKey, Grade, Hedge, EventKey, Delta
    };
}

public enum NsmPrimeLemmaOp
{
    None,
    Evaluate,
    Bind,
    Adjust,
    RememberEvent,
    QueryPrior,
    Discourse
}

[Serializable]
public struct NsmPrimeLemmaProperties
{
    public NsmPrimeLemmaOp op;
    public string term;
    public string group;
    public string sessionId;
    public string varKey;
    public string hedgeId;
    public string eventKey;
    public float grade;
    public float delta;

    public static NsmPrimeLemmaProperties Defaults => new NsmPrimeLemmaProperties
    {
        op = NsmPrimeLemmaOp.None,
        grade = 0.5f,
        delta = 0f
    };
}
