using UnityEngine;
using Locomotion.Narrative;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/School Bootstrap")]
public sealed class SchoolBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;
    public UniversityCampusAsset campus;

    void Awake()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        Ensure();
    }

    public void Ensure()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        if (stub != null)
            stub.kind = CivilSystemKind.School;

        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        var company = GetComponent<CompanyRegistration>();
        if (string.IsNullOrEmpty(company.companyId))
            company.companyId = "university";

        if (GetComponent<PersonaShiftManager>() == null)
            gameObject.AddComponent<PersonaShiftManager>();
        var shifts = GetComponent<PersonaShiftManager>();
        shifts.company = company;
        if (shifts.shifts == null || shifts.shifts.Count == 0)
        {
            shifts.shifts = new System.Collections.Generic.List<PersonaShiftSlot>
            {
                new PersonaShiftSlot { role = "headmaster", personaKey = "headmaster", peckingOrder = 1, openCron = "* 7-18 * * 1-5" },
                new PersonaShiftSlot { role = "dean", personaKey = "dean", peckingOrder = 3, openCron = "* 8-17 * * 1-5" },
                new PersonaShiftSlot { role = "teacher", personaKey = "teacher", peckingOrder = 18, openCron = "* 8-16 * * 1-5" },
                new PersonaShiftSlot { role = "assistant", personaKey = "ta", peckingOrder = 28, openCron = "* 8-16 * * 1-5" },
                new PersonaShiftSlot { role = "grounds", personaKey = "grounds", peckingOrder = 38, openCron = "* 6-15 * * 1-6" }
            };
        }

        if (GetComponent<KeycardAccessRegistry>() == null)
            gameObject.AddComponent<KeycardAccessRegistry>();
        if (GetComponent<InnHotelVenueRuntime>() == null)
            gameObject.AddComponent<InnHotelVenueRuntime>();

        if (GetComponent<EducationalTravelAgent>() == null)
            gameObject.AddComponent<EducationalTravelAgent>();
        if (GetComponent<EducationWarden>() == null)
            gameObject.AddComponent<EducationWarden>();
        var warden = GetComponent<EducationWarden>();
        warden.company = company;
        warden.campus = campus;
        warden.travelAgent = GetComponent<EducationalTravelAgent>();
        warden.travelAgent.educationWarden = warden;
        warden.dormVenue = GetComponent<InnHotelVenueRuntime>();
        warden.dormKeys = GetComponent<KeycardAccessRegistry>();
        warden.BindStaffPecking();
        if (GetComponent<CampusPixelRuntime>() == null)
            gameObject.AddComponent<CampusPixelRuntime>();
        var campusRt = GetComponent<CampusPixelRuntime>();
        campusRt.campus = campus;
        campusRt.SendRoomPrompts();
        if (GetComponent<NarrativeBindings>() == null)
            gameObject.AddComponent<NarrativeBindings>();
        EnsureDefaultDialogBindings();
    }

    void EnsureDefaultDialogBindings()
    {
        var bindings = GetComponent<NarrativeBindings>();
        if (bindings == null || bindings.bindings == null) return;
        EnsureKey(bindings, "headmaster");
        EnsureKey(bindings, "dean");
        EnsureKey(bindings, "teacher");
        EnsureKey(bindings, "ta");
        EnsureKey(bindings, "student");
    }

    static void EnsureKey(NarrativeBindings bindings, string key)
    {
        for (int i = 0; i < bindings.bindings.Count; i++)
            if (bindings.bindings[i] != null && bindings.bindings[i].key == key)
                return;
        bindings.bindings.Add(new NarrativeBindings.BindingEntry { key = key });
    }
}
