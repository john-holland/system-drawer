using UnityEngine;

namespace SdfMax
{
    public sealed class SdfMaxExpressionGraph
    {
        readonly SdfMaxCompositionAsset _asset;
        readonly SdfMaxSolverProfile _profile;
        readonly Matrix4x4 _localToWorld;
        readonly Matrix4x4 _worldToLocal;
        readonly IPlanarEvaluationContext _planar;

        public SdfMaxExpressionGraph(
            SdfMaxCompositionAsset asset,
            SdfMaxSolverProfile profile,
            Matrix4x4 localToWorld,
            IPlanarEvaluationContext planarContext = null)
        {
            _asset = asset;
            _profile = profile;
            _localToWorld = localToWorld;
            _worldToLocal = localToWorld.inverse;
            _planar = profile != null && profile.enablePlanarContext ? planarContext : null;
        }

        public Bounds ComputeWorldBounds()
        {
            if (_asset == null || _asset.nodes == null || _asset.nodes.Count == 0)
                return new Bounds(_localToWorld.MultiplyPoint3x4(Vector3.zero), Vector3.one);

            int root = _asset.ResolveRootIndex();
            if (root < 0)
                return new Bounds(_localToWorld.MultiplyPoint3x4(Vector3.zero), Vector3.one);

            Bounds local = EstimateNodeBounds(root);
            Vector3 worldCenter = _localToWorld.MultiplyPoint3x4(local.center);
            Vector3 ext = Vector3.Scale(local.extents, _localToWorld.lossyScale);
            return new Bounds(worldCenter, ext * 2f);
        }

        public float SampleWorld(Vector3 worldPos, float narrativeTime)
        {
            Vector3 local = _worldToLocal.MultiplyPoint3x4(worldPos);
            int root = _asset != null ? _asset.ResolveRootIndex() : -1;
            if (root < 0)
                return 1000f;
            return EvalNode(root, local, worldPos, narrativeTime);
        }

        Bounds EstimateNodeBounds(int index)
        {
            var node = _asset.nodes[index];
            if (node.op == SdfMaxOp.PrimitiveLeaf)
            {
                Vector3 he = node.halfExtents;
                switch (node.primitiveType)
                {
                    case SdfPrimitiveType.Sphere:
                    case SdfPrimitiveType.Capsule:
                    case SdfPrimitiveType.DisplacedSphere:
                        he = Vector3.one * node.sphereRadius;
                        break;
                    case SdfPrimitiveType.PlanarStamp:
                        float r = Mathf.Max(0.1f, node.stampFootprintMeters);
                        he = new Vector3(r, r, r);
                        break;
                    case SdfPrimitiveType.FractalNoise:
                    case SdfPrimitiveType.MandelbrotDisplacement:
                    case SdfPrimitiveType.LatLonShell:
                        he = Vector3.one * Mathf.Max(node.radius, node.sphereRadius, 1f);
                        break;
                }
                return new Bounds(node.localPosition, he * 2f);
            }

            Bounds b = new Bounds(node.localPosition, Vector3.one * 0.01f);
            bool first = true;
            if (node.childIndexA >= 0 && node.childIndexA < _asset.nodes.Count)
            {
                b = EstimateNodeBounds(node.childIndexA);
                first = false;
            }
            if (node.childIndexB >= 0 && node.childIndexB < _asset.nodes.Count)
            {
                var b2 = EstimateNodeBounds(node.childIndexB);
                if (first) { b = b2; first = false; }
                else b.Encapsulate(b2);
            }
            return b;
        }

        float EvalNode(int index, Vector3 localPoint, Vector3 worldPoint, float t)
        {
            var node = _asset.nodes[index];
            switch (node.op)
            {
                case SdfMaxOp.Constant:
                    return node.constantValue;
                case SdfMaxOp.PrimitiveLeaf:
                    return EvalPrimitive(node, localPoint, worldPoint, t);
                case SdfMaxOp.Max:
                    return Mathf.Max(
                        EvalChild(node.childIndexA, localPoint, worldPoint, t),
                        EvalChild(node.childIndexB, localPoint, worldPoint, t));
                case SdfMaxOp.Min:
                    return Mathf.Min(
                        EvalChild(node.childIndexA, localPoint, worldPoint, t),
                        EvalChild(node.childIndexB, localPoint, worldPoint, t));
                case SdfMaxOp.Subtract:
                    return Mathf.Max(
                        EvalChild(node.childIndexA, localPoint, worldPoint, t),
                        -EvalChild(node.childIndexB, localPoint, worldPoint, t));
                case SdfMaxOp.Add:
                {
                    float a = EvalChild(node.childIndexA, localPoint, worldPoint, t);
                    float b = EvalChild(node.childIndexB, localPoint, worldPoint, t);
                    float k = _profile != null ? _profile.blendK : node.blendK;
                    return SmoothMin(a, b, k);
                }
                case SdfMaxOp.SmoothMax:
                {
                    float a = EvalChild(node.childIndexA, localPoint, worldPoint, t);
                    float b = EvalChild(node.childIndexB, localPoint, worldPoint, t);
                    return SmoothMax(a, b, node.smoothRadius);
                }
                case SdfMaxOp.Multiply:
                {
                    float s = Mathf.Max(0.0001f, node.localScale.x);
                    Vector3 p = new Vector3(localPoint.x / s, localPoint.y / s, localPoint.z / s);
                    return EvalChild(node.childIndexA, p, worldPoint, t) * s;
                }
                case SdfMaxOp.Divide:
                {
                    float s = Mathf.Max(0.0001f, node.localScale.x);
                    Vector3 p = localPoint * s;
                    return EvalChild(node.childIndexA, p, worldPoint, t) / s;
                }
                default:
                    return 1000f;
            }
        }

        float EvalChild(int childIndex, Vector3 localPoint, Vector3 worldPoint, float t)
        {
            if (childIndex < 0 || childIndex >= _asset.nodes.Count)
                return 1000f;
            return EvalNode(childIndex, localPoint, worldPoint, t);
        }

        float EvalPrimitive(SdfMaxNode node, Vector3 p, Vector3 worldP, float t)
        {
            if (t < node.tMin || t > node.tMax)
                return 1000f;

            Matrix4x4 tr = Matrix4x4.TRS(node.localPosition, Quaternion.Euler(node.localRotationEuler), node.localScale);
            Vector3 lp = tr.inverse.MultiplyPoint3x4(p);
            Vector3 world = _localToWorld.MultiplyPoint3x4(p);

            switch (node.primitiveType)
            {
                case SdfPrimitiveType.Sphere:
                    return lp.magnitude - node.radius;
                case SdfPrimitiveType.Box:
                {
                    Vector3 d = new Vector3(Mathf.Abs(lp.x), Mathf.Abs(lp.y), Mathf.Abs(lp.z)) - node.halfExtents;
                    return Mathf.Max(d.x, Mathf.Max(d.y, d.z));
                }
                case SdfPrimitiveType.Capsule:
                    lp.y -= Mathf.Clamp(lp.y, -node.radius, node.radius);
                    return lp.magnitude - node.radius;
                case SdfPrimitiveType.Plane:
                    return lp.y;
                case SdfPrimitiveType.MeshBounds:
                {
                    Vector3 q = new Vector3(
                        Mathf.Abs(lp.x) - node.halfExtents.x,
                        Mathf.Abs(lp.y) - node.halfExtents.y,
                        Mathf.Abs(lp.z) - node.halfExtents.z);
                    return Mathf.Max(q.x, Mathf.Max(q.y, q.z));
                }
                case SdfPrimitiveType.FractalNoise:
                {
                    var settings = _profile != null ? _profile.noiseDefaults : new NoiseLibrarySettings();
                    settings.seed = node.noiseSeed != 0 ? node.noiseSeed : settings.seed;
                    settings.frequency = node.noiseFrequency > 0f ? node.noiseFrequency : settings.frequency;
                    settings.octaves = node.noiseOctaves > 0 ? node.noiseOctaves : settings.octaves;
                    return -SdfMaxNoiseUtility.SampleFractal(new Vector2(lp.x, lp.z), settings, node.noiseSeed);
                }
                case SdfPrimitiveType.MandelbrotDisplacement:
                    return -SdfMaxNoiseUtility.SampleMandelbrot(new Vector2(lp.x, lp.z), node);
                case SdfPrimitiveType.DisplacedSphere:
                {
                    float baseSdf = lp.magnitude - node.sphereRadius;
                    if (node.childIndexA >= 0)
                        baseSdf = SmoothMax(baseSdf, EvalChild(node.childIndexA, p, worldP, t), node.smoothRadius);
                    return baseSdf;
                }
                case SdfPrimitiveType.PlanarStamp:
                    return EvalPlanarStamp(node, world);
                case SdfPrimitiveType.LatLonShell:
                    return EvalLatLonShell(node, world, t);
                default:
                    return 1000f;
            }
        }

        float EvalPlanarStamp(SdfMaxNode node, Vector3 world)
        {
            if (_planar == null)
                return 1000f;
            int idx = node.planarFeatureIndex >= 0 ? node.planarFeatureIndex : 0;
            if (!_planar.TryWorldToPlanarUV(world, out int fi, out Vector2 uv))
                return 1000f;
            if (node.planarFeatureIndex >= 0 && fi != node.planarFeatureIndex)
                return 1000f;
            float h = _planar.SampleStampHeight(fi, uv);
            float r = Mathf.Max(0.1f, node.stampFootprintMeters);
            Vector3 origin = _localToWorld.MultiplyPoint3x4(Vector3.zero);
            float dist = Vector3.Distance(world, origin);
            return dist - (r + h * node.weight);
        }

        float EvalLatLonShell(SdfMaxNode node, Vector3 world, float t)
        {
            float radius = node.sphereRadius > 0f ? node.sphereRadius : node.radius;
            Vector3 origin = _localToWorld.MultiplyPoint3x4(Vector3.zero);
            float dist = Vector3.Distance(world, origin);
            float shell = dist - radius;
            if (_planar != null && _planar.TryWorldToLatLon(world, out float lat, out float lon))
            {
                float h = _planar.SampleHeightAtLatLon(lat, lon);
                shell -= h * node.weight;
            }
            if (node.childIndexA >= 0)
            {
                Vector3 local = _worldToLocal.MultiplyPoint3x4(world);
                shell = SmoothMax(shell, EvalChild(node.childIndexA, local, world, t), node.smoothRadius);
            }
            return shell;
        }

        static float SmoothMin(float a, float b, float k)
        {
            if (k <= 1e-5f)
                return Mathf.Min(a, b);
            float h = Mathf.Clamp(0.5f + 0.5f * (b - a) / k, 0f, 1f);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        static float SmoothMax(float a, float b, float r)
        {
            if (r <= 1e-5f)
                return Mathf.Max(a, b);
            float h = Mathf.Clamp(0.5f + 0.5f * (a - b) / r, 0f, 1f);
            return Mathf.Lerp(a, b, h) + r * h * (1f - h);
        }
    }
}
