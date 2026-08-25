using UnityEngine;

/// <summary>Mount at a pole end or t along a street-wire span. Requires both poleId and wireId.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Powerlines/Street Wire End")]
public sealed class StreetWireEnd : MonoBehaviour
{
    public string poleId;
    public string wireId;
    public StreetWireEndAnchor end = StreetWireEndAnchor.From;
    [Range(0f, 1f)] public float t01 = 0.5f;
    public StreetWireEndKind kind = StreetWireEndKind.TrafficSignal;
    public TrafficLightController signal;
    public TrafficLightPoleDecorator poleDecorator;
    public PixelLightRig luminaire;
    public GameObject signPrefab;
    public HangingShoesComponent hangingShoes;
    public float extraMassKg = 2f;
    public string lastWarning;

    public UtilityPoleAssembly ResolvedPole { get; private set; }
    public PowerLineSpan ResolvedWire { get; private set; }

    bool _resolving;

    void OnEnable()
    {
        if (!string.IsNullOrEmpty(poleId) && !string.IsNullOrEmpty(wireId))
            Resolve();
    }

    public bool Resolve()
    {
        if (_resolving)
            return ResolvedPole != null && ResolvedWire != null;
        _resolving = true;
        try
        {
            lastWarning = null;
            ResolvedPole = PhonePoleIndex.FindById(poleId);
            ResolvedWire = StreetWireIndex.FindById(wireId);
            if (ResolvedPole == null)
                lastWarning = "StreetWireEnd missing poleId '" + poleId + "'";
            if (ResolvedWire == null)
                lastWarning = (lastWarning != null ? lastWarning + "; " : "") + "StreetWireEnd missing wireId '" + wireId + "'";
            if (!string.IsNullOrEmpty(lastWarning) &&
                (!string.IsNullOrEmpty(poleId) || !string.IsNullOrEmpty(wireId)))
                Debug.LogWarning(lastWarning, this);
            ApplyKind();
            return ResolvedPole != null && ResolvedWire != null;
        }
        finally
        {
            _resolving = false;
        }
    }

    public Vector3 AttachWorld()
    {
        if (ResolvedWire == null && !_resolving)
            Resolve();
        if (ResolvedWire == null)
            return transform.position;
        if (end == StreetWireEndAnchor.Along)
            return ResolvedWire.SampleWorld(t01);
        if (end == StreetWireEndAnchor.To)
            return ResolvedWire.SampleWorld(1f);
        return ResolvedWire.SampleWorld(0f);
    }

    void ApplyKind()
    {
        transform.position = AttachWorld();
        switch (kind)
        {
            case StreetWireEndKind.TrafficSignal:
                if (signal == null)
                    signal = GetComponent<TrafficLightController>() ?? gameObject.AddComponent<TrafficLightController>();
                if (poleDecorator == null)
                    poleDecorator = GetComponent<TrafficLightPoleDecorator>() ?? gameObject.AddComponent<TrafficLightPoleDecorator>();
                poleDecorator.controller = signal;
                if (poleDecorator.headRoot == null)
                    poleDecorator.headRoot = transform;
                poleDecorator.EnsureHeads();
                break;
            case StreetWireEndKind.StreetLight:
                if (luminaire == null)
                {
                    var go = PixelLightPrefabFactory.CreateDefaultRuntime(transform);
                    luminaire = go.GetComponent<PixelLightRig>();
                    luminaire.colorPackage = PixelLightColorPackage.CreateSignal(new Color(1f, 0.92f, 0.7f));
                }
                break;
            case StreetWireEndKind.HangingShoes:
                if (hangingShoes == null)
                    hangingShoes = GetComponent<HangingShoesComponent>() ?? gameObject.AddComponent<HangingShoesComponent>();
                hangingShoes.BindWire(this);
                break;
            case StreetWireEndKind.StuckBranch:
                if (ResolvedWire?.ropeSystem != null)
                    extraMassKg = Mathf.Max(extraMassKg, 4f);
                if (ResolvedWire != null && ResolvedWire.ropeSystem != null && ResolvedWire.ropeSystem.IsSnapped)
                    ResolvedWire.StampPowerLinesDown();
                break;
        }
    }
}
