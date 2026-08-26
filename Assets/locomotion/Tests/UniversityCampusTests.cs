#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class UniversityCampusTests
{
    [Test]
    public void AgeBracket_EnrollmentGate_RejectsWrongBand()
    {
        var adult = ScriptableObject.CreateInstance<CivilianPaperDoll>();
        adult.ageBand = CivilianAgeBand.Adult18To64;
        Assert.IsTrue(UniversityAgeBracketRules.Eligible(UniversityAgeBracket.Undergrad, adult, 19));
        Assert.IsFalse(UniversityAgeBracketRules.Eligible(UniversityAgeBracket.LowerSchool, adult, 19));
        Assert.IsFalse(UniversityAgeBracketRules.Eligible(UniversityAgeBracket.Undergrad, adult, 12));

        var child = ScriptableObject.CreateInstance<CivilianPaperDoll>();
        child.ageBand = CivilianAgeBand.Child0To17;
        Assert.IsTrue(UniversityAgeBracketRules.Eligible(UniversityAgeBracket.LowerSchool, child, 10));
        Assert.IsFalse(UniversityAgeBracketRules.Eligible(UniversityAgeBracket.Graduate, child, 10));
        Object.DestroyImmediate(adult);
        Object.DestroyImmediate(child);
    }

    [Test]
    public void Curriculum_TeacherAndAssistant_ShareCourseLoad()
    {
        var cur = ScriptableObject.CreateInstance<UniversityCurriculumAsset>();
        cur.courses.Add(new UniversityCourseSpec { courseId = "chem-101", title = "Chem", campusRoomId = "lecture" });
        cur.staff.Add(new UniversityStaffEntry
        {
            displayName = "Prof",
            job = "teacher",
            courseId = "chem-101",
            peckingOrder = 18,
            inpaintPrompt = "lecture hall chalk"
        });
        cur.staff.Add(new UniversityStaffEntry
        {
            displayName = "TA",
            job = "ta",
            courseId = "chem-101",
            peckingOrder = 28
        });
        Assert.IsTrue(cur.CourseHasTeacherAndAssistant("chem-101"));
        Assert.AreEqual(2, cur.StaffForCourse("chem-101").Count);
        Object.DestroyImmediate(cur);
    }

    [Test]
    public void EducationWarden_BlendsInpaintVsDollOutpaint_AndOverFireFails()
    {
        var go = new GameObject("School");
        try
        {
            var campus = UniversityCampusAsset.CreateBoardingDefaults();
            campus.rooms[0].worldPosition = new Vector3(10f, 0f, 4f);
            campus.rooms[0].inpaintPrompt = "sg4d lecture";
            var cur = ScriptableObject.CreateInstance<UniversityCurriculumAsset>();
            cur.courses.Add(new UniversityCourseSpec
            {
                courseId = "chem-101",
                ageBracket = UniversityAgeBracket.Undergrad,
                campusRoomId = "lecture",
                station = LearningStationKind.UniversityCourse
            });
            cur.staff.Add(new UniversityStaffEntry
            {
                job = "teacher",
                courseId = "chem-101",
                peckingOrder = 18,
                inpaintPrompt = "podium"
            });
            campus.curriculum = cur;

            var doll = ScriptableObject.CreateInstance<CivilianPaperDoll>();
            doll.ageBand = CivilianAgeBand.Adult18To64;
            doll.expected01 = new[] { 0.8f, 0.8f, 0.8f, 0.7f };
            doll.fireLimit01 = new[] { 0.95f, 0.95f, 0.95f, 0.95f };

            var warden = go.AddComponent<EducationWarden>();
            warden.campus = campus;
            warden.curriculum = cur;
            Assert.IsTrue(warden.Enroll(doll, UniversityAgeBracket.Undergrad, 20));
            Assert.AreEqual(CivilianEmploymentStatus.Student, doll.employment);
            Assert.IsNotNull(doll.educationalPlan);
            Assert.Greater(doll.educationalPlan.steps.Count, 0);
            Assert.IsTrue(doll.educationalPlan.steps[0].hasInpaint);

            float[] withInpaint = warden.GradeStudent(doll, doll.educationalPlan, 0.9f, 0f);
            doll.educationalPlan.steps[0].hasInpaint = false;
            float[] without = warden.GradeStudent(doll, doll.educationalPlan, 0.9f, 0f);
            Assert.Greater(withInpaint[0], without[0]);

            doll.fireLimit01 = new[] { 0.05f, 0.05f, 0.05f, 0.05f };
            warden.GradeStudent(doll, doll.educationalPlan, 0.9f, 0f);
            Assert.IsTrue(warden.OverFireLimit(doll));
            Assert.AreEqual(EducationWardenAction.Fail, warden.lastRecommendation);

            Object.DestroyImmediate(campus);
            Object.DestroyImmediate(cur);
            Object.DestroyImmediate(doll);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SchoolBootstrap_WiresWardenHotelAndDialogMaps()
    {
        var go = new GameObject("UniStub");
        try
        {
            go.AddComponent<CivilInstitutionStub>().kind = CivilSystemKind.School;
            var boot = go.AddComponent<SchoolBootstrap>();
            boot.Ensure();
            Assert.IsNotNull(go.GetComponent<EducationWarden>());
            Assert.IsNotNull(go.GetComponent<InnHotelVenueRuntime>());
            Assert.IsNotNull(go.GetComponent<KeycardAccessRegistry>());
            var bindings = go.GetComponent<Locomotion.Narrative.NarrativeBindings>();
            Assert.IsNotNull(bindings);
            Assert.IsTrue(bindings.bindings.Exists(b => b != null && b.key == "headmaster"));
            Assert.IsTrue(bindings.bindings.Exists(b => b != null && b.key == "teacher"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
