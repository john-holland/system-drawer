#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Integration: web Lemma Build deeplink JSON → route → form applied on Lemma Properties window.
/// Catches Continuuuum.Editor ↔ SystemDrawer.Lemmas asmdef breaks that silently drop form apply.
/// </summary>
public sealed class DeepLinkIntegrationTests
{
    /// <summary>Same envelope shape Continuuuum API writes to CONTINUUUUM_DEEPLINK_PATH.</summary>
    const string ApiShapedEnvelope = @"{
  ""window"": ""System Drawer/Lemmas/Lemma Build"",
  ""form"": {
    ""lemma"": ""unlock"",
    ""partOfSpeech"": ""verb"",
    ""posTag"": ""verb"",
    ""mechanicalRole"": ""AtomicAction"",
    ""outputTier"": 1,
    ""functionalDescription"": ""Opens a latch"",
    ""mechanismPrompt"": ""door latch"",
    ""synonyms"": [""unbolt"", ""open""],
    ""compositionChildren"": [
      { ""entryId"": ""urn:unity:continuuuum:builtin:v1:/en/noun/object"", ""sortOrder"": 0 }
    ],
    ""properties"": [
      { ""propertyKey"": ""causality-tree"", ""propertyValue"": ""open"" }
    ],
    ""engine"": ""haxe""
  }
}";

    [Test]
    public void WebLemmaBuildWindow_ResolvesToLemmaBuild_NotProperties()
    {
        Assert.AreEqual(
            DeepLinkContract.Target.LemmaBuild,
            DeepLinkContract.ResolveTarget(DeepLinkContract.LemmaBuildWindow, ""));
        Assert.AreEqual(
            DeepLinkContract.Target.LemmaProperties,
            DeepLinkContract.ResolveTarget(DeepLinkContract.LemmaPropertiesWindow, ""));
    }

    [Test]
    public void ApiEnvelope_RoutesAndExtractsForm()
    {
        var window = DeepLinkContract.ParseJsonString(ApiShapedEnvelope, "window");
        Assert.AreEqual(
            DeepLinkContract.Target.LemmaBuild,
            DeepLinkContract.ResolveTarget(window, ""));

        var formJson = DeepLinkContract.ExtractJsonObject(ApiShapedEnvelope, "form");
        Assert.IsFalse(string.IsNullOrEmpty(formJson), "form object missing from envelope");

        var form = JsonUtility.FromJson<LemmaBuildDeeplinkForm>(formJson);
        Assert.IsNotNull(form);
        Assert.AreEqual("unlock", form.lemma);
        Assert.AreEqual("verb", form.posTag);
        Assert.AreEqual("AtomicAction", form.mechanicalRole);
        Assert.AreEqual(1, form.outputTier);
        Assert.AreEqual("haxe", form.engine);
        Assert.IsNotNull(form.synonyms);
        Assert.AreEqual(2, form.synonyms.Length);
    }

    [Test]
    public void OpenOnLemmaBuildTabWithForm_AppliesApiShapedPayload()
    {
        var formJson = DeepLinkContract.ExtractJsonObject(ApiShapedEnvelope, "form");
        VocabularyLemmaPropertyEditorWindow.OpenOnLemmaBuildTabWithForm(formJson);

        var editorWindow = EditorWindow.GetWindow<VocabularyLemmaPropertyEditorWindow>("Lemma Properties");
        Assert.IsNotNull(editorWindow);

        var snapshot = editorWindow.GetLemmaBuildFormSnapshot();
        Assert.AreEqual("unlock", snapshot.lemma);
        Assert.AreEqual("verb", snapshot.posTag);
        Assert.AreEqual("AtomicAction", snapshot.mechanicalRole);
        Assert.AreEqual(1, snapshot.outputTier);
        Assert.AreEqual("Opens a latch", snapshot.functionalDescription);
        Assert.AreEqual("door latch", snapshot.mechanismPrompt);
        Assert.AreEqual("haxe", snapshot.engine);
        Assert.IsNotNull(snapshot.synonyms);
        CollectionAssert.AreEqual(new[] { "unbolt", "open" }, snapshot.synonyms);
        Assert.IsNotNull(snapshot.compositionChildren);
        Assert.AreEqual(1, snapshot.compositionChildren.Length);
        Assert.AreEqual(
            "urn:unity:continuuuum:builtin:v1:/en/noun/object",
            snapshot.compositionChildren[0].entryId);
        Assert.IsNotNull(snapshot.properties);
        Assert.AreEqual(1, snapshot.properties.Length);
        Assert.AreEqual("causality-tree", snapshot.properties[0].propertyKey);
    }

    [Test]
    public void LemmaBuildDeeplinkForm_IsVisibleToContinuuuumEditorAssembly()
    {
        // Compile-time + runtime guard for the asmdef regression that broke deeplinks.
        var t = typeof(LemmaBuildDeeplinkForm);
        Assert.AreEqual("SystemDrawer.Lemmas", t.Assembly.GetName().Name);
        Assert.IsNotNull(typeof(VocabularyLemmaPropertyEditorWindow).GetMethod(
            nameof(VocabularyLemmaPropertyEditorWindow.OpenOnLemmaBuildTabWithForm)));
    }
}
#endif
