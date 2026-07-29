using UnityEngine;
using SdfMax;
using System.Collections.Generic;

/// <summary>
/// Destructive smudge of wet paint SDF Max when physics objects cross the canvas.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Smudge Collider")]
[RequireComponent(typeof(Collider))]
public sealed class PaintSmudgeCollider : MonoBehaviour
{
    public PaintCanvas canvas;
    public LayerMask ignoreMask;
    public float minRelativeSpeed = 0.05f;
    public PaintTransferDecal transferDecal;

    void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInParent<PaintCanvas>();
        if (transferDecal == null)
            transferDecal = GetComponent<PaintTransferDecal>();
    }

    void OnCollisionStay(Collision collision)
    {
        if (canvas == null || canvas.layerStack == null || !canvas.layerStack.enableDestructiveSmudge)
            return;
        if (collision == null || collision.collider == null) return;
        if (((1 << collision.gameObject.layer) & ignoreMask) != 0) return;
        if (collision.relativeVelocity.magnitude < minRelativeSpeed) return;

        ContactPoint contact = collision.GetContact(0);
        ApplySmudge(contact.point, contact.normal, collision.relativeVelocity, collision.collider);
    }

    void OnTriggerStay(Collider other)
    {
        if (canvas == null || canvas.layerStack == null || !canvas.layerStack.enableDestructiveSmudge)
            return;
        if (other == null) return;
        if (((1 << other.gameObject.layer) & ignoreMask) != 0) return;
        Vector3 point = other.ClosestPoint(transform.position);
        ApplySmudge(point, transform.forward, Vector3.zero, other);
    }

    public void ApplySmudge(Vector3 worldPoint, Vector3 normal, Vector3 velocity, Collider source)
    {
        if (!canvas.WorldToCanvasUv(worldPoint, out Vector2 uv))
            return;

        var stack = canvas.layerStack;
        var layer = stack.TopWetLayer();
        if (layer == null) return;
        if (layer.dry01 >= stack.smudgeDryLock && !stack.smudgeDryLayers)
            return;

        // Viscosity smear
        float strength = stack.smudgeStrength;
        Color smear = new Color(0f, Mathf.Clamp01(0.2f * strength), 0f, 0.1f);
        var visc = canvas.Viscosity;
        if (visc != null)
        {
            visc.Stamp(uv, smear, stack.smudgeRadius);
            visc.SampleUv(uv, out Color cur);
            cur.b = Mathf.Max(0f, cur.b - 0.15f * strength);
            visc.Stamp(uv, cur, stack.smudgeRadius * 0.5f);
            visc.Apply();
        }

        AppendSubtract(layer, worldPoint, stack.smudgeRadius * strength, velocity);

        Color paintColor = canvas.SamplePaintColor(uv);
        transferDecal?.TryApply(source, worldPoint, normal, paintColor);

        canvas.BindMaterials();
    }

    void AppendSubtract(PaintLayerExpression layer, Vector3 worldPoint, float radius, Vector3 velocity)
    {
        if (layer.composition == null)
        {
            layer.composition = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            layer.composition.nodes = new List<SdfMaxNode>();
        }

        Vector3 local = canvas.transform.InverseTransformPoint(worldPoint);
        Vector3 smear = canvas.transform.InverseTransformVector(velocity) * 0.02f;
        var nodes = layer.composition.nodes;
        int leaf = nodes.Count;
        nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Sphere,
            radius = Mathf.Max(0.002f, radius),
            sphereRadius = Mathf.Max(0.002f, radius),
            localPosition = local + smear
        });
        int prev = layer.composition.ResolveRootIndex();
        if (prev < 0)
        {
            layer.composition.rootNodeIndex = leaf;
            return;
        }
        nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.Subtract,
            childIndexA = prev,
            childIndexB = leaf
        });
        layer.composition.rootNodeIndex = nodes.Count - 1;
    }
}
