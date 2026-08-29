using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CountrySpec", menuName = "Locomotion/Civil/Country Spec")]
public sealed class CountrySpec : ScriptableObject
{
    public string countryId = "country";
    public string displayName = "Country";
    public ConstitutionAsset constitution;
    public List<StateSpec> states = new List<StateSpec>();
}

[CreateAssetMenu(fileName = "StateSpec", menuName = "Locomotion/Civil/State Spec")]
public sealed class StateSpec : ScriptableObject
{
    public string stateId = "state";
    public string displayName = "State";
    public CountrySpec country;
}

public enum LawChamberKind
{
    House = 0,
    Senate = 1,
    Parliament = 2
}

public enum LawStageKind
{
    Draft = 0,
    Committee = 1,
    House = 2,
    Senate = 3,
    Filibuster = 4,
    Amendment = 5,
    Veto = 6,
    Enact = 7,
    JudicialReview = 8
}

[System.Serializable]
public sealed class Congress
{
    public string congressId = "congress";
    public bool bicameral = true;
    public List<CongressPersonPaperDoll> house = new List<CongressPersonPaperDoll>();
    public List<SenatePersonPaperDoll> senate = new List<SenatePersonPaperDoll>();
    public List<ParliamentPersonPaperDoll> parliament = new List<ParliamentPersonPaperDoll>();
}

[System.Serializable]
public sealed class BillOfRightsArticle
{
    public string articleId = "amendment-1";
    public string displayName = "Speech";
    [TextArea] public string text;
    public bool enabled = true;
}

[CreateAssetMenu(fileName = "ConstitutionAsset", menuName = "Locomotion/Civil/Constitution")]
public sealed class ConstitutionAsset : ScriptableObject
{
    public string constitutionId = "constitution";
    public CountrySpec country;
    public List<BillOfRightsArticle> articles = new List<BillOfRightsArticle>();

    public BillOfRightsArticle FindArticle(string articleId)
    {
        if (articles == null || string.IsNullOrEmpty(articleId)) return null;
        for (int i = 0; i < articles.Count; i++)
            if (articles[i] != null && articles[i].articleId == articleId)
                return articles[i];
        return null;
    }

    public bool ArticleEnabled(string articleId)
    {
        var a = FindArticle(articleId);
        return a != null && a.enabled;
    }
}
