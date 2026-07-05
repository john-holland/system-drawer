using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Computed camera pose for an open/close topology stop.</summary>
    public struct OpenCloseCameraStop
    {
        public Vector3 position;
        public Quaternion rotation;
        public float fieldOfView;
        public Vector3 focusPoint;

        public static OpenCloseCameraStop Compute(
            OpenCloseTopologyNode node,
            OpenCloseTopologyNode parentStop = null,
            float baseFov = 60f)
        {
            if (node == null)
                return default;

            Vector3 focus = node.cameraHintCenter;
            if (node.concaveVolume.hasVolume)
                focus = node.concaveVolume.center;
            if (node.target != null)
            {
                var renderers = node.target.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        b.Encapsulate(renderers[i].bounds);
                    focus = b.center;
                }
            }

            Vector3 tangent = node.floorTangentHint;
            if (parentStop != null)
            {
                Vector3 fromPrev = focus - parentStop.cameraHintCenter;
                fromPrev.y = 0f;
                if (fromPrev.sqrMagnitude > 1e-4f)
                    tangent = fromPrev.normalized;
            }

            Vector3 inward = -node.openingNormal;
            if (inward.sqrMagnitude < 1e-4f && node.target != null)
                inward = -node.target.transform.forward;
            inward.Normalize();

            Vector3 viewDir = (inward * 0.7f + tangent * 0.3f).normalized;
            float distance = node.concaveVolume.hasVolume
                ? Mathf.Max(node.concaveVolume.size.magnitude * 0.6f, 1.5f)
                : 2.5f;

            Vector3 camPos = focus - viewDir * distance + Vector3.up * 0.5f;
            var rot = Quaternion.LookRotation(focus - camPos, Vector3.up);
            float fov = baseFov;
            if (node.concaveVolume.hasVolume)
                fov = Mathf.Clamp(baseFov - node.concaveVolume.size.magnitude * 2f, 25f, baseFov);

            return new OpenCloseCameraStop
            {
                position = camPos,
                rotation = rot,
                fieldOfView = fov,
                focusPoint = focus,
            };
        }
    }
}
