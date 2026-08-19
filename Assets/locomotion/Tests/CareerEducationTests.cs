using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public sealed class CareerEducationTests
{
    [Test]
    public void KindFromBuildingType_UnemploymentOffice()
    {
        Assert.AreEqual(CivilSystemKind.UnemploymentOffice, CivilSystemLattice.KindFromBuildingType("job_center"));
        Assert.AreEqual(CivilSystemKind.UnemploymentOffice, CivilSystemLattice.KindFromBuildingType("unemployment_office"));
        Assert.AreEqual(CivilSystemKind.UnemploymentOffice, CivilSystemLattice.KindFromBuildingType("dol"));
    }

    [Test]
    public void DefaultSlots_UnemploymentAndSchoolLearningStations()
    {
        var office = BuildingRequirementSpec.DefaultSlotsFor("job_center");
        Assert.IsTrue(office.Exists(s => s.slotId == "intake_desk"));
        Assert.IsTrue(office.Exists(s => s.slotId == "interview_room"));
        Assert.IsTrue(office.Exists(s => s.slotId == "job_board"));
        Assert.IsTrue(office.Exists(s => s.slotId == "benefits_window"));
        var school = BuildingRequirementSpec.DefaultSlotsFor("school");
        Assert.IsTrue(school.Exists(s => s.slotId == "desk"));
        Assert.IsTrue(school.Exists(s => s.slotId == "computer"));
        Assert.IsTrue(school.Exists(s => s.slotId == "phone"));
    }

    [Test]
    public void Demographics_RejectOverQuotaUnemployed()
    {
        var demo = new CivilianDemographics { cityPopulation = 10, unemploymentRate01 = 0.1f };
        Assert.AreEqual(1, demo.UnemployedQuota);
        var existing = new List<CivilianPaperDoll>();
        var first = ScriptableObject.CreateInstance<CivilianPaperDoll>();
        first.employment = CivilianEmploymentStatus.Unemployed;
        first.ageBand = CivilianAgeBand.Adult18To64;
        first.education = CivilianEducationAttainment.None;
        Assert.IsTrue(demo.TryAcceptUnemployed(first, existing));
        existing.Add(first);
        var second = ScriptableObject.CreateInstance<CivilianPaperDoll>();
        second.employment = CivilianEmploymentStatus.Unemployed;
        second.ageBand = CivilianAgeBand.Adult18To64;
        second.education = CivilianEducationAttainment.None;
        Assert.IsFalse(demo.TryAcceptUnemployed(second, existing));
        Object.DestroyImmediate(first);
        Object.DestroyImmediate(second);
    }

    [Test]
    public void Demographics_FromWelfareFallback()
    {
        var d = CivilianDemographics.FromSocietyFeatures(
            new Dictionary<string, float> { ["welfareBenefits"] = 0.8f }, 50);
        Assert.AreEqual(0.2f, d.unemploymentRate01, 0.001f);
        var explicitRate = CivilianDemographics.FromSocietyFeatures(
            new Dictionary<string, float> { ["unemploymentRate"] = 0.12f }, 50);
        Assert.AreEqual(0.12f, explicitRate.unemploymentRate01, 0.001f);
    }

    [Test]
    public void RequireNoPretraining_SkipsEducationalTravelAgent()
    {
        var go = new GameObject("warden");
        var warden = go.AddComponent<CareerWarden>();
        var company = go.AddComponent<CompanyRegistration>();
        company.companyId = "co";
        warden.company = company;
        var doll = ScriptableObject.CreateInstance<CivilianPaperDoll>();
        doll.personaKey = "pat";
        doll.employment = CivilianEmploymentStatus.Unemployed;
        var role = ScriptableObject.CreateInstance<CareerRoleSpec>();
        role.roleId = "temp";
        role.requireNoPretraining = true;
        Assert.IsTrue(warden.AssignJob(doll, role, company));
        Assert.AreEqual(CivilianEmploymentStatus.Employed, doll.employment);
        Assert.IsNull(doll.educationalPlan);
        Assert.IsNotNull(company.FindStaff("pat"));
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(doll);
        Object.DestroyImmediate(role);
    }

    [Test]
    public void ResolvePath_EmitsStationCertDegreeAndManagement()
    {
        var go = new GameObject("eta");
        var agent = go.AddComponent<EducationalTravelAgent>();
        var doll = ScriptableObject.CreateInstance<CivilianPaperDoll>();
        var lane = EducationalLane.CreateWith(LearningStationKind.Desk, LearningStationKind.Class);
        var role = ScriptableObject.CreateInstance<CareerRoleSpec>();
        role.roleId = "manager";
        role.lane = lane;
        role.certificationIds = new[] { "osha" };
        role.degreeIds = new[] { "ba" };
        role.requiresManagement = true;
        role.requiresHiringManager = true;
        var steps = agent.ResolvePath(doll, role);
        Assert.AreEqual(6, steps.Count);
        Assert.AreEqual(LearningStationKind.Desk, steps[0].station);
        Assert.AreEqual(LearningStationKind.Class, steps[1].station);
        Assert.AreEqual(LearningStationKind.Certification, steps[2].station);
        Assert.AreEqual(LearningStationKind.UniversityCourse, steps[3].station);
        Assert.AreEqual(LearningStationKind.Conversation, steps[4].station);
        Assert.AreEqual(LearningStationKind.Phone, steps[5].station);
        Assert.AreEqual(CareerPlanEffect.Hire, steps[5].effect);
        Assert.AreEqual(CivilianEmploymentStatus.Training, doll.employment);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(doll);
        Object.DestroyImmediate(lane);
        Object.DestroyImmediate(role);
    }

    [Test]
    public void GovernmentRole_SameHireFirePath()
    {
        var go = new GameObject("gov");
        var warden = go.AddComponent<CareerWarden>();
        var company = go.AddComponent<CompanyRegistration>();
        company.companyId = "dol";
        warden.company = company;
        var doll = ScriptableObject.CreateInstance<CivilianPaperDoll>();
        doll.personaKey = "clerk";
        var role = ScriptableObject.CreateInstance<CareerRoleSpec>();
        role.roleId = "clerk";
        role.isGovernment = true;
        role.requireNoPretraining = true;
        Assert.IsTrue(warden.Hire(doll, role, company));
        Assert.IsTrue(doll.isGovernmentJob);
        Assert.AreEqual("government", company.parentCompanyId);
        Assert.AreEqual(CivilianEmploymentStatus.Employed, doll.employment);
        Assert.IsTrue(warden.Fire(doll, company));
        Assert.AreEqual(CivilianEmploymentStatus.Unemployed, doll.employment);
        Assert.IsNull(company.FindStaff("clerk"));
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(doll);
        Object.DestroyImmediate(role);
    }

    [Test]
    public void PlanEffects_HirePromoteDemoteFire()
    {
        var go = new GameObject("career");
        var warden = go.AddComponent<CareerWarden>();
        var company = go.AddComponent<CompanyRegistration>();
        company.companyId = "factory";
        warden.company = company;
        var worker = ScriptableObject.CreateInstance<CareerRoleSpec>();
        worker.roleId = "line_worker";
        worker.peckingOrder = 20;
        var manager = ScriptableObject.CreateInstance<CareerRoleSpec>();
        manager.roleId = "factory_manager";
        manager.prerequisiteRoleIds = new[] { "line_worker" };
        manager.peckingOrder = 3;
        var tree = ScriptableObject.CreateInstance<CareerAdvancementTree>();
        tree.roles = new List<CareerRoleSpec> { worker, manager };
        warden.tree = tree;
        var doll = ScriptableObject.CreateInstance<CivilianPaperDoll>();
        doll.personaKey = "lee";
        warden.ApplyPlanEffect(doll, CareerPlanEffect.Hire, "line_worker");
        Assert.AreEqual("line_worker", doll.currentRoleId);
        warden.ApplyPlanEffect(doll, CareerPlanEffect.Promote, null);
        Assert.AreEqual("factory_manager", doll.currentRoleId);
        warden.ApplyPlanEffect(doll, CareerPlanEffect.Demote, null);
        Assert.AreEqual("line_worker", doll.currentRoleId);
        warden.ApplyPlanEffect(doll, CareerPlanEffect.Fire, null);
        Assert.AreEqual(CivilianEmploymentStatus.Unemployed, doll.employment);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(worker);
        Object.DestroyImmediate(manager);
        Object.DestroyImmediate(tree);
        Object.DestroyImmediate(doll);
    }

    [Test]
    public void Grade_OverFireLimitRecommendsFire()
    {
        var go = new GameObject("grade");
        var warden = go.AddComponent<CareerWarden>();
        var threat = go.AddComponent<ThreatWarden>();
        warden.threatWarden = threat;
        warden.threatAgencyId = "career";
        threat.SetLevels("career", ThreatAlertLevel.UnderAttack, ThreatLevel.PotentialIntruders, 1f, 1f);
        var doll = ScriptableObject.CreateInstance<CivilianPaperDoll>();
        doll.expected01 = new[] { 0.2f, 0.2f, 0.2f, 0.2f };
        doll.fireLimit01 = new[] { 0.15f, 0.15f, 0.15f, 0.15f };
        var grade = warden.GradeEmployee(doll);
        Assert.AreEqual(4, grade.Length);
        Assert.IsTrue(warden.OverFireLimit(doll, grade));
        Assert.AreEqual(CareerWardenAction.Fire, warden.lastRecommendation);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(doll);
    }

    [Test]
    public void Prebake_RngSpecificAndConditional()
    {
        var go = new GameObject("prebake");
        var agent = go.AddComponent<EducationalTravelAgent>();
        var calGo = new GameObject("cal");
        var cal = calGo.AddComponent<NarrativeCalendarAsset>();
        agent.steps = new List<EducationalStep>
        {
            new EducationalStep
            {
                station = LearningStationKind.Desk,
                timing = EducationalTimingMode.RngRange,
                minSeconds = 100,
                maxSeconds = 300
            },
            new EducationalStep
            {
                station = LearningStationKind.Class,
                timing = EducationalTimingMode.Specific,
                durationSeconds = 3600
            },
            new EducationalStep
            {
                station = LearningStationKind.Library,
                timing = EducationalTimingMode.Conditional,
                effect = CareerPlanEffect.Hire
            }
        };
        int n = agent.PrebakeCalendar(cal);
        Assert.AreEqual(3, n);
        Assert.AreEqual(3, cal.events.Count);
        Assert.IsTrue(cal.events[0].tags.Contains("education"));
        Assert.IsTrue(cal.events[0].notes.Contains("rng"));
        Assert.AreEqual(3600, cal.events[1].durationSeconds);
        Assert.GreaterOrEqual(cal.causalLinks.Count, 1);
        Assert.AreEqual(cal.events[1].id, cal.causalLinks[0].fromEventId);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(calGo);
    }

    [Test]
    public void Bootstrap_WiresCareerWardenAndGovernmentCompany()
    {
        var go = new GameObject("job_center");
        var stub = go.AddComponent<CivilInstitutionStub>();
        stub.kind = CivilSystemKind.UnemploymentOffice;
        var boot = go.AddComponent<UnemploymentOfficeBootstrap>();
        boot.Ensure();
        Assert.IsNotNull(go.GetComponent<CareerWarden>());
        Assert.IsNotNull(go.GetComponent<AuthWarden>());
        Assert.AreEqual("government", go.GetComponent<CompanyRegistration>().parentCompanyId);
        var shifts = go.GetComponent<PersonaShiftManager>();
        Assert.IsTrue(shifts.shifts.Exists(s => s.role == "counselor"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void CivilCard_JobSearchChecklist()
    {
        var card = CivilCard.Generate(CivilianDutyKind.JobSearch, "pat");
        Assert.AreEqual("intake", card.dutyChecklist[0]);
        Assert.AreEqual(CivilianDutyKind.JobSearch, card.civicDuty);
    }

    [Test]
    public void RequestCivilian_HonorsQuota()
    {
        var go = new GameObject("office");
        var warden = go.AddComponent<CareerWarden>();
        warden.demographics = new CivilianDemographics { cityPopulation = 10, unemploymentRate01 = 0.1f };
        var a = warden.RequestCivilianPaperDoll("a", 1);
        Assert.IsNotNull(a);
        var b = warden.RequestCivilianPaperDoll("b", 2);
        Assert.IsNull(b);
        Object.DestroyImmediate(go);
        if (a != null) Object.DestroyImmediate(a);
    }
}
