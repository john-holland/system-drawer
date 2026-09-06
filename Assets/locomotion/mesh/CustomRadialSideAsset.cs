using System.Collections.Generic;
using UnityEngine;

/// <summary>Authored CustomRadialSide: loop grabber + JointMiddle / FlyAway bounds + optional wrap.</summary>
[CreateAssetMenu(fileName = "CustomRadialSide", menuName = "Locomotion/Mesh/Custom Radial Side")]
public sealed class CustomRadialSideAsset : ScriptableObject
{
    public SkinnedMeshLoopSectionAsset sectionAsset;
    public string loopId = "";
    public GameObject recognizedFromPiece;
    public string lastRecognizeHash = "";
    public Bounds jointMiddle = new Bounds(Vector3.zero, Vector3.one);
    public Bounds flyAway = new Bounds(new Vector3(0f, 0f, 0.6f), Vector3.one);
    public Vector3 origin;
    public Vector3 normal = Vector3.forward;
    public Vector3 tangent = Vector3.right;
    [Tooltip("Ring wrap degrees. 0 = auto.")]
    public float customAngle;
    public GameObject customAngleObject;

    public CustomRadialSidePose ToPose()
    {
        return new CustomRadialSidePose
        {
            origin = origin,
            normal = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.forward,
            tangent = tangent.sqrMagnitude > 1e-8f ? tangent.normalized : Vector3.right,
            jointMiddle = jointMiddle,
            flyAway = flyAway,
            customAngle = customAngle,
            hasCustomAngleObject = customAngleObject != null,
            customAngleObjectWorld = customAngleObject != null ? customAngleObject.transform.position : Vector3.zero
        };
    }

    public void ApplyPose(CustomRadialSidePose pose)
    {
        origin = pose.origin;
        normal = pose.normal;
        tangent = pose.tangent;
        jointMiddle = pose.jointMiddle;
        flyAway = pose.flyAway;
        if (pose.customAngle > 0f)
            customAngle = pose.customAngle;
    }

    public List<int> JointMiddleVertexIndices(Mesh mesh, Matrix4x4 meshLocalToWorld)
    {
        var list = new List<int>();
        if (sectionAsset == null || string.IsNullOrEmpty(loopId))
            return CollectBoundsVerts(mesh, meshLocalToWorld, jointMiddle, list);
        var loop = sectionAsset.GetLoop(loopId);
        if (loop == null)
            return CollectBoundsVerts(mesh, meshLocalToWorld, jointMiddle, list);
        return loop.CombinedVertexIndices(mesh != null ? mesh.vertices : null, meshLocalToWorld);
    }

    static List<int> CollectBoundsVerts(Mesh mesh, Matrix4x4 meshLocalToWorld, Bounds local, List<int> dst)
    {
        if (mesh == null)
            return dst;
        var verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            if (local.Contains(verts[i]) || local.Contains(meshLocalToWorld.MultiplyPoint3x4(verts[i])))
                dst.Add(i);
        }
        return dst;
    }
}
