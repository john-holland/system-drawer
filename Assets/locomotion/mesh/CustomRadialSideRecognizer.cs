using System.Collections.Generic;
using UnityEngine;

/// <summary>Boundary-loop recognition and JointMiddle / FlyAway auto-resize for a piece mesh.</summary>
public static class CustomRadialSideRecognizer
{
    public static List<CustomRadialSidePose> Recognize(Mesh mesh)
    {
        var poses = new List<CustomRadialSidePose>();
        if (mesh == null)
            return poses;
        int subCount = Mathf.Max(1, mesh.subMeshCount);
        var verts = mesh.vertices;
        Vector3 centroid = mesh.bounds.center;
        for (int s = 0; s < subCount; s++)
        {
            var loops = SkinnedMeshLoopMaterialBreakout.BoundaryLoops(mesh, s);
            for (int i = 0; i < loops.Count; i++)
            {
                var pose = FromLoop(verts, loops[i], centroid);
                if (pose.HasValue)
                    poses.Add(pose.Value);
            }
        }
        poses.Sort((a, b) => LoopScore(b, centroid).CompareTo(LoopScore(a, centroid)));
        return poses;
    }

    public static void AutoResize(CustomRadialSideAsset asset, Mesh mesh)
    {
        if (asset == null || mesh == null)
            return;
        var poses = Recognize(mesh);
        if (poses.Count == 0)
            return;
        asset.ApplyPose(poses[0]);
        asset.lastRecognizeHash = mesh.GetInstanceID() + ":" + mesh.vertexCount + ":" + poses.Count;
    }

    public static CustomRadialSidePose? FromLoop(Vector3[] verts, List<int> loop, Vector3 pieceCentroid)
    {
        if (verts == null || loop == null || loop.Count < 3)
            return null;
        Vector3 origin = Vector3.zero;
        for (int i = 0; i < loop.Count; i++)
        {
            int vi = loop[i];
            if (vi < 0 || vi >= verts.Length)
                return null;
            origin += verts[vi];
        }
        origin /= loop.Count;
        Vector3 n = Vector3.zero;
        for (int i = 0; i < loop.Count; i++)
        {
            Vector3 a = verts[loop[i]] - origin;
            Vector3 b = verts[loop[(i + 1) % loop.Count]] - origin;
            n += Vector3.Cross(a, b);
        }
        if (n.sqrMagnitude < 1e-8f)
            return null;
        n.Normalize();
        if (Vector3.Dot(n, origin - pieceCentroid) < 0f)
            n = -n;
        Vector3 tangent = Vector3.Cross(n, Vector3.up);
        if (tangent.sqrMagnitude < 1e-6f)
            tangent = Vector3.Cross(n, Vector3.right);
        tangent.Normalize();

        Bounds loopBounds = new Bounds(verts[loop[0]], Vector3.zero);
        for (int i = 1; i < loop.Count; i++)
            loopBounds.Encapsulate(verts[loop[i]]);
        float thick = Mathf.Max(0.02f, loopBounds.extents.magnitude * 0.15f);
        var middle = new Bounds(origin - n * thick * 0.5f, loopBounds.size + n * thick);
        var fly = new Bounds(origin + n * thick * 1.5f, loopBounds.size + Vector3.one * thick);
        return new CustomRadialSidePose
        {
            origin = origin,
            normal = n,
            tangent = tangent,
            jointMiddle = middle,
            flyAway = fly,
            customAngle = 0f
        };
    }

    static float LoopScore(CustomRadialSidePose pose, Vector3 centroid)
    {
        float area = pose.jointMiddle.size.x * pose.jointMiddle.size.y;
        float outward = Vector3.Dot(pose.normal, pose.origin - centroid);
        float planar = 1f - Mathf.Abs(Vector3.Dot(pose.normal, Vector3.up)) * 0.25f;
        return area + outward + planar;
    }
}
