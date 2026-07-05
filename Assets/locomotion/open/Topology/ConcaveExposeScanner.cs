using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Open.Topology
{
    /// <summary>Locomotion-focused concavity and opening scanner for open/close topology.</summary>
    public static class ConcaveExposeScanner
    {
        const float ApproachOffset = 0.75f;

        public static void ScanHierarchy(Transform root, OpenCloseTopologyAsset asset, OpenCloseTopologyNode node, AutoCloseBtMode defaultAutoClose)
        {
            if (root == null || node == null || asset == null)
                return;

            node.target = root.gameObject;
            node.nodeId = string.IsNullOrEmpty(node.nodeId) ? root.name : node.nodeId;
            node.jointKind = OpenableJointProbe.InferKind(root.gameObject);
            if (node.autoCloseBt == AutoCloseBtMode.OnStopExit && defaultAutoClose != AutoCloseBtMode.OnStopExit)
                node.autoCloseBt = defaultAutoClose;

            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                node.concaveVolume = new EnclosedVolumeRef
                {
                    hasVolume = true,
                    center = b.center,
                    size = b.size,
                };
            }

            var opening = FindPrimaryOpening(root, renderers);
            if (opening != null)
            {
                node.openingNormal = opening.normal;
                node.cameraHintCenter = opening.center;
                node.floorTangentHint = Vector3.ProjectOnPlane(Vector3.Cross(opening.normal, Vector3.up), Vector3.up);
                if (node.floorTangentHint.sqrMagnitude < 1e-4f)
                    node.floorTangentHint = Vector3.right;
                node.floorTangentHint.Normalize();

                var anchor = opening.center - opening.normal * ApproachOffset;
                anchor.y = opening.center.y;
                node.approachAnchorWorld = anchor;
                node.hasApproachAnchor = true;
                node.reachRadiusMeters = Mathf.Max(0.4f, Mathf.Sqrt(opening.area) * 0.25f);
            }
            else if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                node.cameraHintCenter = b.center;
                node.approachAnchorWorld = b.center - root.forward * ApproachOffset;
                node.hasApproachAnchor = true;
                node.openingNormal = -root.forward;
            }

            var childTransforms = new List<Transform>();
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.GetComponent<OpenableJointDriver>() != null ||
                    c.GetComponent<OpenableLatch>() != null ||
                    c.GetComponent<HingeJoint>() != null ||
                    c.GetComponent<ConfigurableJoint>() != null)
                    childTransforms.Add(c);
            }

            asset.SetChildCount(node, childTransforms.Count);

            for (int i = 0; i < childTransforms.Count; i++)
                ScanHierarchy(childTransforms[i], asset, asset.GetChild(node, i), defaultAutoClose);
        }

        static OpeningLoop FindPrimaryOpening(Transform root, Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0)
                return null;

            var loop = new OpeningLoop();
            var b = renderers[0].bounds;
            loop.center = b.center;
            loop.normal = -root.forward;
            loop.vertices.Add(b.min);
            loop.vertices.Add(new Vector3(b.max.x, b.min.y, b.center.z));
            loop.vertices.Add(b.max);
            loop.vertices.Add(new Vector3(b.min.x, b.max.y, b.center.z));
            loop.CalculateProperties();
            return loop;
        }

        public static List<EnclosedVolume> FindEnclosedVolumes(Collider[] colliders)
        {
            var result = new List<EnclosedVolume>();
            if (colliders == null)
                return result;

            foreach (var col in colliders)
            {
                if (col == null)
                    continue;
                var b = col.bounds;
                result.Add(new EnclosedVolume
                {
                    bounds = b,
                    center = b.center,
                    volume = b.size.x * b.size.y * b.size.z,
                    lowestPoint = b.min.y,
                    highestOpening = b.max.y,
                });
            }
            return result;
        }
    }
}
