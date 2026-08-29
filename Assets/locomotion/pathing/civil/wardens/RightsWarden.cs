using System.Collections.Generic;
using UnityEngine;

/// <summary>Reads <see cref="ConstitutionWarden"/>. Reports 0 when junta suspended or kangaroo court.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rights Warden")]
public sealed class RightsWarden : MonoBehaviour
{
    [Range(0f, 1f)] public float lastScore01 = 1f;
    public ConstitutionWarden constitutionWarden;
    public JuntaRuntime junta;
    public CourtKind courtKind = CourtKind.American;
    public List<WardenLimitKv> limits = new List<WardenLimitKv>();

    public float Allow01()
    {
        if (Suspended())
        {
            lastScore01 = 0f;
            return 0f;
        }
        var constitution = constitutionWarden != null ? constitutionWarden : GetComponent<ConstitutionWarden>();
        if (constitution != null)
        {
            lastScore01 = constitution.Allow01();
            return lastScore01;
        }
        return lastScore01;
    }

    public bool Suspended()
    {
        if (courtKind == CourtKind.Kangaroo) return true;
        var j = junta != null ? junta : GetComponent<JuntaRuntime>();
        if (j != null && j.canSuspendConstitution) return true;
        var constitution = constitutionWarden != null ? constitutionWarden : GetComponent<ConstitutionWarden>();
        return constitution != null && constitution.Suspended();
    }
}
