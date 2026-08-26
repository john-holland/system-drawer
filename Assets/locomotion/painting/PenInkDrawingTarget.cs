using System.Collections.Generic;
using UnityEngine;
using SdfMax;
using Locomotion.Audio;

/// <summary>
/// Drawing IK target from text or image. Unknown code points become box U+25A1 / replacement U+FFFD.
/// IK train requires understandingConfirmed.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Pen Ink Drawing Target")]
[ExecuteAlways]
public sealed class PenInkDrawingTarget : MonoBehaviour
{
    public const int BoxCodePoint = 0x25A1;
    public const int ReplacementCodePoint = 0xFFFD;

    public enum SourceKind
    {
        Text,
        Image
    }

    public SourceKind sourceKind = SourceKind.Text;
    [TextArea] public string sourceText = "ink";
    public Texture2D sourceImage;
    public bool enableOcrImage;
    [Tooltip("Must be checked before IK train.")]
    public bool understandingConfirmed;
    public List<int> codePoints = new List<int>();
    public SdfMaxCompositionAsset strokeSdf;
    public Transform nibTip;
    public PaintCanvas canvas;

    public bool CanTrain => understandingConfirmed && codePoints != null && codePoints.Count > 0;

    public static int VerifyCodePoint(int cp)
    {
        if (cp < 0 || cp > 0x10FFFF)
            return ReplacementCodePoint;
        if (cp == 0xFFFD)
            return ReplacementCodePoint;
        if (char.IsSurrogate((char)Mathf.Clamp(cp, 0, 0xFFFF)) && cp <= 0xFFFF)
            return ReplacementCodePoint;
        if (cp < 32 && cp != 9 && cp != 10 && cp != 13)
            return ReplacementCodePoint;
        if (IsKnownGlyph(cp))
            return cp;
        return BoxCodePoint;
    }

    public static bool IsKnownGlyph(int cp)
    {
        if (cp == 32 || cp == 9 || cp == 10 || cp == 13)
            return true;
        if (cp >= 33 && cp <= 126)
            return true;
        if (cp == BoxCodePoint || cp == ReplacementCodePoint)
            return true;
        return false;
    }

    public void Compile()
    {
        if (codePoints == null)
            codePoints = new List<int>();
        codePoints.Clear();
        if (sourceKind == SourceKind.Image)
            CompileImage();
        else
            CompileText(sourceText);
        RebuildStrokeSdf();
    }

    public void CompileText(string text)
    {
        if (codePoints == null)
            codePoints = new List<int>();
        codePoints.Clear();
        if (string.IsNullOrEmpty(text))
            return;
        for (int i = 0; i < text.Length; i++)
        {
            int cp = char.ConvertToUtf32(text, i);
            if (char.IsHighSurrogate(text[i]))
                i++;
            codePoints.Add(VerifyCodePoint(cp));
        }
    }

    void CompileImage()
    {
        bool prev = OcrSheetMusicImporter.EnableOcrSheetMusic;
        OcrSheetMusicImporter.EnableOcrSheetMusic = enableOcrImage;
        try
        {
            if (!enableOcrImage)
            {
                codePoints.Add(BoxCodePoint);
                return;
            }
            var doc = OcrSheetMusicImporter.ImportFromImage(sourceImage);
            if (doc == null || doc.events == null || doc.events.Length == 0)
            {
                codePoints.Add(BoxCodePoint);
                return;
            }
            string title = doc.title ?? "";
            if (string.IsNullOrEmpty(title) || title == "ocr-disabled" || title == "ocr-sheet")
            {
                codePoints.Add(BoxCodePoint);
                return;
            }
            CompileText(title);
        }
        finally
        {
            OcrSheetMusicImporter.EnableOcrSheetMusic = prev;
        }
    }

    public void RebuildStrokeSdf()
    {
        Bounds acc = new Bounds(Vector3.zero, Vector3.zero);
        bool has = false;
        for (int i = 0; i < codePoints.Count; i++)
        {
            char c = codePoints[i] <= 0xFFFF ? (char)codePoints[i] : (char)BoxCodePoint;
            var mesh = FontFamilyGlyphMesher.ExtrudeCharacter(c, 0.002f, 0.04f);
            if (mesh == null) continue;
            Bounds b = mesh.bounds;
            b.center += Vector3.right * (i * 0.05f);
            if (!has)
            {
                acc = b;
                has = true;
            }
            else
                acc.Encapsulate(b);
            if (Application.isPlaying)
                Object.Destroy(mesh);
            else
                Object.DestroyImmediate(mesh);
        }
        if (!has)
            acc = new Bounds(Vector3.zero, Vector3.one * 0.04f);
        strokeSdf = GlyphSdfMaxComposer.ComposeLegendSubtract(acc, new Vector3(0.08f, 0.01f, 0.04f), "PenInkStrokeSdf");
    }

    public float NibTipError(Vector3 worldTip)
    {
        Vector3 target = transform.position;
        if (canvas != null)
            target = canvas.transform.position;
        return Vector3.Distance(worldTip, target);
    }
}
