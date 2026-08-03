using UnityEngine;

/// <summary>Lemma / bespoke scale for municipal water (turn pressure down or up).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Plumbing/Municipal Water Lemma Bias")]
public sealed class MunicipalWaterLemmaBias : MonoBehaviour
{
    [Tooltip("Multiplier on supply pressure (0 = off, 1 = nominal, >1 = boost).")]
    [Range(0f, 3f)] public float pressureScale = 1f;
    [Range(0f, 2f)] public float hotScale = 1f;
    [Range(0f, 2f)] public float coldScale = 1f;
    [Range(0f, 2f)] public float sewerScale = 1f;
    public string lemmaToken;

    public float PressureScale => Mathf.Max(0f, pressureScale);
    public float HotScale => Mathf.Max(0f, hotScale);
    public float ColdScale => Mathf.Max(0f, coldScale);
    public float SewerScale => Mathf.Max(0f, sewerScale);

    public void ApplyLemmaToken(string token)
    {
        lemmaToken = token ?? "";
        var t = lemmaToken.ToLowerInvariant();
        if (t.Contains("drought") || t.Contains("low"))
            pressureScale = 0.35f;
        else if (t.Contains("boost") || t.Contains("high"))
            pressureScale = 1.6f;
        else if (t.Contains("off") || t.Contains("shut"))
            pressureScale = 0f;
        else
            pressureScale = 1f;
    }
}
