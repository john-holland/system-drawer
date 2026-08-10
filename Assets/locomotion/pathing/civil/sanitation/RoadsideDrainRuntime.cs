using UnityEngine;

/// <summary>Street drain → sewer graph (poo/soapy shared outflow).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Roadside Drain")]
public sealed class RoadsideDrainRuntime : MonoBehaviour
{
    public SewerGraph graph;
    public float streetInflowPerSec = 0.01f;
    string _nodeId;

    void Awake()
    {
        if (graph == null)
            graph = FindFirstObjectByType<SewerGraph>();
        if (graph == null) return;
        var n = new SewerNode
        {
            nodeId = "drain_" + GetInstanceID(),
            worldPosition = transform.position,
            isStreetDrain = true,
            building = gameObject
        };
        graph.nodes.Add(n);
        _nodeId = n.nodeId;
        graph.EnsureFullyConnectedToPlant();
    }

    void Update()
    {
        if (graph == null || string.IsNullOrEmpty(_nodeId)) return;
        graph.TransmitFromFixture(_nodeId, 0f, streetInflowPerSec * Time.deltaTime);
    }
}
