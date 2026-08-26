using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UniversityCurriculum", menuName = "Locomotion/Civil/University Curriculum")]
public sealed class UniversityCurriculumAsset : ScriptableObject
{
    public List<UniversityCourseSpec> courses = new List<UniversityCourseSpec>();
    public List<UniversityStaffEntry> staff = new List<UniversityStaffEntry>();

    public UniversityCourseSpec FindCourse(string courseId)
    {
        if (courses == null || string.IsNullOrEmpty(courseId)) return null;
        for (int i = 0; i < courses.Count; i++)
            if (courses[i] != null && courses[i].courseId == courseId)
                return courses[i];
        return null;
    }

    public List<UniversityStaffEntry> StaffForCourse(string courseId)
    {
        var list = new List<UniversityStaffEntry>();
        if (staff == null) return list;
        for (int i = 0; i < staff.Count; i++)
        {
            var s = staff[i];
            if (s != null && s.courseId == courseId)
                list.Add(s);
        }
        list.Sort((a, b) => a.peckingOrder.CompareTo(b.peckingOrder));
        return list;
    }

    public bool CourseHasTeacherAndAssistant(string courseId)
    {
        var list = StaffForCourse(courseId);
        bool teacher = false, assistant = false;
        for (int i = 0; i < list.Count; i++)
        {
            int p = list[i].peckingOrder;
            if (p >= 15 && p <= 24) teacher = true;
            if (p >= 25 && p <= 34) assistant = true;
            string job = list[i].job != null ? list[i].job.ToLowerInvariant() : "";
            if (job == "teacher" || job == "professor") teacher = true;
            if (job == "assistant" || job == "ta") assistant = true;
        }
        return teacher && assistant;
    }

    public void EnsureDefaultStaffPecking()
    {
        if (staff == null) return;
        for (int i = 0; i < staff.Count; i++)
        {
            if (staff[i] == null) continue;
            if (staff[i].peckingOrder <= 0)
                staff[i].peckingOrder = UniversityStaffEntry.DefaultPecking(staff[i].job);
        }
    }

    public List<RetinuePeckingEntry> ToRetinue()
    {
        var list = new List<RetinuePeckingEntry>();
        if (staff == null) return list;
        for (int i = 0; i < staff.Count; i++)
            if (staff[i] != null)
                list.Add(staff[i].ToRetinue());
        return list;
    }
}
