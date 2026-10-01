using System;
using UnityEngine;

public enum CivilianAgeBand
{
    Child0To17 = 0,
    Adult18To64 = 1,
    Senior65Plus = 2
}

public enum CivilianEducationAttainment
{
    None = 0,
    Certification = 1,
    Degree = 2
}

public enum CivilianEmploymentStatus
{
    Unemployed = 0,
    Employed = 1,
    Student = 2,
    Training = 3
}

/// <summary>Civilian description sheet. Prison silhouette preview stays in PrisonWardenPowerDiamondWindow.</summary>
[CreateAssetMenu(fileName = "CivilianPaperDoll", menuName = "Locomotion/Civil/Civilian Paper Doll")]
public sealed class CivilianPaperDoll : ScriptableObject
{
    public const int AxisCount = 4;
    public static readonly string[] GradeAxes = { "Skill", "Conduct", "Reliability", "Authority" };

    public string personaKey = "civilian";
    public CivilianAgeBand ageBand = CivilianAgeBand.Adult18To64;
    public CivilianEducationAttainment education = CivilianEducationAttainment.None;
    public CivilianEmploymentStatus employment = CivilianEmploymentStatus.Unemployed;
    public string currentRoleId;
    public string employerCompanyId;
    [Tooltip("Display / gov-glove only. Same hire/train/fire path as private jobs.")]
    public bool isGovernmentJob;
    [Range(0f, 1f)] public float[] expected01 = { 0.55f, 0.55f, 0.55f, 0.4f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };
    public EducationalTravelAgent educationalPlan;
    public int selectedStepIndex;
    public string[] certificationIds = Array.Empty<string>();
    public string[] degreeIds = Array.Empty<string>();

    public float[] Expected01() => Pad4(expected01, 0.55f);

    public float[] FireLimit01() => Pad4(fireLimit01, 0.9f);

    public float[] WhiteStep01()
    {
        if (educationalPlan == null) return Expected01();
        educationalPlan.selectedStepIndex = selectedStepIndex;
        var step = educationalPlan.SelectedStep;
        return step != null ? step.Expected01() : Expected01();
    }

    public void CopyLimitsFrom(CareerRoleSpec role)
    {
        if (role == null) return;
        expected01 = Pad4(role.expected01, 0.55f);
        fireLimit01 = Pad4(role.fireLimit01, 0.9f);
        isGovernmentJob = role.isGovernment;
        currentRoleId = role.roleId;
    }

    public bool HasCredential(string id)
    {
        if (string.IsNullOrEmpty(id)) return true;
        if (ContainsId(certificationIds, id)) return true;
        return ContainsId(degreeIds, id);
    }

    public static float[] Pad4(float[] src, float fallback)
    {
        var a = new float[AxisCount];
        for (int i = 0; i < AxisCount; i++)
            a[i] = src != null && i < src.Length ? Mathf.Clamp01(src[i]) : fallback;
        return a;
    }

    static bool ContainsId(string[] ids, string id)
    {
        if (ids == null) return false;
        for (int i = 0; i < ids.Length; i++)
            if (string.Equals(ids[i], id, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
