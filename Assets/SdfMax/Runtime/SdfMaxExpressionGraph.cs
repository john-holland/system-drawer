using UnityEngine;

namespace SdfMax
{
    public sealed class SdfMaxExpressionGraph
    {
        readonly SdfMaxCompositionAsset _asset;
        readonly SdfMaxSolverProfile _profile;
        readonly Matrix4x4 _localToWorld;
        readonly Matrix4x4 _worldToLocal;

        public SdfMaxExpressionGraph(
            SdfMaxCompositionAsset asset,
            SdfMaxSolverProfile profile,
            Matrix4x4 localToWorld)
        {
            _asset = asset;
            _profile = profile;
            _localToWorld = localToWorld;
            _worldToLocal = localToWorld.inverse;
        }

        public Bounds ComputeWorldBounds()
        {
            if (_asset == null || _asset.nodes == null || _asset.nodes.Count == 0)
                return new Bounds(_localToWorld.GetColumn(3), Vector3.one);

            int root = _asset.ResolveRootIndex();
            if (root < 0)
                return new Bounds(_localToWorld.GetColumn(3), Vector3.one);

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
            return EvalNode(root, local, narrativeTime);
        }

        Bounds EstimateNodeBounds(int index)
        {
            var node = _asset.nodes[index];
            if (node.op == SdfMaxOp.PrimitiveLeaf)
            {
                Vector3 he = node.halfExtents;
                if (node.primitiveType == SdfPrimitiveType.Sphere || node.primitiveType == SdfPrimitiveType.Capsule)
                    he = Vector3.one * node.radius;
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

        float EvalNode(int index, Vector3 localPoint, float t)
        {
            var node = _asset.nodes[index];
            switch (node.op)
            {
                case SdfMaxOp.Constant:
                    return node.constantValue;
                case SdfMaxOp.PrimitiveLeaf:
                    return EvalPrimitive(node, localPoint, t);
                case SdfMaxOp.Max:
                    return Mathf.Max(EvalChild(node.childIndexA, localPoint, t), EvalChild(node.childIndexB, localPoint, t));
                case SdfMaxOp.Min:
                    return Mathf.Min(EvalChild(node.childIndexA, localPoint, t), EvalChild(node.childIndexB, localPoint, t));
                case SdfMaxOp.Subtract:
                    return Mathf.Max(EvalChild(node.childIndexA, localPoint, t), -EvalChild(node.childIndexB, localPoint, t));
                case SdfMaxOp.Add:
                {
                    float a = EvalChild(node.childIndexA, localPoint, t);
                    float b = EvalChild(node.childIndexB, localPoint, t);
                    float k = _profile != null ? _profile.blendK : node.blendK;
                    return SmoothMin(a, b, k);
                }
                case SdfMaxOp.SmoothMax:
                {
                    float a = EvalChild(node.childIndexA, localPoint, t);
                    float b = EvalChild(node.childIndexB, localPoint, t);
                    return SmoothMax(a, b, node.smoothRadius);
                }
                case SdfMaxOp.Multiply:
                {
                    float s = Mathf.Max(0.0001f, node.localScale.x);
                    Vector3 p = new Vector3(localPoint.x / s, localPoint.y / s, localPoint.z / s);
                    return EvalChild(node.childIndexA, p, t) * s;
                }
                case SdfMaxOp.Divide:
                {
                    float s = Mathf.Max(0.0001f, node.localScale.x);
                    Vector3 p = localPoint * s;
                    return EvalChild(node.childIndexA, p, t) / s;
                }
                default:
                    return 1000f;
            }
        }

        float EvalChild(int childIndex, Vector3 localPoint, float t)
        {
            if (childIndex < 0 || childIndex >= _asset.nodes.Count)
                return 1000f;
            return EvalNode(childIndex, localPoint, t);
        }

        static float EvalPrimitive(SdfMaxNode node, Vector3 p, float t)
        {
            if (t < node.tMin || t > node.tMax)
                return 1000f;

            Matrix4x4 tr = Matrix4x4.TRS(node.localPosition, Quaternion.Euler(node.localRotationEuler), node.localScale);
            Vector3 lp = tr.inverse.MultiplyPoint3x4(p);

            switch (node.primitiveType)
            {
                case SdfPrimitiveType.Sphere:
                    return lp.magnitude - node.radius;
                case SdfPrimitiveType.Box:
                    Vector3 d = new Vector3(Mathf.Abs(lp.x), Mathf.Abs(lp.y), Mathf.Abs(lp.z)) - node.halfExtents;
                    return Mathf.Max(d.x, Mathf.Max(d.y, d.z));
                case SdfPrimitiveType.Capsule:
                    lp.y -= Mathf.Clamp(lp.y, -node.radius, node.radius);
                    return lp.magnitude - node.radius;
                case SdfPrimitiveType.Plane:
                    return lp.y;
                case SdfPrimitiveType.MeshBounds:
                    Vector3 q = new Vector3(
                        Mathf.Abs(lp.x) - node.halfExtents.x,
                        Mathf.Abs(lp.y) - node.halfExtents.y,
                        Mathf.Abs(lp.z) - node.halfExtents.z);
                    return Mathf.Max(q.x, Mathf.Max(q.y, q.z));
                default:
                    return 1000f;
            }
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
