using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates rope arc-length state, ring-buffer rigid bodies, tensile/overlap/cord, caches, and pathing footprint.
/// </summary>
[DisallowMultipleComponent]
public class RopeSystem : MonoBehaviour
{
    [SerializeField] RopeConfig config = new RopeConfig();
    [SerializeField] Transform headAnchor;
    [SerializeField] Transform spoolAnchor;
    [SerializeField] Transform tailAnchor;
    [SerializeField] Vector3 unwindDirection = Vector3.down;
    [SerializeField] RopeSegmentBody segmentPrefab;
    [SerializeField] Material ropeMaterial;
    [SerializeField] MeshRenderer ropeMeshRenderer;
    [SerializeField] AudioClip scrapeClip;
    [SerializeField] AudioClip impactClip;
    [SerializeField] AudioClip snapClip;
    [SerializeField] Transform audioListener;

    RopeArcLengthState _arc;
    RopeSegmentRingBuffer _ring;
    RopeWindingController _winding;
    RopeTensileModel _tensile;
    RopeOverlapIndex _overlap;
    RopeCordSolver _cord;
    RopeRadialStrainCache _radial;
    RopeAudioMap _audio;
    RopePathingFootprint _footprint;

    bool _initialized;
    bool _snapped;

    public RopeConfig Config => config;
    public RopeArcLengthState Arc => _arc;
    public RopeOverlapIndex OverlapIndex => _overlap;
    public float NormalizedLoad => _tensile != null ? _tensile.NormalizedLoad : 0f;
    public float MaxTensionN => _tensile != null ? _tensile.MaxTensionN : 0f;
    public float TotalBreakTensionN => _tensile != null ? _tensile.TotalBreakTensionN : 0f;
    public bool IsSnapped => _snapped;

    void Awake()
    {
        Initialize();
        _footprint = GetComponent<RopePathingFootprint>();
        if (_footprint == null)
            _footprint = gameObject.AddComponent<RopePathingFootprint>();
    }

    void OnEnable()
    {
        if (_tensile != null)
            _tensile.Snapped += OnSnapped;
    }

    void OnDisable()
    {
        if (_tensile != null)
            _tensile.Snapped -= OnSnapped;
    }

    public void Initialize()
    {
        if (_initialized)
            return;

        _arc = new RopeArcLengthState(config);
        _ring = new RopeSegmentRingBuffer(config, _arc, transform, segmentPrefab);
        _ring.ApplyJointParameters(config.jointSpring, config.jointDamper);
        _winding = new RopeWindingController(config, _arc, _ring);
        _tensile = new RopeTensileModel(config, _arc, _ring);
        _overlap = new RopeOverlapIndex();
        _cord = new RopeCordSolver(config, _overlap, _ring);
        _radial = new RopeRadialStrainCache(config, _arc, _tensile);
        _audio = new RopeAudioMap(config, _arc, _overlap);

        foreach (RopeSegmentBody slot in _ring.Slots)
        {
            if (slot.GetComponent<RopeSegmentCollisionRelay>() == null)
            {
                var relay = slot.gameObject.AddComponent<RopeSegmentCollisionRelay>();
                relay.Initialize(this, slot);
            }
        }

        _ring.RebuildActiveMapping(headAnchor, tailAnchor, unwindDirection);
        if (ropeMaterial != null)
            _radial.BindToMaterial(ropeMaterial);
        if (ropeMeshRenderer != null && ropeMaterial != null)
            ropeMeshRenderer.sharedMaterial = ropeMaterial;

        _initialized = true;
    }

    void FixedUpdate()
    {
        if (!_initialized || _snapped)
            return;

        _overlap.Clear();
        _audio.ClearFrame();

        _winding.Tick(Time.fixedDeltaTime, spoolAnchor, headAnchor, unwindDirection);

        _tensile.SampleAfterPhysics();
        _cord.Solve();
        _audio.AccumulateFromOverlaps();

        _radial.WriteFromSimulation();
        if (ropeMaterial != null)
            _radial.BindToMaterial(ropeMaterial);

        if (_footprint != null)
            _footprint.RebuildSamples();

        Transform listener = audioListener != null ? audioListener : Camera.main != null ? Camera.main.transform : transform;
        _audio.EmitEvents(listener, scrapeClip, impactClip, snapClip);
    }

    public void SetWindRate(float signedRateMps)
    {
        if (_winding != null)
            _winding.SetRate(signedRateMps);
    }

    public void BindAnchors(Transform head, Transform tail, float? totalLengthM = null)
    {
        headAnchor = head;
        tailAnchor = tail;
        if (totalLengthM.HasValue)
            config.totalLengthM = Mathf.Max(0.1f, totalLengthM.Value);
        if (_initialized)
            _ring?.RebuildActiveMapping(headAnchor, tailAnchor, unwindDirection);
    }

    public void RegisterSegmentPairContact(RopeSegmentBody a, RopeSegmentBody b, ContactPoint contact)
    {
        _overlap?.RegisterCollision(a, b, contact, _arc);
    }

    public void RegisterExternalContact(RopeSegmentBody seg, ContactPoint contact)
    {
        _overlap?.RegisterExternal(seg, contact, _arc);
    }

    void OnSnapped(RopeSnapEvent evt)
    {
        _snapped = true;
        _audio?.QueueSnap(evt.arcM);
        int from = evt.segmentIndex;
        _ring.InvalidateLogicalRange(from, _arc.ActiveTailSegment);
        _overlap.InvalidateLogicalRange(from, _arc.ActiveTailSegment);
        for (int l = from; l <= _arc.ActiveTailSegment; l++)
        {
            RopeSegmentBody body = _ring.GetBody(l);
            body?.SetSimulated(false);
        }
    }

    public void CollectPathSamples(List<Vector3> output, int maxSamples)
    {
        output.Clear();
        if (_ring == null || _arc == null)
            return;

        int count = Mathf.Min(maxSamples, _arc.ActiveSegmentCount);
        if (count <= 0)
        {
            if (headAnchor != null)
                output.Add(headAnchor.position);
            return;
        }

        int step = Mathf.Max(1, _arc.ActiveSegmentCount / count);
        for (int logical = _arc.ActiveHeadSegment; logical <= _arc.ActiveTailSegment; logical += step)
        {
            RopeSegmentBody body = _ring.GetBody(logical);
            if (body != null && body.gameObject.activeSelf)
                output.Add(body.transform.position);
        }

        if (headAnchor != null && output.Count > 0)
            output[0] = headAnchor.position;
    }

    public void ResetSnapped()
    {
        _snapped = false;
        _tensile?.ClearPendingSnap();
        _arc?.ResetWound(0f);
        _ring?.RebuildActiveMapping(headAnchor, tailAnchor, unwindDirection);
    }
}
