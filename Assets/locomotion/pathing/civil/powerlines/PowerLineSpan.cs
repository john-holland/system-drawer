using UnityEngine;

/// <summary>Black cable span between poles — RopeSystem required.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Power Line Span")]
public sealed class PowerLineSpan : MonoBehaviour
{
    public string wireId;
    public string fromPoleId;
    public string toPoleId;
    public Transform fromAnchor;
    public Transform toAnchor;
    public LineRenderer line;
    public Color cableColor = Color.black;
    public float cableRadiusM = 0.02f;
    public bool prebakeDestructible = true;
    [Range(0f, 1f)] public float tension01;
    public PowerLineTensionLemma tensionLemma;
    public RopeSystem ropeSystem;
    [Min(1f)] public float sagFactor = 1.08f;
    public CityPixelGrid stampGrid;

    readonly System.Collections.Generic.List<Vector3> _samples = new System.Collections.Generic.List<Vector3>(16);

    void OnEnable()
    {
        if (string.IsNullOrEmpty(wireId))
            wireId = gameObject.name;
        ResolvePoleIds();
        StreetWireIndex.Register(this);
        EnsureRope();
    }

    void OnDisable() => StreetWireIndex.Unregister(this);

    void Awake()
    {
        if (line == null)
            line = gameObject.GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
        line.startWidth = cableRadiusM * 2f;
        line.endWidth = cableRadiusM * 2f;
        line.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default"));
        line.startColor = cableColor;
        line.endColor = cableColor;
        if (tensionLemma == null)
            tensionLemma = GetComponent<PowerLineTensionLemma>();
        EnsureRope();
    }

    public RopeSystem EnsureRope()
    {
        if (fromAnchor == null || toAnchor == null)
            return ropeSystem;
        if (ropeSystem == null)
            ropeSystem = GetComponent<RopeSystem>() ?? gameObject.AddComponent<RopeSystem>();
        ropeSystem.Config.mode = RopeMode.Grapple;
        float dist = Vector3.Distance(fromAnchor.position, toAnchor.position);
        ropeSystem.BindAnchors(fromAnchor, toAnchor, dist * sagFactor);
        if (GetComponent<RopePathingFootprint>() == null)
            gameObject.AddComponent<RopePathingFootprint>();
        return ropeSystem;
    }

    void ResolvePoleIds()
    {
        if (string.IsNullOrEmpty(fromPoleId) && fromAnchor != null)
        {
            var pole = fromAnchor.GetComponentInParent<UtilityPoleAssembly>();
            if (pole != null) fromPoleId = pole.poleId;
        }
        if (string.IsNullOrEmpty(toPoleId) && toAnchor != null)
        {
            var pole = toAnchor.GetComponentInParent<UtilityPoleAssembly>();
            if (pole != null) toPoleId = pole.poleId;
        }
    }

    void LateUpdate()
    {
        if (fromAnchor == null || toAnchor == null) return;
        if (ropeSystem != null)
        {
            ropeSystem.CollectPathSamples(_samples, 12);
            if (_samples.Count >= 2)
            {
                line.positionCount = _samples.Count;
                for (int i = 0; i < _samples.Count; i++)
                    line.SetPosition(i, _samples[i]);
            }
            tension01 = ropeSystem.NormalizedLoad;
        }
        else
        {
            line.positionCount = 2;
            line.SetPosition(0, fromAnchor.position);
            line.SetPosition(1, toAnchor.position);
            float dist = Vector3.Distance(fromAnchor.position, toAnchor.position);
            tension01 = Mathf.Clamp01((dist - 8f) / 20f);
        }

        if (ropeSystem != null && ropeSystem.IsSnapped)
            StampPowerLinesDown();

        if (prebakeDestructible && tensionLemma != null && tension01 > 0.5f)
        {
            var pole = fromAnchor.GetComponentInParent<UtilityPoleAssembly>();
            pole?.ApplyTension(tension01);
        }
    }

    public Vector3 SampleWorld(float t01)
    {
        t01 = Mathf.Clamp01(t01);
        if (ropeSystem != null)
        {
            ropeSystem.CollectPathSamples(_samples, 16);
            if (_samples.Count >= 2)
            {
                float f = t01 * (_samples.Count - 1);
                int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, _samples.Count - 2);
                return Vector3.Lerp(_samples[i], _samples[i + 1], f - i);
            }
        }
        if (fromAnchor != null && toAnchor != null)
            return Vector3.Lerp(fromAnchor.position, toAnchor.position, t01);
        return transform.position;
    }

    public void StampPowerLinesDown()
    {
        if (stampGrid == null) return;
        stampGrid.EnsureHighwayLayers();
        Vector3 mid = SampleWorld(0.5f);
        if (stampGrid.WorldToCell(mid, out int x, out int y))
            stampGrid.PaintLayerCell(CityPixelLayerKind.PowerLinesDown, 0, x, y);
    }

    public void Configure(Transform from, Transform to)
    {
        fromAnchor = from;
        toAnchor = to;
        ResolvePoleIds();
        EnsureRope();
    }
}
