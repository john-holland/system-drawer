using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Relationship stage, fidelity, and admirers. Does not replace <see cref="RomanceProfile"/> cards.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Romance Warden")]
public sealed class RomanceWarden : MonoBehaviour
{
    // consider platous of love and romance for romantic depth, "oh i could never stay angry at you, except sundays and tuesdays, leave me alone then"
    
    public RomanceSeverity stage = RomanceSeverity.Notion;
    public string customStageId;
    public RomanceFidelity fidelity = RomanceFidelity.Faithful;
    [Range(0f, 1f)] public float lastGrade01 = 0.2f;
    [Range(0f, 1f)] public float lastScore01 = 0.2f;
    public List<GameObject> subjects = new List<GameObject>();

    public float Allow01() => lastScore01;

    public float Evaluate()
    {
        lastGrade01 = StageTo01(stage);
        if (fidelity == RomanceFidelity.Cheating)
            lastGrade01 = Mathf.Clamp01(lastGrade01 * 0.85f);
        lastScore01 = lastGrade01;
        return lastGrade01;
    }

    public void SetStage(RomanceSeverity next)
    {
        stage = next;
        Evaluate();
        if (subjects == null) return;
        for (int i = 0; i < subjects.Count; i++)
        {
            var go = subjects[i];
            if (go == null) continue;
            var profile = go.GetComponent<RomanceProfile>();
            if (profile != null)
                profile.severity = next;
        }
    }

    public static float StageTo01(RomanceSeverity s)
    {
        switch (s)
        {
            case RomanceSeverity.FriendZone: return 0.08f;
            case RomanceSeverity.Notion: return 0.18f;
            case RomanceSeverity.Crush: return 0.32f;
            case RomanceSeverity.GoingOut: return 0.48f;
            case RomanceSeverity.GoingSteady: return 0.62f;
            case RomanceSeverity.HotAndHeavy: return 0.78f;
            case RomanceSeverity.OnAgainOffAgain: return 0.55f;
            case RomanceSeverity.Newlywed: return 0.9f;
            case RomanceSeverity.Married: return 1f;
            case RomanceSeverity.OnTheRocks: return 0.4f;
            case RomanceSeverity.Estranged: return 0.28f;
            case RomanceSeverity.Separated: return 0.18f;
            case RomanceSeverity.Divorced: return 0.1f;
            default: return 0.5f;
        }
    }
}
