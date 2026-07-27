using UnityEngine;

/// <summary>
/// Rope-sibling scroll of paper: rolling cylinder sized by sheet props; perforation / sheet / empty textures.
/// Not rope_grapple semantics.
/// </summary>
[AddComponentMenu("Locomotion/Bathroom/Paper Scroll System")]
public sealed class PaperScrollSystem : MonoBehaviour
{
    [Header("Sheet physics")]
    public float sheetLengthM = 0.1f;
    public float sheetWidthM = 0.1f;
    public float sheetThicknessM = 0.0002f;
    public int sheetsRemaining = 200;
    public float coreRadiusM = 0.02f;

    [Header("Textures")]
    public Texture2D perforationTex;
    public Texture2D sheetTex;
    public Texture2D emptySheetTex;
    public Renderer rollRenderer;

    [Header("Runtime")]
    public float offRollSheetLengthM;
    public float unwindRateMps = 0.2f;

    float _woundRadius;
    MaterialPropertyBlock _mpb;
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    static readonly int EmptyBlendId = Shader.PropertyToID("_EmptyBlend");

    public float WoundRadiusM => _woundRadius;
    public bool HasSheets => sheetsRemaining > 0;

    void Awake()
    {
        RecalcRadius();
        _mpb = new MaterialPropertyBlock();
    }

    void RecalcRadius()
    {
        float area = sheetsRemaining * sheetLengthM * sheetThicknessM;
        _woundRadius = Mathf.Sqrt(coreRadiusM * coreRadiusM + Mathf.Max(0f, area / Mathf.PI));
    }

    /// <summary>Pull n sheets (3–5 typical); returns length unwound.</summary>
    public float PullSheets(int count)
    {
        int n = Mathf.Clamp(count, 0, sheetsRemaining);
        sheetsRemaining -= n;
        float len = n * sheetLengthM;
        offRollSheetLengthM += len;
        RecalcRadius();
        BindTextures();
        return len;
    }

    public void TickUnwind(float dt)
    {
        if (sheetsRemaining <= 0)
        {
            BindTextures();
            return;
        }
        float want = unwindRateMps * Mathf.Max(0f, dt);
        while (want > 0f && sheetsRemaining > 0)
        {
            float take = Mathf.Min(want, sheetLengthM - (offRollSheetLengthM % sheetLengthM));
            if (take < 1e-5f) take = Mathf.Min(want, sheetLengthM);
            offRollSheetLengthM += take;
            want -= take;
            if (offRollSheetLengthM >= sheetLengthM)
            {
                sheetsRemaining = Mathf.Max(0, sheetsRemaining - 1);
                offRollSheetLengthM -= sheetLengthM;
            }
        }
        RecalcRadius();
        BindTextures();
    }

    void BindTextures()
    {
        if (rollRenderer == null) return;
        rollRenderer.GetPropertyBlock(_mpb);
        // As last sheets leave, blend toward empty sheet texture around the roll.
        float emptyBlend = sheetsRemaining <= 1 ? 1f : (sheetsRemaining < 5 ? (5 - sheetsRemaining) / 5f : 0f);
        if (sheetsRemaining <= 0 && emptySheetTex != null)
            _mpb.SetTexture(MainTexId, emptySheetTex);
        else if (sheetTex != null)
            _mpb.SetTexture(MainTexId, sheetTex);
        _mpb.SetFloat(EmptyBlendId, emptyBlend);
        rollRenderer.SetPropertyBlock(_mpb);
        transform.localScale = new Vector3(_woundRadius * 2f, sheetWidthM, _woundRadius * 2f);
    }
}
