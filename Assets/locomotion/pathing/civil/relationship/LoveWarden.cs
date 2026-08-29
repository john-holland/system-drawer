using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Affection/physicality toward a stage from <see cref="LoveCard"/> and proximity.
/// Does not replace lovemaking IK.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Love Warden")]
public sealed class LoveWarden : MonoBehaviour
{
    public LoveCard loveCard;
    public Transform stageAnchor;
    public ThreatWarden threatWarden;
    public string threatAgencyId = "relationship";
    [Range(0f, 1f)] public float lastGrade01 = 0.5f;
    [Range(0f, 1f)] public float lastScore01 = 0.5f;
    [Range(0f, 1f)] public float maxPhysicality01 = 1f;
    public float[] lastAxes01 = { 0.5f, 0.5f, 0.5f, 0.5f };
    public List<RetinuePeckingEntry> staff = new List<RetinuePeckingEntry>();

    void Awake()
    {
        BindStaffPecking();
    }

    public float Allow01() => lastScore01;

    public void BindStaffPecking()
    {
        var threat = threatWarden != null ? threatWarden : GetComponent<ThreatWarden>();
        threat?.SetRetinuePeckingOrder(staff);
    }

    public float[] Evaluate(IList<GameObject> subjects)
    {
        float prox = 0.5f;
        if (stageAnchor != null && subjects != null)
        {
            float d = 0f;
            int n = 0;
            for (int i = 0; i < subjects.Count; i++)
            {
                var s = subjects[i];
                if (s == null) continue;
                d += Vector3.Distance(s.transform.position, stageAnchor.position);
                n++;
            }
            if (n > 0)
                prox = Mathf.Clamp01(1f - d / n / 8f);
        }
        float phys = loveCard != null ? loveCard.physicality01 : 0.35f;
        float desire = loveCard != null ? loveCard.desireIntensity01 : 0.5f;
        lastGrade01 = Mathf.Clamp01(prox * 0.5f + desire * 0.5f);
        lastScore01 = lastGrade01;
        lastAxes01 = new[] { lastGrade01, Mathf.Min(phys, maxPhysicality01), desire, prox };
        return lastAxes01;
    }

    public void ApplyEffect(RomanceSeverity stage, float physicality01)
    {
        lastGrade01 = RomanceWarden.StageTo01(stage);
        lastScore01 = lastGrade01;
        if (loveCard != null)
            loveCard.physicality01 = Mathf.Min(Mathf.Clamp01(physicality01), maxPhysicality01);
    }

    public void CapPhysicality(float cap01)
    {
        maxPhysicality01 = Mathf.Clamp01(cap01);
        if (loveCard != null && loveCard.physicality01 > maxPhysicality01)
            loveCard.physicality01 = maxPhysicality01;
    }
}
