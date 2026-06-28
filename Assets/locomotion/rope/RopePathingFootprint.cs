using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Publishes head capsule + active tail samples for hierarchical pathing and multibody clearance.
/// </summary>
[DisallowMultipleComponent]
public class RopePathingFootprint : MonoBehaviour
{
    [SerializeField] RopeSystem ropeSystem;
    [SerializeField] int maxSamples = 12;
    [SerializeField] float sampleRadiusM = 0.04f;

    readonly List<Vector3> _samples = new List<Vector3>(16);

    public float SampleRadiusM => sampleRadiusM;
    public IReadOnlyList<Vector3> BodySamples => _samples;

    void Awake()
    {
        if (ropeSystem == null)
            ropeSystem = GetComponent<RopeSystem>();
    }

    void OnEnable() => RopePathingFootprintRegistry.Register(this);
    void OnDisable() => RopePathingFootprintRegistry.Unregister(this);

    public void RebuildSamples()
    {
        _samples.Clear();
        if (ropeSystem == null)
            return;

        ropeSystem.CollectPathSamples(_samples, maxSamples);
    }

    public Bounds ComputeBounds()
    {
        RebuildSamples();
        if (_samples.Count == 0)
            return new Bounds(transform.position, Vector3.one * sampleRadiusM * 2f);

        Bounds b = new Bounds(_samples[0], Vector3.zero);
        for (int i = 1; i < _samples.Count; i++)
            b.Encapsulate(_samples[i]);
        b.Expand(sampleRadiusM * 2f);
        return b;
    }
}
