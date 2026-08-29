using UnityEngine;

/// <summary>
/// Mix of government flavors with coefficients. Example: parliamentary senate with enabled theocracy.
/// </summary>
[System.Serializable]
public sealed class GovernmentFlavorMix
{
    [Range(0f, 1f)] public float republic01 = 0.7f;
    [Range(0f, 1f)] public float parliamentary01;
    [Range(0f, 1f)] public float theocracy01;
    [Range(0f, 1f)] public float monarchyCeremonial01;
    [Range(0f, 1f)] public float monarchyReal01;
    [Range(0f, 1f)] public float junta01;
    public bool parliamentarySenateEnablesTheocracy;

    public float ThroughLine01()
    {
        float sum = republic01 + parliamentary01 + theocracy01 + monarchyCeremonial01 + monarchyReal01 + junta01;
        if (sum <= 1e-5f) return 0.5f;
        float civic = (republic01 + parliamentary01 + monarchyCeremonial01) / sum;
        float extra = parliamentarySenateEnablesTheocracy ? theocracy01 / sum : 0f;
        return Mathf.Clamp01(civic * 0.7f + extra * 0.3f + (1f - junta01 / sum) * 0.3f);
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Government Model Bio Rhythm")]
public sealed class GovernmentModelBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    public GovernmentFlavorMix mix = new GovernmentFlavorMix();

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>()
                       ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
    }

    public void Tick()
    {
        if (venueBio == null) return;
        venueBio.activity01 = mix != null ? mix.ThroughLine01() : 0.5f;
        venueBio.stress01 = mix != null ? mix.junta01 : 0.2f;
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Government Model Ragdoll")]
public sealed class GovernmentModelRagdoll : MonoBehaviour
{
    public GovernmentFlavorMix mix = new GovernmentFlavorMix();
    public GovernmentModelBioRhythm bioRhythm;
    public ListOfKingdoms kingdoms = new ListOfKingdoms();

    void Awake()
    {
        if (bioRhythm == null)
            bioRhythm = GetComponent<GovernmentModelBioRhythm>();
        if (bioRhythm != null && mix != null)
            bioRhythm.mix = mix;
    }

    public float ThroughLine01() => mix != null ? mix.ThroughLine01() : 0.5f;
}

[System.Serializable]
public sealed class ListOfKingdoms
{
    public ListOfKingdomEntry[] houses = System.Array.Empty<ListOfKingdomEntry>();
}

[System.Serializable]
public sealed class ListOfKingdomEntry
{
    public string houseId = "house";
    public MonarchPaperDoll monarch;
    public KingCard king;
    public QueenCard queen;
    [Range(0f, 1f)] public float realMonarchy01;
}
