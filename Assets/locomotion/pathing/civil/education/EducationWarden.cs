using System.Collections.Generic;
using UnityEngine;

public enum EducationWardenAction
{
    Pass = 0,
    Probation = 1,
    Fail = 2
}

/// <summary>
/// Academic authority. Grades students from EducationalTravelAgent in-paint, CivilianPaperDoll out-paint, and emergent attendance/board.
/// Does not replace CareerWarden.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Education Warden")]
public sealed class EducationWarden : MonoBehaviour
{
    public UniversityCampusAsset campus;
    public UniversityCurriculumAsset curriculum;
    public EducationalTravelAgent travelAgent;
    public CompanyRegistration company;
    public ThreatWarden threatWarden;
    public TrafficWarden trafficWarden;
    public SafetyWardenPlannerService safetyWarden;
    public InnHotelVenueRuntime dormVenue;
    public KeycardAccessRegistry dormKeys;
    public string threatAgencyId = "education";
    public EducationWardenAction lastRecommendation = EducationWardenAction.Pass;
    [Range(0f, 1f)] public float lastGrade01;
    [Range(0f, 1f)] public float inpaintWeight = 0.35f;
    [Range(0f, 1f)] public float outpaintWeight = 0.4f;
    [Range(0f, 1f)] public float emergentWeight = 0.25f;

    public List<CivilianPaperDoll> enrolled = new List<CivilianPaperDoll>();

    void Awake()
    {
        if (travelAgent == null)
            travelAgent = GetComponent<EducationalTravelAgent>();
        if (company == null)
            company = GetComponent<CompanyRegistration>();
        if (curriculum == null && campus != null)
            curriculum = campus.curriculum;
        if (dormVenue == null)
            dormVenue = GetComponent<InnHotelVenueRuntime>();
        if (dormKeys == null)
            dormKeys = GetComponent<KeycardAccessRegistry>();
        BindStaffPecking();
    }

    public void BindStaffPecking()
    {
        var cur = curriculum != null ? curriculum : campus != null ? campus.curriculum : null;
        if (cur == null) return;
        cur.EnsureDefaultStaffPecking();
        var threat = threatWarden != null ? threatWarden : GetComponent<ThreatWarden>();
        threat?.SetRetinuePeckingOrder(cur.ToRetinue());
        if (company == null) return;
        var staff = cur.staff;
        if (staff == null) return;
        for (int i = 0; i < staff.Count; i++)
        {
            var s = staff[i];
            if (s == null) continue;
            company.TryHire(s.personaKey, s.job, s.peckingOrder);
        }
    }

    public bool Enroll(CivilianPaperDoll doll, UniversityAgeBracket bracket, int ageYears, EducationalTravelAgent agent = null)
    {
        if (doll == null) return false;
        if (!UniversityAgeBracketRules.Eligible(bracket, doll, ageYears))
            return false;
        agent = agent != null ? agent : travelAgent;
        if (agent == null)
            agent = GetComponent<EducationalTravelAgent>() ?? gameObject.AddComponent<EducationalTravelAgent>();
        travelAgent = agent;
        var cur = curriculum != null ? curriculum : campus != null ? campus.curriculum : null;
        agent.educationWarden = this;
        agent.ResolveCourseLoad(doll, cur, campus, bracket);
        doll.employment = CivilianEmploymentStatus.Student;
        doll.educationalPlan = agent;
        if (!enrolled.Contains(doll))
            enrolled.Add(doll);
        lastRecommendation = EducationWardenAction.Pass;
        return true;
    }

    public float[] GradeStudent(
        CivilianPaperDoll doll,
        EducationalTravelAgent agent = null,
        float attendance01 = 0.8f,
        float crowdDelay01 = 0f)
    {
        agent = agent != null ? agent : (doll != null ? doll.educationalPlan : travelAgent);
        float[] outpaint = doll != null ? doll.Expected01() : new[] { 0.55f, 0.55f, 0.55f, 0.4f };
        float inpaint = 0.45f;
        if (agent != null)
        {
            var step = agent.SelectedStep;
            if (step != null && step.hasInpaint)
                inpaint = 0.78f;
            else if (step != null)
                inpaint = 0.5f;
        }
        float sleep = 0.5f;
        var inn = dormVenue != null ? dormVenue : GetComponent<InnHotelVenueRuntime>();
        if (inn != null)
            sleep = inn.SleepInPaintComfort01();
        float emergent = Mathf.Clamp01(attendance01 * 0.6f + sleep * 0.3f + (1f - crowdDelay01) * 0.1f);
        float wIn = inpaintWeight;
        float wOut = outpaintWeight;
        float wEm = emergentWeight;
        float wSum = Mathf.Max(1e-4f, wIn + wOut + wEm);
        wIn /= wSum;
        wOut /= wSum;
        wEm /= wSum;

        var grade = new float[4];
        for (int i = 0; i < 4; i++)
            grade[i] = Mathf.Clamp01(outpaint[i] * wOut + inpaint * wIn + emergent * wEm);
        lastGrade01 = (grade[0] + grade[1] + grade[2] + grade[3]) * 0.25f;
        lastRecommendation = OverFireLimit(doll, grade)
            ? EducationWardenAction.Fail
            : lastGrade01 < 0.45f
                ? EducationWardenAction.Probation
                : EducationWardenAction.Pass;
        return grade;
    }

    public bool OverFireLimit(CivilianPaperDoll doll, float[] grade = null)
    {
        if (doll == null) return false;
        grade = grade ?? GradeStudent(doll);
        var red = doll.FireLimit01();
        for (int i = 0; i < 4; i++)
            if (grade[i] > red[i] + 1e-4f)
                return true;
        return false;
    }

    public Vector3 ClassroomGoal(string courseId)
    {
        var cur = curriculum != null ? curriculum : campus != null ? campus.curriculum : null;
        var course = cur != null ? cur.FindCourse(courseId) : null;
        if (course != null && campus != null)
            return campus.RoomWorld(course.campusRoomId);
        if (campus != null)
            return campus.RoomWorld("lecture");
        return transform.position;
    }
}
