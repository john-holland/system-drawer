using System.Collections.Generic;
using UnityEngine;

public enum CareerWardenAction
{
    Retain = 0,
    Fire = 1
}

/// <summary>Hiring, firing, unemployment-office requests, and combined warden employee grade.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Career Warden")]
public sealed class CareerWarden : MonoBehaviour
{
    public CivilianDemographics demographics = new CivilianDemographics();
    public List<CivilianPaperDoll> unemployedPool = new List<CivilianPaperDoll>();
    public CareerAdvancementTree tree;
    public CompanyRegistration company;
    public ThreatWarden threatWarden;
    public PrisonWarden prisonWarden;
    public TrafficWarden trafficWarden;
    public SafetyWardenPlannerService safetyWarden;
    public AuthWarden authWarden;
    public string threatAgencyId = "career";
    public CareerWardenAction lastRecommendation = CareerWardenAction.Retain;
    [Range(0f, 1f)] public float lastGrade01;

    void Awake()
    {
        if (company == null)
            company = GetComponent<CompanyRegistration>();
        if (threatWarden == null)
            threatWarden = GetComponent<ThreatWarden>();
        if (prisonWarden == null)
            prisonWarden = GetComponent<PrisonWarden>();
        if (authWarden == null)
            authWarden = GetComponent<AuthWarden>();
    }

    public void ApplySocietyFeatures(IReadOnlyDictionary<string, float> societyFeatures, int population = 100)
    {
        demographics = CivilianDemographics.FromSocietyFeatures(societyFeatures, population);
    }

    public CivilianPaperDoll RequestCivilianPaperDoll(string personaKey = "civilian", int seed = 0)
    {
        var doll = demographics.SampleUnemployed(personaKey, unemployedPool, seed);
        if (doll == null)
            return null;
        unemployedPool.Add(doll);
        return doll;
    }

    public bool AssignJob(CivilianPaperDoll doll, CareerRoleSpec role, CompanyRegistration employer, EducationalTravelAgent agent = null)
    {
        if (doll == null || role == null) return false;
        if (role.requireNoPretraining)
            return Hire(doll, role, employer);
        if (agent == null)
            agent = GetComponent<EducationalTravelAgent>() ?? gameObject.AddComponent<EducationalTravelAgent>();
        agent.warden = this;
        agent.ResolvePath(doll, role);
        doll.educationalPlan = agent;
        doll.employment = CivilianEmploymentStatus.Training;
        doll.CopyLimitsFrom(role);
        if (employer != null)
            doll.employerCompanyId = employer.companyId;
        return true;
    }

    public bool Hire(CivilianPaperDoll doll, CareerRoleSpec role, CompanyRegistration employer)
    {
        if (doll == null || role == null) return false;
        employer = employer != null ? employer : company;
        if (employer == null) return false;
        unemployedPool.Remove(doll);
        doll.CopyLimitsFrom(role);
        doll.employment = CivilianEmploymentStatus.Employed;
        doll.employerCompanyId = employer.companyId;
        doll.isGovernmentJob = role.isGovernment;
        if (role.isGovernment && string.IsNullOrEmpty(employer.parentCompanyId))
            employer.parentCompanyId = "government";
        employer.TryHire(doll.personaKey, role.roleId, role.peckingOrder);
        BindWorkBio(doll, employer);
        lastRecommendation = CareerWardenAction.Retain;
        return true;
    }

    public bool Fire(CivilianPaperDoll doll, CompanyRegistration employer = null)
    {
        if (doll == null) return false;
        employer = employer != null ? employer : company;
        employer?.TryFire(doll.personaKey);
        doll.employment = CivilianEmploymentStatus.Unemployed;
        doll.employerCompanyId = "";
        doll.currentRoleId = "";
        if (!unemployedPool.Contains(doll) && demographics.TryAcceptUnemployed(doll, unemployedPool))
            unemployedPool.Add(doll);
        lastRecommendation = CareerWardenAction.Fire;
        return true;
    }

    public bool Promote(CivilianPaperDoll doll, CompanyRegistration employer = null)
    {
        if (doll == null || tree == null) return false;
        var next = !string.IsNullOrEmpty(doll.currentRoleId)
            ? tree.NextPromotion(doll.currentRoleId)
            : null;
        if (next == null) return false;
        return Hire(doll, next, employer);
    }

    public bool Demote(CivilianPaperDoll doll, CompanyRegistration employer = null)
    {
        if (doll == null || tree == null) return false;
        var prev = tree.PreviousDemotion(doll.currentRoleId);
        if (prev == null)
            return Fire(doll, employer);
        return Hire(doll, prev, employer);
    }

    public void ApplyPlanEffect(CivilianPaperDoll doll, CareerPlanEffect effect, string targetRoleId)
    {
        if (doll == null || effect == CareerPlanEffect.None) return;
        var role = tree != null ? tree.FindRole(targetRoleId) : null;
        switch (effect)
        {
            case CareerPlanEffect.Hire:
                if (role != null) Hire(doll, role, company);
                break;
            case CareerPlanEffect.Promote:
                Promote(doll, company);
                break;
            case CareerPlanEffect.Demote:
                Demote(doll, company);
                break;
            case CareerPlanEffect.Fire:
                Fire(doll, company);
                break;
        }
    }

    public float[] GradeEmployee(CivilianPaperDoll doll)
    {
        float skill = Skill01(doll);
        float conduct = Conduct01();
        float reliability = Reliability01(doll);
        float authority = Authority01(doll);
        lastGrade01 = (skill + conduct + reliability + authority) * 0.25f;
        var grade = new[] { skill, conduct, reliability, authority };
        lastRecommendation = OverFireLimit(doll, grade) ? CareerWardenAction.Fire : CareerWardenAction.Retain;
        return grade;
    }

    public bool OverFireLimit(CivilianPaperDoll doll, float[] grade = null)
    {
        if (doll == null) return false;
        grade = grade ?? GradeEmployee(doll);
        var red = doll.FireLimit01();
        for (int i = 0; i < 4; i++)
            if (grade[i] > red[i] + 1e-4f)
                return true;
        return false;
    }

    float Skill01(CivilianPaperDoll doll)
    {
        if (doll == null) return 0.4f;
        float s = 0.35f;
        if (doll.education == CivilianEducationAttainment.Certification) s += 0.2f;
        if (doll.education == CivilianEducationAttainment.Degree) s += 0.35f;
        if (doll.certificationIds != null) s += 0.05f * doll.certificationIds.Length;
        if (doll.degreeIds != null) s += 0.08f * doll.degreeIds.Length;
        if (doll.employment == CivilianEmploymentStatus.Training) s += 0.1f;
        if (doll.educationalPlan != null && doll.educationalPlan.steps != null && doll.educationalPlan.steps.Count > 0)
            s += 0.15f * ((doll.selectedStepIndex + 1f) / doll.educationalPlan.steps.Count);
        return Mathf.Clamp01(s);
    }

    float Conduct01()
    {
        float threat = 0f;
        if (threatWarden != null && !string.IsNullOrEmpty(threatAgencyId))
        {
            var agency = threatWarden.GetAgency(threatAgencyId);
            threat = agency.threatScore01;
        }
        float prison = prisonWarden != null ? prisonWarden.lastScore01 : 0f;
        float safety = 0f;
        if (safetyWarden != null)
            safety = 0.2f;
        return Mathf.Clamp01(1f - (threat * 0.5f + prison * 0.3f + safety * 0.2f));
    }

    float Reliability01(CivilianPaperDoll doll)
    {
        float attend = 0.7f;
        if (doll != null && doll.employment == CivilianEmploymentStatus.Employed)
            attend = 0.8f;
        if (doll != null && doll.employment == CivilianEmploymentStatus.Unemployed)
            attend = 0.4f;
        float traffic = 0f;
        if (trafficWarden != null)
            traffic = Mathf.Clamp01(trafficWarden.MaxEdgeDemand / 16f);
        return Mathf.Clamp01(attend * (1f - traffic * 0.25f));
    }

    float Authority01(CivilianPaperDoll doll)
    {
        float peck = 0.4f;
        if (company != null && doll != null)
        {
            var entry = company.FindStaff(doll.personaKey);
            if (entry != null)
                peck = Mathf.Clamp01(1f - entry.peckingOrder / 40f);
        }
        float auth = authWarden != null && doll != null && authWarden.HasGrant(gameObject.name, doll.personaKey)
            ? 0.2f
            : 0f;
        var role = tree != null && doll != null ? tree.FindRole(doll.currentRoleId) : null;
        float mgmt = 0f;
        if (role != null)
        {
            if (role.requiresManagement) mgmt += 0.15f;
            if (role.requiresHiringManager) mgmt += 0.15f;
        }
        return Mathf.Clamp01(peck + auth + mgmt);
    }

    static void BindWorkBio(CivilianPaperDoll doll, CompanyRegistration employer)
    {
        if (doll == null || employer == null) return;
        var runtime = Object.FindObjectsByType<CivilianPaperDollRuntime>(FindObjectsSortMode.None);
        for (int i = 0; i < runtime.Length; i++)
        {
            if (runtime[i] == null || runtime[i].doll != doll || runtime[i].schedule == null)
                continue;
            runtime[i].schedule.workBio = employer.GetComponent<BuildingBioRhythmService>();
        }
    }
}
