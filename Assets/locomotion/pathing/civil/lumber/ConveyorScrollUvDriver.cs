using UnityEngine;

/// <summary>Scrolls belt UVs from <see cref="RopeWindingController"/> via <see cref="RopeSystem.WindRateMps"/>.</summary>
[AddComponentMenu("Locomotion/Civil/Conveyor Scroll UV")]
public sealed class ConveyorScrollUvDriver : MonoBehaviour
{
    public RopeSystem rope;
    public Renderer beltRenderer;
    public float uvScale = 1f;
    public float fallbackRateMps = 0.25f;
    float _offset;

    public float OffsetV => _offset;

    void Awake()
    {
        if (rope == null)
            rope = GetComponent<RopeSystem>();
        if (beltRenderer == null)
            beltRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        float rate = rope != null ? rope.WindRateMps : fallbackRateMps;
        _offset += rate * Mathf.Max(0f, dt) * uvScale;
        if (beltRenderer == null) return;
        var mat = Application.isPlaying ? beltRenderer.material : beltRenderer.sharedMaterial;
        if (mat == null) return;
        if (mat.HasProperty("_ScrollV"))
            mat.SetFloat("_ScrollV", _offset);
        else if (mat.HasProperty("_MainTex"))
            mat.SetTextureOffset("_MainTex", new Vector2(0f, _offset));
    }
}
