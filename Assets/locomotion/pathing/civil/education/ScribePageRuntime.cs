using UnityEngine;

/// <summary>Loads a scribe page body onto an optional PenInkDrawingTarget / dialog surface.</summary>
[AddComponentMenu("Locomotion/Civil/Scribe Page Runtime")]
public sealed class ScribePageRuntime : MonoBehaviour
{
    public ScribePaperDoll doll;
    public PenInkDrawingTarget drawingTarget;
    public string configId;
    public int pageIndex;
    public string anchorKey;
    [TextArea] public string bodyText;
    public string format = "plain";
    public PenInkDrawingTarget.SourceKind sourceKind = PenInkDrawingTarget.SourceKind.Text;
    public Texture2D sourceImage;
    public string dialogTreeSetId;

    public void ApplyPage(string text, string fmt = null, string anchor = null)
    {
        sourceKind = PenInkDrawingTarget.SourceKind.Text;
        bodyText = text ?? "";
        if (!string.IsNullOrEmpty(fmt))
            format = fmt;
        if (anchor != null)
            anchorKey = anchor;
        ApplyToDrawingTarget();
        ApplyDialogBindings();
    }

    public void ApplyImage(Texture2D image, string anchor = null)
    {
        sourceKind = PenInkDrawingTarget.SourceKind.Image;
        sourceImage = image;
        if (anchor != null)
            anchorKey = anchor;
        ApplyToDrawingTarget();
        ApplyDialogBindings();
    }

    public void ApplyToDrawingTarget()
    {
        if (drawingTarget == null)
            return;
        drawingTarget.sourceKind = sourceKind;
        drawingTarget.sourceText = bodyText ?? "";
        drawingTarget.sourceImage = sourceImage;
    }

    public void ApplyDialogBindings()
    {
        var bindings = GetComponent<Locomotion.Narrative.NarrativeBindings>();
        if (bindings == null)
            bindings = gameObject.AddComponent<Locomotion.Narrative.NarrativeBindings>();
        EnsureKey(bindings, "head-scribe");
        EnsureKey(bindings, "copyist");
        EnsureKey(bindings, "scribe");
        if (doll != null && !string.IsNullOrEmpty(doll.personaKey))
            EnsureKey(bindings, doll.personaKey);
    }

    static void EnsureKey(Locomotion.Narrative.NarrativeBindings bindings, string key)
    {
        if (bindings.bindings == null)
            bindings.bindings = new System.Collections.Generic.List<Locomotion.Narrative.NarrativeBindings.BindingEntry>();
        for (int i = 0; i < bindings.bindings.Count; i++)
            if (bindings.bindings[i] != null && bindings.bindings[i].key == key)
                return;
        bindings.bindings.Add(new Locomotion.Narrative.NarrativeBindings.BindingEntry { key = key });
    }
}
