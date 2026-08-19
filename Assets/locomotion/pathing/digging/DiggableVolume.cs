using SdfMax;
using UnityEngine;

public enum DiggableVolumeKind
{
    Wall = 0,
    Soil = 1,
    Foundation = 2
}

/// <summary>Generic diggable volume for houses, prisons, and excavation sites.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Digging/Diggable Volume")]
public class DiggableVolume : MonoBehaviour
{
    public bool diggable = true;
    [Range(0f, 1f)] public float destructibility01 = 0.5f;
    [Range(0f, 1f)] public float tunnelStress01;
    public string materialClass = "masonry";
    public Bounds localBounds = new Bounds(Vector3.zero, Vector3.one * 2f);
    public int floorIndex = 1;
    public DiggableVolumeKind volumeKind = DiggableVolumeKind.Wall;
    public SdfMaxCompositionAsset sdf;

    public Bounds WorldBounds
    {
        get
        {
            var b = localBounds;
            Vector3 c = transform.TransformPoint(b.center);
            return new Bounds(c, Vector3.Scale(b.size, transform.lossyScale));
        }
    }

    public int ApplyScoop(DigScoopSph sph, Vector3 contactWorld, float amount)
    {
        sph ??= new DigScoopSph();
        var node = sph.BuildSubtractNode(transform.InverseTransformPoint(contactWorld), amount);
        if (sdf == null)
            return 0;
        if (sdf.nodes == null)
            sdf.nodes = new System.Collections.Generic.List<SdfMaxNode>();
        int child = sdf.nodes.Count;
        sdf.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = node.primitiveType,
            localPosition = node.localPosition,
            radius = node.radius,
            sphereRadius = node.sphereRadius
        });
        int prevRoot = sdf.ResolveRootIndex();
        sdf.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.Subtract,
            childIndexA = prevRoot,
            childIndexB = child
        });
        sdf.rootNodeIndex = sdf.nodes.Count - 1;
        return 1;
    }
}
