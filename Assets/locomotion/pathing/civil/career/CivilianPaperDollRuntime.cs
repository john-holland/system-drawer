using UnityEngine;

/// <summary>Runtime binding of a CivilianPaperDoll onto an actor.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Civilian Paper Doll Runtime")]
public sealed class CivilianPaperDollRuntime : MonoBehaviour
{
    public CivilianPaperDoll doll;
    public PersonalSchedule schedule;
    public EducationalTravelAgent educationalPlan;

    void Awake()
    {
        if (schedule == null)
            schedule = GetComponent<PersonalSchedule>();
        if (educationalPlan == null)
            educationalPlan = GetComponent<EducationalTravelAgent>();
        if (doll != null && educationalPlan != null)
            doll.educationalPlan = educationalPlan;
        if (doll != null && schedule != null && !string.IsNullOrEmpty(doll.personaKey))
            schedule.personaKey = doll.personaKey;
    }
}
