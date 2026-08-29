using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Legal Building Bootstrap")]
public sealed class LegalBuildingBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        Ensure();
    }

    public void Ensure()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        if (stub != null)
            stub.kind = CivilSystemKind.CourtHouse;

        var building = GetComponent<LegalBuilding>() ?? gameObject.AddComponent<LegalBuilding>();
        building.EnsureDefaultRooms();
        if (building.requirements == null)
            building.requirements = BuildingRequirementSpec.CreateDefault("courthouse", CivilSystemKind.CourtHouse);

        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        var company = GetComponent<CompanyRegistration>();
        if (string.IsNullOrEmpty(company.companyId))
            company.companyId = "courthouse";
        building.company = company;

        if (GetComponent<PersonaShiftManager>() == null)
            gameObject.AddComponent<PersonaShiftManager>();
        var shifts = GetComponent<PersonaShiftManager>();
        shifts.company = company;
        bool hasJudge = false;
        if (shifts.shifts != null)
        {
            for (int i = 0; i < shifts.shifts.Count; i++)
                if (shifts.shifts[i] != null && shifts.shifts[i].role == "judge")
                    hasJudge = true;
        }
        if (!hasJudge)
        {
            shifts.shifts = new List<PersonaShiftSlot>
            {
                new PersonaShiftSlot { role = "judge", personaKey = "judge", peckingOrder = 1, openCron = "* 8-17 * * 1-5" },
                new PersonaShiftSlot { role = "bailiff", personaKey = "bailiff", peckingOrder = 4, openCron = "* 8-17 * * 1-5" },
                new PersonaShiftSlot { role = "prosecution", personaKey = "prosecution", peckingOrder = 8, openCron = "* 8-17 * * 1-5" },
                new PersonaShiftSlot { role = "defense", personaKey = "defense", peckingOrder = 9, openCron = "* 8-17 * * 1-5" },
                new PersonaShiftSlot { role = "clerk", personaKey = "clerk", peckingOrder = 18, openCron = "* 8-16 * * 1-5" },
                new PersonaShiftSlot { role = "security", personaKey = "security", peckingOrder = 12, openCron = "* 7-18 * * 1-5" }
            };
        }

        if (GetComponent<CivilVenueAmenities>() == null)
            gameObject.AddComponent<CivilVenueAmenities>();
        if (GetComponent<CivilVenueBioRhythmService>() == null)
            gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (GetComponent<CourtWarden>() == null)
            gameObject.AddComponent<CourtWarden>();
        if (GetComponent<CorruptionWarden>() == null)
            gameObject.AddComponent<CorruptionWarden>();
        if (GetComponent<ConstitutionWarden>() == null)
            gameObject.AddComponent<ConstitutionWarden>();
        if (GetComponent<RightsWarden>() == null)
            gameObject.AddComponent<RightsWarden>();
        if (GetComponent<JusticeWarden>() == null)
            gameObject.AddComponent<JusticeWarden>();
        if (GetComponent<LawWarden>() == null)
            gameObject.AddComponent<LawWarden>();
        if (GetComponent<GovernmentWarden>() == null)
            gameObject.AddComponent<GovernmentWarden>();
        if (GetComponent<ThreatWarden>() == null)
            gameObject.AddComponent<ThreatWarden>();
        if (GetComponent<GenevaConventionWarden>() == null)
            gameObject.AddComponent<GenevaConventionWarden>();
        if (GetComponent<CourtSystemBioRhythm>() == null)
            gameObject.AddComponent<CourtSystemBioRhythm>();
        if (GetComponent<LegalSystemTravelAgent>() == null)
            gameObject.AddComponent<LegalSystemTravelAgent>();
        if (GetComponent<CourtroomPixelRuntime>() == null)
            gameObject.AddComponent<CourtroomPixelRuntime>();
        if (GetComponent<NarrativeBindings>() == null)
            gameObject.AddComponent<NarrativeBindings>();

        var court = GetComponent<CourtWarden>();
        court.company = company;
        court.EnsureDefaultPecking();
        building.courtWarden = court;
        building.bioRhythm = GetComponent<CourtSystemBioRhythm>();
        building.bioRhythm.courtWarden = court;

        var rights = GetComponent<RightsWarden>();
        rights.constitutionWarden = GetComponent<ConstitutionWarden>();
        var justice = GetComponent<JusticeWarden>();
        justice.rightsWarden = rights;
        var geneva = GetComponent<GenevaConventionWarden>();
        geneva.threatWarden = GetComponent<ThreatWarden>();
        geneva.rightsWarden = rights;
        geneva.justiceWarden = justice;
        geneva.junta = GetComponent<JuntaRuntime>();
        geneva.consentWarden = GetComponent<ConsentWarden>();
        geneva.romanceWarden = GetComponent<RomanceWarden>();

        var agent = GetComponent<LegalSystemTravelAgent>();
        agent.courtWarden = court;
        agent.corruptionWarden = GetComponent<CorruptionWarden>();
        agent.legalBuilding = building;
        if (agent.steps == null || agent.steps.Count == 0)
            agent.ResolvePath();

        var bindings = GetComponent<NarrativeBindings>();
        if (bindings != null && bindings.bindings != null)
        {
            EnsureKey(bindings, "judge");
            EnsureKey(bindings, "bailiff");
            EnsureKey(bindings, "prosecution");
            EnsureKey(bindings, "defense");
            EnsureKey(bindings, "clerk");
        }
    }

    static void EnsureKey(NarrativeBindings bindings, string key)
    {
        for (int i = 0; i < bindings.bindings.Count; i++)
            if (bindings.bindings[i] != null && bindings.bindings[i].key == key)
                return;
        bindings.bindings.Add(new NarrativeBindings.BindingEntry { key = key });
    }
}
