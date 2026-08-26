using System;
using UnityEngine;

public enum UniversityAgeBracket
{
    LowerSchool = 0,
    UpperSchool = 1,
    Undergrad = 2,
    Graduate = 3
}

public static class UniversityAgeBracketRules
{
    public static int MinAgeYears(UniversityAgeBracket b)
    {
        switch (b)
        {
            case UniversityAgeBracket.LowerSchool: return 8;
            case UniversityAgeBracket.UpperSchool: return 14;
            case UniversityAgeBracket.Undergrad: return 17;
            default: return 13;
        }
    }

    public static int MaxAgeYears(UniversityAgeBracket b)
    {
        switch (b)
        {
            case UniversityAgeBracket.LowerSchool: return 13;
            case UniversityAgeBracket.UpperSchool: return 18;
            case UniversityAgeBracket.Undergrad: return 24;
            default: return 99;
        }
    }

    public static CivilianAgeBand ToCivilianBand(UniversityAgeBracket b)
    {
        return b == UniversityAgeBracket.LowerSchool || b == UniversityAgeBracket.UpperSchool
            ? CivilianAgeBand.Child0To17
            : CivilianAgeBand.Adult18To64;
    }

    public static bool Eligible(UniversityAgeBracket bracket, CivilianPaperDoll doll, int ageYears)
    {
        if (doll == null) return false;
        if (ageYears < MinAgeYears(bracket) || ageYears > MaxAgeYears(bracket))
            return false;
        var band = ToCivilianBand(bracket);
        if (bracket == UniversityAgeBracket.UpperSchool && doll.ageBand == CivilianAgeBand.Adult18To64 && ageYears <= 18)
            return true;
        return doll.ageBand == band;
    }
}

[Serializable]
public sealed class UniversityStaffEntry
{
    public string displayName = "Staff";
    public string job = "teacher";
    public string personaKey = "teacher";
    public string inpaintPrompt;
    public int peckingOrder = 20;
    public string departmentId;
    public string courseId;
    public GameObject actor;

    public RetinuePeckingEntry ToRetinue()
    {
        return new RetinuePeckingEntry
        {
            personaKey = personaKey,
            role = job,
            peckingOrder = peckingOrder,
            actor = actor,
            agencyAffinity = "education"
        };
    }

    public static int DefaultPecking(string job)
    {
        if (string.IsNullOrEmpty(job)) return 45;
        string j = job.Trim().ToLowerInvariant();
        if (j == "headmaster" || j == "head-master") return 1;
        if (j == "dean") return 3;
        if (j.Contains("chair") || j == "department") return 8;
        if (j.Contains("business") || j == "bursar" || j == "registrar") return 10;
        if (j == "teacher" || j == "professor" || j == "instructor") return 18;
        if (j == "assistant" || j == "ta" || j == "teachers-assistant") return 28;
        if (j.Contains("ground") || j.Contains("maintenance") || j == "custodian") return 38;
        return 45;
    }
}

[Serializable]
public sealed class CampusElevationBand
{
    public string id = "street";
    public float depthMinM;
    public float depthMaxM = 15f;
}

[Serializable]
public sealed class CampusRoomSpec
{
    public string roomId = "classroom";
    public string displayName = "Classroom";
    public int floorIndex = 1;
    public string zoneId = "class";
    public LearningStationKind station = LearningStationKind.Class;
    public string sg4dPrompt;
    public string inpaintPrompt;
    public PixelLightMultiSlotCatalog pixelLightSlots;
    public string pixelLightSlotId;
    public Vector3 worldPosition;
}

[Serializable]
public sealed class UniversityCourseSpec
{
    public string courseId = "core";
    public string title = "Core";
    public UniversityAgeBracket ageBracket = UniversityAgeBracket.Undergrad;
    [TextArea] public string contentOutline;
    public string dialogTreeSetId;
    public LearningStationKind station = LearningStationKind.UniversityCourse;
    public string campusRoomId;
    public string[] staffPersonaKeys = Array.Empty<string>();
}
