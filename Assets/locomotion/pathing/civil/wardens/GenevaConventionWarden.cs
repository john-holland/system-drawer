using UnityEngine;

/// <summary>
/// Geneva compliance scorer. Uses <see cref="ThreatWarden.IsTorture"/> plus Consent / Rights / Justice / Romance.
/// When junta or prison <c>respectsGenevaConventions</c> is false, allow is 0.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Geneva Convention Warden")]
public sealed class GenevaConventionWarden : MonoBehaviour
{
    public bool respectsGenevaConventions = true;
    public ThreatWarden threatWarden;
    public ConsentWarden consentWarden;
    public RightsWarden rightsWarden;
    public JusticeWarden justiceWarden;
    public RomanceWarden romanceWarden;
    public JuntaRuntime junta;
    public PrisonWarden prisonWarden;
    public bool lastIsTorture;
    [Range(0f, 1f)] public float lastScore01 = 1f;

    public bool Respects()
    {
        var j = junta != null ? junta : GetComponent<JuntaRuntime>();
        if (j != null) return j.respectsGenevaConventions;
        var p = prisonWarden != null ? prisonWarden : GetComponent<PrisonWarden>();
        if (p != null) return p.respectsGenevaConventions;
        return respectsGenevaConventions;
    }

    public float Allow01()
    {
        var threat = threatWarden != null ? threatWarden : GetComponent<ThreatWarden>();
        if (threat != null)
        {
            if (threat.consentWarden == null) threat.consentWarden = consentWarden != null ? consentWarden : GetComponent<ConsentWarden>();
            if (threat.rightsWarden == null) threat.rightsWarden = rightsWarden != null ? rightsWarden : GetComponent<RightsWarden>();
            if (threat.justiceWarden == null) threat.justiceWarden = justiceWarden != null ? justiceWarden : GetComponent<JusticeWarden>();
            if (threat.romanceWarden == null) threat.romanceWarden = romanceWarden != null ? romanceWarden : GetComponent<RomanceWarden>();
            lastIsTorture = threat.IsTorture();
        }
        else
            lastIsTorture = false;

        if (!Respects())
        {
            lastScore01 = 0f;
            return 0f;
        }
        lastScore01 = lastIsTorture ? 0f : 1f;
        return lastScore01;
    }
}
