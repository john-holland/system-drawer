#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class ScribeSystemTests
{
    [Test]
    public void ScribeCard_PeckingAndPageAnchor()
    {
        var card = ScribeCard.Generate(ScribeActivity.Copy, "charter", 2, "lemma-open");
        Assert.IsTrue(card.isScribeGoal);
        Assert.AreEqual(2, card.pageIndex);
        Assert.AreEqual("lemma-open", card.anchorKey);
        Assert.AreEqual(ScribeActivity.Copy, card.activity);
        var snap = CardHistorySnapshot.FromCard(card, "solver", "active");
        Assert.IsTrue(snap.isScribeGoal);
    }

    [Test]
    public void ScribePaperDoll_HeadScribePecksAboveCopyist()
    {
        var head = ScribePaperDoll.CreateHeadScribe();
        var copyist = ScriptableObject.CreateInstance<ScribePaperDoll>();
        copyist.personaKey = "copyist";
        copyist.peckingOrder = 22;
        Assert.Less(head.peckingOrder, copyist.peckingOrder);
        Object.DestroyImmediate(head);
        Object.DestroyImmediate(copyist);
    }

    [Test]
    public void ScribePageRuntime_AppliesBodyAndDialogMaps()
    {
        var go = new GameObject("Scribe");
        try
        {
            var rt = go.AddComponent<ScribePageRuntime>();
            rt.ApplyPage("In the beginning", "odt", "bookmark-start");
            Assert.AreEqual("In the beginning", rt.bodyText);
            Assert.AreEqual("odt", rt.format);
            var bindings = go.GetComponent<Locomotion.Narrative.NarrativeBindings>();
            Assert.IsNotNull(bindings);
            Assert.IsTrue(bindings.bindings.Exists(b => b != null && b.key == "head-scribe"));
            Assert.IsTrue(bindings.bindings.Exists(b => b != null && b.key == "copyist"));
            var tex = new Texture2D(2, 2);
            rt.ApplyImage(tex, "illum");
            Assert.AreEqual(PenInkDrawingTarget.SourceKind.Image, rt.sourceKind);
            Assert.AreSame(tex, rt.sourceImage);
            Assert.AreEqual("illum", rt.anchorKey);
            Object.DestroyImmediate(tex);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
