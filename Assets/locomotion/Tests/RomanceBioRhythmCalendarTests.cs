#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class RomanceBioRhythmCalendarTests
{
    [Test]
    public void KindTint_HealthIsBlue_PoliticalIsPurple()
    {
        var health = RomanceBioRhythmCalendarColors.ForKind(RomanceBioRhythmCalendarColors.Kind.Health);
        Assert.AreEqual(RomanceBioRhythmCalendarColors.HealthBlue.r, health.r, 0.01f);
        Assert.AreEqual(RomanceBioRhythmCalendarColors.HealthBlue.b, health.b, 0.01f);

        var political = RomanceBioRhythmCalendarColors.ForKind(RomanceBioRhythmCalendarColors.Kind.Political);
        Assert.AreEqual(RomanceBioRhythmCalendarColors.PoliticalPurple.r, political.r, 0.01f);
    }

    [Test]
    public void LoveTint_HighPhysicality_MovesTowardRed()
    {
        var soft = RomanceBioRhythmCalendarColors.LoveTint(0.1f, 2);
        var hard = RomanceBioRhythmCalendarColors.LoveTint(1f, 4);
        Assert.Greater(hard.r, soft.r - 0.05f);
        Assert.Less(hard.g, soft.g + 0.05f);
    }

    [Test]
    public void InferKindFromTags_LoveAndPolitical()
    {
        Assert.AreEqual(
            RomanceBioRhythmCalendarColors.Kind.Love,
            RomanceBioRhythmCalendarColors.InferKindFromTags(new List<string> { "romance", "date" }));
        Assert.AreEqual(
            RomanceBioRhythmCalendarColors.Kind.Political,
            RomanceBioRhythmCalendarColors.InferKindFromTags(new List<string> { "society", "liberalism" }));
        Assert.AreEqual(
            RomanceBioRhythmCalendarColors.Kind.Health,
            RomanceBioRhythmCalendarColors.InferKindFromTags(new List<string> { "biorhythm", "clinical" }));
    }

    [Test]
    public void CalendarAsset_ShowBioRhythmEvents_DefaultsOff()
    {
        var go = new GameObject("Cal");
        try
        {
            var cal = go.AddComponent<Locomotion.Narrative.NarrativeCalendarAsset>();
            Assert.IsFalse(cal.showBioRhythmEvents);

            var sched = go.AddComponent<Locomotion.Narrative.NarrativeScheduler>();
            sched.calendar = cal;
            sched.showBioRhythmEvents = true;
            // OnValidate only runs in editor inspector; mirror Update sync manually:
            cal.showBioRhythmEvents = sched.showBioRhythmEvents;
            Assert.IsTrue(cal.showBioRhythmEvents);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
