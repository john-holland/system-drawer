using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Venue biorhythm for a relationship: composes <see cref="CivilVenueBioRhythmService"/>,
/// does not subclass <see cref="DispatchBioRhythm"/>.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Relationship Bio Rhythm")]
public sealed class RelationshipBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    public List<LifeSystemsSheet> subjectSheets = new List<LifeSystemsSheet>();
    [Range(0f, 1f)] public float affection01 = 0.4f;
    [Range(0f, 1f)] public float attention01 = 0.45f;
    public List<string> prebakeTags = new List<string> { "romance", "love", "biorhythm" };
    public float tickInterval = 0.5f;
    float _accum;

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>()
                ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
    }

    void Update()
    {
        _accum += Time.deltaTime;
        if (_accum < tickInterval) return;
        _accum = 0f;
        Tick(tickInterval);
    }

    public void Tick(float dt)
    {
        affection01 = Mathf.MoveTowards(affection01, 0.45f, dt * 0.02f);
        attention01 = Mathf.MoveTowards(attention01, 0.4f, dt * 0.015f);
        if (venueBio != null)
        {
            venueBio.activity01 = Mathf.Clamp01(0.3f + affection01 * 0.4f);
            venueBio.stress01 = Mathf.Clamp01(1f - affection01);
            venueBio.Tick(dt);
        }
        if (subjectSheets == null) return;
        for (int i = 0; i < subjectSheets.Count; i++)
        {
            var sheet = subjectSheets[i];
            if (sheet == null) continue;
            sheet.EnsureDefaults();
            sheet.Set01(LifeSystemsChannelCatalog.Morale, affection01);
            sheet.Set01(LifeSystemsChannelCatalog.Attention, attention01);
        }
    }

    public void BindSubjects(IList<GameObject> subjects)
    {
        if (subjectSheets == null) subjectSheets = new List<LifeSystemsSheet>();
        subjectSheets.Clear();
        if (subjects == null) return;
        for (int i = 0; i < subjects.Count; i++)
        {
            var go = subjects[i];
            if (go == null) continue;
            var sheet = go.GetComponent<LifeSystemsSheet>() ?? go.GetComponentInChildren<LifeSystemsSheet>();
            if (sheet != null)
                subjectSheets.Add(sheet);
        }
    }
}
