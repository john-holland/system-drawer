using UnityEngine;

/// <summary>Black cable span between poles — rope-backed when RopeSystem present.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Power Line Span")]
public sealed class PowerLineSpan : MonoBehaviour
{
    public Transform fromAnchor;
    public Transform toAnchor;
    public LineRenderer line;
    public Color cableColor = Color.black;
    public float cableRadiusM = 0.02f;
    public bool prebakeDestructible = true;
    [Range(0f, 1f)] public float tension01;
    public PowerLineTensionLemma tensionLemma;
    public MonoBehaviour ropeSystem;

    void Awake()
    {
        if (line == null)
            line = gameObject.GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
        line.startWidth = cableRadiusM * 2f;
        line.endWidth = cableRadiusM * 2f;
        line.positionCount = 2;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = cableColor;
        line.endColor = cableColor;
        if (tensionLemma == null)
            tensionLemma = GetComponent<PowerLineTensionLemma>();
    }

    void LateUpdate()
    {
        if (fromAnchor == null || toAnchor == null) return;
        line.SetPosition(0, fromAnchor.position);
        line.SetPosition(1, toAnchor.position);
        float dist = Vector3.Distance(fromAnchor.position, toAnchor.position);
        tension01 = Mathf.Clamp01((dist - 8f) / 20f);
        if (prebakeDestructible && tensionLemma != null && tension01 > 0.5f)
        {
            // Usually lean poles — break is rare
            var pole = fromAnchor.GetComponentInParent<UtilityPoleAssembly>();
            pole?.ApplyTension(tension01);
        }
    }

    public void Configure(Transform from, Transform to)
    {
        fromAnchor = from;
        toAnchor = to;
    }
}
