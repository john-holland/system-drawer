using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Unemployment Office Bootstrap")]
public sealed class UnemploymentOfficeBootstrap : MonoBehaviour
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
            stub.kind = CivilSystemKind.UnemploymentOffice;

        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        var company = GetComponent<CompanyRegistration>();
        company.parentCompanyId = "government";
        if (string.IsNullOrEmpty(company.companyId))
            company.companyId = "unemployment_office";

        if (GetComponent<PersonaShiftManager>() == null)
            gameObject.AddComponent<PersonaShiftManager>();
        var shifts = GetComponent<PersonaShiftManager>();
        shifts.company = company;
        bool airportDefault = shifts.shifts != null && shifts.shifts.Count > 0 &&
                              !string.IsNullOrEmpty(shifts.shifts[0].role) &&
                              shifts.shifts[0].role.StartsWith("tsa");
        if (shifts.shifts == null || shifts.shifts.Count == 0 || airportDefault)
        {
            shifts.shifts = new System.Collections.Generic.List<PersonaShiftSlot>
            {
                new PersonaShiftSlot { role = "counselor", personaKey = "counselor", peckingOrder = 5, openCron = "* 8-17 * * 1-5" },
                new PersonaShiftSlot { role = "intake", personaKey = "intake_clerk", peckingOrder = 15, openCron = "* 8-17 * * 1-5" }
            };
        }

        if (GetComponent<AuthWarden>() == null)
            gameObject.AddComponent<AuthWarden>();
        if (GetComponent<CareerWarden>() == null)
            gameObject.AddComponent<CareerWarden>();
        var warden = GetComponent<CareerWarden>();
        warden.company = company;
        if (GetComponent<EducationalTravelAgent>() == null)
            gameObject.AddComponent<EducationalTravelAgent>();
        GetComponent<EducationalTravelAgent>().warden = warden;
    }
}
