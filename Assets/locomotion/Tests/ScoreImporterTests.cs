#if UNITY_EDITOR
using Locomotion.Audio;
using NUnit.Framework;

public class ScoreImporterTests
{
    [Test]
    public void TabImporter_ParsesFrets()
    {
        var doc = TabScoreImporter.ImportText("e|--0--2--3--|\nB|--1--1--0--|", 120f);
        Assert.Greater(doc.events.Length, 0);
        Assert.AreEqual("guitar", doc.partNames[0]);
    }

    [Test]
    public void MusicXmlImporter_ParsesPitch()
    {
        string xml = @"<note><pitch><step>C</step><octave>4</octave></pitch><duration>4</duration></note>";
        var doc = MusicXmlImporter.ImportXml(xml, 120f);
        Assert.AreEqual(1, doc.events.Length);
        Assert.AreEqual(60, doc.events[0].midiNote);
    }

    [Test]
    public void Ocr_Disabled_ReturnsEmpty()
    {
        OcrSheetMusicImporter.EnableOcrSheetMusic = false;
        var doc = OcrSheetMusicImporter.ImportFromImage(null);
        Assert.AreEqual(0, doc.events.Length);
    }
}
#endif
