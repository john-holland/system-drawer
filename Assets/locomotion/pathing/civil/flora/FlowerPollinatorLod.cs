using UnityEngine;

/// <summary>
/// Very-near pollinator LOD. Anther contact stamps paint; the proboscis stream is any Behaviour
/// (assign a DrinkStreamRenderer) and is enabled only inside the near radius.
/// </summary>
[AddComponentMenu("Locomotion/Civil/Flower Pollinator LOD")]
public sealed class FlowerPollinatorLod : MonoBehaviour
{
    public FlowerTravelAgent flower;
    public string pollinatorFlockId = "pollinator";
    [Min(0.01f)] public float nearRadius = 0.35f;
    public PaintBrushDefinition antherBrush;
    public PaintCanvasLayerStack canvas;
    public Behaviour proboscisStream;
    [Range(0f, 1f)] public float beeLoad01;
    public bool veryNear;

    void Awake()
    {
        if (flower == null)
            flower = GetComponent<FlowerTravelAgent>();
    }

    void LateUpdate()
    {
        veryNear = AnyPollinatorVeryNear();
        if (proboscisStream != null)
        {
            proboscisStream.enabled = veryNear;
            if (veryNear)
                proboscisStream.transform.position = NectaryWorld();
        }
        if (!veryNear)
            beeLoad01 = 0f;
    }

    public Vector3 NectaryWorld()
    {
        var nectaries = flower != null && flower.organism != null ? flower.organism.nectaries : null;
        if (nectaries != null && nectaries.Count > 0 && nectaries[0] != null)
            return transform.TransformPoint(nectaries[0].localPlacement);
        return transform.position;
    }

    public bool VeryNear(Vector3 pollinatorPos)
    {
        var step = flower != null ? flower.SelectedStep : null;
        if (step == null || !step.attractsPollinators) return false;
        float r = nearRadius;
        return (pollinatorPos - transform.position).sqrMagnitude <= r * r;
    }

    public bool AnyPollinatorVeryNear()
    {
        var peers = TravelAgentRegistry.All;
        for (int i = 0; i < peers.Count; i++)
        {
            var agent = peers[i];
            if (agent == null || agent.flockGroupId != pollinatorFlockId) continue;
            if (VeryNear(agent.transform.position)) return true;
        }
        return false;
    }

    public float LoadAnther(Vector2 uv, bool contact, Vector3 pollinatorPos)
    {
        if (!contact || !VeryNear(pollinatorPos))
        {
            beeLoad01 = 0f;
            return 0f;
        }
        if (canvas != null && antherBrush != null)
            canvas.Stamp(uv, antherBrush.defaultPaintColor, Mathf.Max(0.01f, antherBrush.ferruleRadiusM));
        beeLoad01 = antherBrush != null ? Mathf.Clamp01(antherBrush.saturationSpeed) : 1f;
        return beeLoad01;
    }
}
