using UnityEngine;

/// <summary>Session hours, docket stress, and audience fill for a courthouse venue.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Court System Bio Rhythm")]
public sealed class CourtSystemBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    public CourtWarden courtWarden;
    public string hoursCron = "* 8-17 * * 1-5";
    [Range(0f, 1f)] public float sessionOpen01 = 0.5f;
    [Range(0f, 1f)] public float docketStress01 = 0.3f;
    [Range(0f, 1f)] public float audienceFill01 = 0.4f;

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>()
                       ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (courtWarden == null)
            courtWarden = GetComponent<CourtWarden>();
    }

    public void Tick(System.DateTime utcNow)
    {
        bool open = CronDue.IsActiveSchedule(hoursCron, utcNow);
        sessionOpen01 = open ? 0.85f : 0.15f;
        if (venueBio != null)
        {
            venueBio.activity01 = sessionOpen01;
            venueBio.stress01 = docketStress01;
            venueBio.pace01 = Mathf.Clamp01(0.4f + audienceFill01 * 0.3f);
        }
        if (courtWarden != null)
        {
            courtWarden.docketStress01 = docketStress01;
            courtWarden.audienceFill01 = audienceFill01;
        }
    }
}
