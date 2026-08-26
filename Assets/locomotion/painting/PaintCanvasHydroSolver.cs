using System;
using UnityEngine;
using SdfMax;

/// <summary>
/// Canvas-local paint particle for SPH-style surface tension on SDF Max expressions.
/// </summary>
[Serializable]
public struct PaintCanvasHydroParticle
{
    public Vector3 localPos;
    public Vector3 velocity;
    public float mass;
    public float density;
    public float tension;
    public float wet;
    public Color pigment;
    public bool active;
}

/// <summary>Sampled SPH ridge / pressure at a nib tip for optional force feedback.</summary>
public struct HydroNibRidgeSample
{
    public float density;
    public float tension;
    public float ridgeHeightM;
    public float contactForceN;
    public float requestedBendDeg;
    public Vector3 worldForce;
    public int neighborCount;
}

/// <summary>
/// Realtime canvas hydro: rolling-sphere + SPH-style tension on IntegralConvexTree leaves
/// of the top wet layer SDF. Writes viscosity (R wet / G dry / B mass / A caustic-spec)
/// and layer specular (matte beads vs semi-gloss film).
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Canvas Hydro Solver")]
public sealed class PaintCanvasHydroSolver : MonoBehaviour
{
    public PaintCanvas canvas;
    public PaintPileLiquidDriver pileSource;
    [Range(32, 256)] public int maxParticles = 160;
    [Range(0.005f, 0.08f)] public float kernelRadiusM = 0.025f;
    [Range(0f, 1f)] public float surfaceTension = 0.85f;
    [Range(0f, 2f)] public float tensionGain = 1.1f;
    [Range(0f, 2f)] public float pressureGain = 0.8f;
    [Range(0f, 1f)] public float damping = 0.12f;
    [Min(1)] public int rebuildIctEveryNFrames = 8;
    [Range(0f, 0.05f)] public float sdfBandM = 0.012f;
    public bool runSimulation = true;
    [Tooltip("When on, SPH film ridge (∇ρ) and pressure apply force back to the assigned nib.")]
    public bool feedRidgeForceToNib;
    public PenInkInstrument nibFeedbackTarget;
    [Min(0f)] public float ridgeForceGain = 8f;
    [Min(0f)] public float ridgeBendGainDeg = 12f;

    PaintCanvasHydroParticle[] _pool;
    int _active;
    IntegralConvexTreeSolver _ict;
    SdfMaxEvaluator _eval;
    SdfMaxSolverProfile _profile;
    int _frame;
    bool _dirtyIct = true;
    float _fluxTimer;
    Vector3 _fluxDirLocal;

    public int ActiveCount => _active;
    public PaintCanvasHydroParticle[] Particles => _pool;
    public float EffectiveSphDryRate =>
        canvas != null && canvas.inkProfile != null ? canvas.inkProfile.sphDryRate : 0.02f;

    void Awake()
    {
        if (canvas == null)
            canvas = GetComponent<PaintCanvas>();
        EnsurePool();
        _ict = new IntegralConvexTreeSolver();
        _profile = ScriptableObject.CreateInstance<SdfMaxSolverProfile>();
        _profile.maxDepth = 5;
        _profile.minLeafExtent = 0.04f;
        _profile.sampleEpsilon = 0.002f;
        _profile.enablePlanarContext = false;
    }

    void OnDestroy()
    {
        if (_profile != null)
        {
            if (Application.isPlaying) Destroy(_profile);
            else DestroyImmediate(_profile);
        }
    }

    void EnsurePool()
    {
        int n = Mathf.Clamp(maxParticles, 32, 256);
        if (_pool != null && _pool.Length == n) return;
        _pool = new PaintCanvasHydroParticle[n];
        _active = 0;
    }

    void FixedUpdate()
    {
        if (!runSimulation || canvas == null) return;
        EnsurePool();
        _frame++;
        if (_dirtyIct || (_frame % Mathf.Max(1, rebuildIctEveryNFrames) == 0))
            RebuildIct();

        float dt = Mathf.Max(1e-4f, Time.fixedDeltaTime);
        if (_fluxTimer > 0f)
            _fluxTimer -= dt;

        StepSph(dt);
        ProjectToSdfSurface();
        if (feedRidgeForceToNib)
            TryFeedRidgeForceToNib(nibFeedbackTarget);
        WriteViscosityAndSpecular();
        canvas.BindMaterials();
    }

    public void MarkIctDirty() => _dirtyIct = true;

    /// <summary>Seed particles at a canvas-local stamp (call after stroke).</summary>
    public void SeedFromStamp(Vector3 worldTip, Color pigment, float mass, float wet01, int count = 8)
    {
        if (canvas == null) return;
        EnsurePool();
        Vector3 local = canvas.transform.InverseTransformPoint(worldTip);
        local.z = 0f;
        count = Mathf.Clamp(count, 1, 24);
        float spread = kernelRadiusM * 0.65f;
        for (int i = 0; i < count; i++)
        {
            float ang = (i / (float)count) * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * spread * (0.35f + 0.65f * (i % 3) / 2f);
            Spawn(local + offset, Vector3.zero, pigment, mass / count, wet01);
        }
        MarkIctDirty();
    }

    public bool TryGetFilmCentroid(out Vector3 world)
    {
        world = canvas != null ? canvas.transform.position : Vector3.zero;
        if (_pool == null || canvas == null) return false;
        Vector3 acc = Vector3.zero;
        float w = 0f;
        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].active) continue;
            acc += _pool[i].localPos * _pool[i].mass;
            w += _pool[i].mass;
        }
        if (w < 1e-6f) return false;
        world = canvas.transform.TransformPoint(acc / w);
        return true;
    }

    /// <summary>Brush hairs pull away: outward flux thins film → semi-gloss.</summary>
    public void ApplyPullAwayFlux(Vector3 worldTip, Vector3 worldNormal, float strength)
    {
        if (canvas == null) return;
        EnsurePool();
        Vector3 localTip = canvas.transform.InverseTransformPoint(worldTip);
        localTip.z = 0f;
        Vector3 n = canvas.transform.InverseTransformDirection(worldNormal);
        n.z = 0f;
        if (n.sqrMagnitude < 1e-6f)
            n = Vector3.up;
        n.Normalize();
        _fluxDirLocal = n;
        _fluxTimer = 0.12f;

        float r2 = kernelRadiusM * kernelRadiusM * 4f;
        float s = Mathf.Clamp01(strength) * surfaceTension;
        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].active) continue;
            Vector3 d = _pool[i].localPos - localTip;
            d.z = 0f;
            if (d.sqrMagnitude > r2) continue;
            _pool[i].velocity += n * (s * 0.35f);
            _pool[i].wet = Mathf.Clamp01(_pool[i].wet * (1f - 0.15f * s));
            _pool[i].mass *= 1f - 0.08f * s;
        }
    }

    /// <summary>Pull mass/color/tension from an SDF Max pile into canvas hydro.</summary>
    public bool TryPullFromPile(Vector3 worldPoint, float dt, float maxTake = 0.08f)
    {
        if (pileSource == null || canvas == null) return false;
        if (!pileSource.TrySampleContact(worldPoint, out float depth, out Color color, out float mass))
            return false;
        if (depth <= 1e-5f || mass <= 1e-5f) return false;

        float take = Mathf.Min(maxTake * depth * Mathf.Max(dt, 1e-3f) * 8f, mass, 0.2f);
        if (take <= 1e-5f) return false;

        pileSource.ConsumeMass(take);
        SeedFromStamp(worldPoint, color, take, wet01: 1f, count: 6);

        if (canvas.WorldToCanvasUv(worldPoint, out Vector2 uv))
        {
            // Blend like manifold flood: density/tension into viscosity
            float t = Mathf.Clamp01(0.55f);
            Color sample = color;
            sample.r = Mathf.Lerp(0.4f, 1f, t); // wet
            sample.g = 0f;
            sample.b = take;
            sample.a = Mathf.Lerp(0.25f, 0.55f, surfaceTension); // caustic/spec film
            canvas.Viscosity.Stamp(uv, sample, kernelRadiusM * 2f);
            canvas.Viscosity.Apply();
        }
        return true;
    }

    public void RebuildIct()
    {
        _dirtyIct = false;
        _eval = null;
        if (canvas?.layerStack == null) return;
        var layer = canvas.layerStack.TopWetLayer();
        if (layer?.composition == null || layer.composition.nodes == null || layer.composition.nodes.Count == 0)
            return;

        var graph = new SdfMaxExpressionGraph(
            layer.composition,
            _profile,
            canvas.transform.localToWorldMatrix);
        _eval = new SdfMaxEvaluator(graph);
        // ICT samples evaluator in world space — build over canvas world AABB
        Vector3 e = canvas.transform.TransformVector(new Vector3(0.6f, 0.6f, 0.1f));
        e = new Vector3(Mathf.Abs(e.x), Mathf.Abs(e.y), Mathf.Max(0.05f, Mathf.Abs(e.z)));
        Bounds worldBounds = new Bounds(canvas.transform.position, e * 2f);
        _ict.Build(_eval, worldBounds, _profile);
    }

    void Spawn(Vector3 localPos, Vector3 velocity, Color pigment, float mass, float wet01)
    {
        EnsurePool();
        int slot = -1;
        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].active)
            {
                slot = i;
                break;
            }
        }
        if (slot < 0)
        {
            // Replace weakest mass
            float best = float.MaxValue;
            for (int i = 0; i < _pool.Length; i++)
            {
                if (_pool[i].mass < best)
                {
                    best = _pool[i].mass;
                    slot = i;
                }
            }
        }

        localPos.z = 0f;
        _pool[slot] = new PaintCanvasHydroParticle
        {
            localPos = localPos,
            velocity = velocity,
            mass = Mathf.Max(1e-4f, mass),
            density = 1f,
            tension = surfaceTension,
            wet = Mathf.Clamp01(wet01),
            pigment = pigment,
            active = true
        };
        RecountActive();
    }

    void RecountActive()
    {
        _active = 0;
        for (int i = 0; i < _pool.Length; i++)
            if (_pool[i].active) _active++;
    }

    /// <summary>Advance SPH without writing viscosity (tests / stamp feedback).</summary>
    public void Simulate(float dt)
    {
        EnsurePool();
        float step = Mathf.Max(1e-4f, dt);
        StepSph(step);
        ProjectToSdfSurface();
    }

    public HydroNibRidgeSample SampleRidgeAt(Vector3 worldTip)
    {
        var sample = new HydroNibRidgeSample();
        if (canvas == null)
            return sample;
        EnsurePool();
        Vector3 local = canvas.transform.InverseTransformPoint(worldTip);
        local.z = 0f;
        float h = Mathf.Max(1e-4f, kernelRadiusM);
        float h2 = h * h;
        float rho = 0f;
        Vector3 gradRho = Vector3.zero;
        float tensionAcc = 0f;
        int n = 0;
        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].active) continue;
            Vector3 r = local - _pool[i].localPos;
            r.z = 0f;
            float d2 = r.sqrMagnitude;
            if (d2 > h2 * 4f) continue;
            n++;
            rho += _pool[i].mass * Poly6(d2, h);
            float dist = Mathf.Sqrt(Mathf.Max(d2, 1e-12f));
            if (dist < h)
            {
                Vector3 dir = r / dist;
                gradRho += dir * (_pool[i].mass * SpikyGrad(dist, h));
                tensionAcc += _pool[i].tension;
            }
        }

        sample.neighborCount = n;
        sample.density = rho;
        sample.tension = n > 0 ? tensionAcc / n : 0f;
        float gradMag = gradRho.magnitude;
        sample.ridgeHeightM = gradMag * h;
        float gain = Mathf.Max(0f, ridgeForceGain);
        float pressureN = rho * pressureGain * gain;
        Vector3 forceLocal = -gradRho * (surfaceTension * tensionGain * gain);
        Vector3 worldRidge = canvas.transform.TransformDirection(forceLocal);
        Vector3 worldPressure = canvas.transform.forward * pressureN;
        sample.worldForce = worldRidge + worldPressure;
        sample.contactForceN = sample.worldForce.magnitude;
        sample.requestedBendDeg = sample.ridgeHeightM / h * Mathf.Max(0f, ridgeBendGainDeg);
        return sample;
    }

    /// <summary>
    /// Optional two-way couple: SPH ridge + pressure push the nib. Does not re-seed hydro (no splatter).
    /// </summary>
    public bool TryFeedRidgeForceToNib(PenInkInstrument instrument, Collider contactCollider = null)
    {
        if (!feedRidgeForceToNib || instrument == null || canvas == null)
            return false;
        var sample = SampleRidgeAt(instrument.TipWorld);
        instrument.lastHydroRidgeHeightM = sample.ridgeHeightM;
        instrument.lastHydroWorldForce = sample.worldForce;
        var rb = instrument.GetComponent<Rigidbody>();
        if (rb == null && instrument.tip != null)
            rb = instrument.tip.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic && sample.worldForce.sqrMagnitude > 1e-8f)
            rb.AddForce(sample.worldForce, ForceMode.Force);
        Vector3 n = canvas.transform.forward;
        instrument.ContactCanvas(canvas, sample.requestedBendDeg, sample.contactForceN, contactCollider, n, splatter: false);
        return sample.neighborCount > 0 || sample.contactForceN > 0f;
    }

    void StepSph(float dt)
    {
        float h = Mathf.Max(1e-4f, kernelRadiusM);
        float h2 = h * h;
        float restDensity = 1f;

        // Density
        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].active) continue;
            float rho = _pool[i].mass * Poly6(0f, h);
            for (int j = 0; j < _pool.Length; j++)
            {
                if (i == j || !_pool[j].active) continue;
                Vector3 r = _pool[i].localPos - _pool[j].localPos;
                r.z = 0f;
                float d2 = r.sqrMagnitude;
                if (d2 > h2) continue;
                rho += _pool[j].mass * Poly6(d2, h);
            }
            _pool[i].density = Mathf.Max(1e-3f, rho);
        }

        // Forces: pressure + surface tension (~∇ρ) + flux
        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].active) continue;
            var layer = canvas.layerStack != null ? canvas.layerStack.TopWetLayer() : null;
            float dryLock = canvas.layerStack != null ? canvas.layerStack.smudgeDryLock : 0.85f;
            bool locked = layer != null && layer.dry01 >= dryLock;
            if (locked)
            {
                _pool[i].velocity *= 0.5f;
                _pool[i].tension = 0f;
                continue;
            }

            Vector3 force = Vector3.zero;
            Vector3 gradRho = Vector3.zero;
            float pi = pressureGain * (_pool[i].density - restDensity);

            for (int j = 0; j < _pool.Length; j++)
            {
                if (i == j || !_pool[j].active) continue;
                Vector3 r = _pool[i].localPos - _pool[j].localPos;
                r.z = 0f;
                float dist = r.magnitude;
                if (dist < 1e-5f || dist > h) continue;
                Vector3 dir = r / dist;
                float pj = pressureGain * (_pool[j].density - restDensity);
                float wSpiky = SpikyGrad(dist, h);
                force += -dir * ((pi + pj) * 0.5f * _pool[j].mass / _pool[j].density) * wSpiky;
                gradRho += dir * (_pool[j].mass * wSpiky);
            }

            float tensionScale = surfaceTension * tensionGain * _pool[i].wet;
            force += -gradRho * tensionScale;
            _pool[i].tension = Mathf.Clamp01(gradRho.magnitude * tensionScale);

            if (_fluxTimer > 0f)
                force += _fluxDirLocal * (surfaceTension * 0.5f);

            _pool[i].velocity += force * dt;
            _pool[i].velocity *= 1f - Mathf.Clamp01(damping);
            _pool[i].localPos += _pool[i].velocity * dt;
            _pool[i].localPos.z = 0f;
            _pool[i].localPos.x = Mathf.Clamp(_pool[i].localPos.x, -0.55f, 0.55f);
            _pool[i].localPos.y = Mathf.Clamp(_pool[i].localPos.y, -0.55f, 0.55f);

            float baseDry = canvas != null && canvas.inkProfile != null
                ? canvas.inkProfile.sphDryRate
                : 0.02f;
            float dryRate = baseDry * dt * (1f - _pool[i].wet);
            _pool[i].wet = Mathf.Clamp01(_pool[i].wet - dryRate);
        }
    }

    void ProjectToSdfSurface()
    {
        if (_eval == null || _ict == null || _ict.Leaves.Count == 0) return;
        float band = Mathf.Max(1e-4f, sdfBandM);
        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].active) continue;
            Vector3 world = canvas.transform.TransformPoint(_pool[i].localPos);
            float sdf = _eval.Sample(world, 0f);
            // Keep in a thin band around the zero isosurface (paint body)
            if (Mathf.Abs(sdf) > band * 4f)
            {
                // Softly kill particles far outside expression
                if (sdf > band * 8f)
                {
                    _pool[i].mass *= 0.92f;
                    if (_pool[i].mass < 1e-4f)
                        _pool[i].active = false;
                }
                continue;
            }

            Vector3 grad = EstimateWorldGradient(world);
            if (grad.sqrMagnitude < 1e-8f) continue;
            grad.Normalize();
            // Pull toward surface: sdf > 0 outside
            world -= grad * Mathf.Clamp(sdf, -band, band);
            Vector3 local = canvas.transform.InverseTransformPoint(world);
            local.z = 0f;
            _pool[i].localPos = local;
        }
        RecountActive();
    }

    Vector3 EstimateWorldGradient(Vector3 world)
    {
        float e = 0.004f;
        float dx = _eval.Sample(world + Vector3.right * e, 0f) - _eval.Sample(world - Vector3.right * e, 0f);
        float dy = _eval.Sample(world + Vector3.up * e, 0f) - _eval.Sample(world - Vector3.up * e, 0f);
        float dz = _eval.Sample(world + Vector3.forward * e, 0f) - _eval.Sample(world - Vector3.forward * e, 0f);
        return new Vector3(dx, dy, dz) / (2f * e);
    }

    void WriteViscosityAndSpecular()
    {
        if (canvas?.Viscosity == null) return;
        var visc = canvas.Viscosity;
        var layer = canvas.layerStack != null ? canvas.layerStack.TopWetLayer() : null;

        float accMass = 0f;
        float accWet = 0f;
        float accTension = 0f;
        float accCaustic = 0f;
        int n = 0;

        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].active) continue;
            n++;
            Vector3 world = canvas.transform.TransformPoint(_pool[i].localPos);
            if (!canvas.WorldToCanvasUv(world, out Vector2 uv))
                continue;

            float pileFactor = Mathf.Clamp01(_pool[i].mass * _pool[i].tension * 4f);
            float filmFactor = Mathf.Clamp01(_pool[i].wet * (1f - pileFactor));
            // Caustic: high |∇tension| / wet film highlight
            float caustic = Mathf.Clamp01(_pool[i].tension * _pool[i].wet * (1f - pileFactor) * 1.5f);
            float specA = ComputeSpecular(filmFactor, pileFactor, layer != null ? layer.dry01 : 0f);

            Color sample = _pool[i].pigment;
            sample.r = _pool[i].wet;
            sample.g = (1f - _pool[i].wet) * 0.35f; // dry
            sample.b = Mathf.Clamp01(_pool[i].mass * 4f);
            sample.a = Mathf.Max(specA, caustic);

            visc.Stamp(uv, sample, kernelRadiusM);
            accMass += _pool[i].mass;
            accWet += _pool[i].wet;
            accTension += _pool[i].tension;
            accCaustic += caustic;
        }

        if (n > 0 && layer != null)
        {
            float inv = 1f / n;
            float pile = Mathf.Clamp01(accMass * inv * accTension * inv * 4f);
            float film = Mathf.Clamp01(accWet * inv * (1f - pile));
            layer.specular = ComputeSpecular(film, pile, layer.dry01);
            layer.roughness = Mathf.Clamp01(Mathf.Lerp(0.35f, 0.85f, pile) * (0.5f + 0.5f * layer.dry01));
        }

        visc.Apply();
    }

    /// <summary>pileFactor → matte; filmFactor → semi-gloss.</summary>
    public static float ComputeSpecular(float filmFactor, float pileFactor, float dry01)
    {
        float film = Mathf.Clamp01(filmFactor);
        float pile = Mathf.Clamp01(pileFactor);
        float specular = Mathf.Lerp(0.12f, 0.55f, film) * (1f - Mathf.Clamp01(dry01));
        specular *= 1f - 0.65f * pile;
        return Mathf.Clamp01(specular);
    }

    static float Poly6(float r2, float h)
    {
        float h2 = h * h;
        if (r2 > h2) return 0f;
        float diff = h2 - r2;
        float h9 = h2 * h2 * h2 * h2 * h; // h^8 * h = h^9 approx via h2^4 * h
        // Standard poly6 ~ (h2-r2)^3 ; normalize loosely
        return diff * diff * diff / Mathf.Max(1e-6f, h2 * h2 * h2);
    }

    static float SpikyGrad(float r, float h)
    {
        if (r <= 0f || r >= h) return 0f;
        float q = h - r;
        return q * q / Mathf.Max(1e-6f, h * h * h * h);
    }
}
