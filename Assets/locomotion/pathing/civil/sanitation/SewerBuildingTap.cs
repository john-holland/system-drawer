using UnityEngine;

/// <summary>Links building fixtures (toilet/shower/sink) + street drains into SewerGraph.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Sewer Building Tap")]
public sealed class SewerBuildingTap : MonoBehaviour
{
    public SewerGraph graph;
    public FixturePlumbingNode[] fixtures;
    public float pooPerFlush01 = 0.08f;
    public float soapyPerUse01 = 0.04f;
    string _nodeId;

    void Awake()
    {
        if (graph == null)
            graph = FindFirstObjectByType<SewerGraph>() ?? gameObject.AddComponent<SewerGraph>();
        if (fixtures == null || fixtures.Length == 0)
            fixtures = GetComponentsInChildren<FixturePlumbingNode>(true);
        var node = graph.AddOrGetBuildingNode(gameObject);
        _nodeId = node != null ? node.nodeId : null;
        graph.EnsureFullyConnectedToPlant();
    }

    public void OnToiletFlush() => graph?.TransmitFromFixture(_nodeId, pooPerFlush01, 0f);

    public void OnShowerOrSinkUse() => graph?.TransmitFromFixture(_nodeId, 0f, soapyPerUse01);

    void Update()
    {
        // Soft continuous soapy contribution when fixtures present.
        if (fixtures != null && fixtures.Length > 0 && Time.frameCount % 60 == 0)
            graph?.TransmitFromFixture(_nodeId, 0f, soapyPerUse01 * 0.02f);
    }
}
