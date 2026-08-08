#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Locomotion.EditorTools
{
    /// <summary>Copies Rigidbody / ConfigurableJoint / Collider state from a source bone to a dest bone.</summary>
    public static class RagdollPhysicsCopy
    {
        public static void CopyBonePhysics(
            Transform sourceBone,
            Transform destBone,
            Rigidbody destParentRb,
            bool copyColliders = true,
            bool copyJointLimitsAndDrives = true)
        {
            if (sourceBone == null || destBone == null) return;

            var srcRb = sourceBone.GetComponent<Rigidbody>();
            if (srcRb != null)
            {
                var dstRb = destBone.GetComponent<Rigidbody>();
                if (dstRb == null)
                    dstRb = Undo.AddComponent<Rigidbody>(destBone.gameObject);
                Undo.RecordObject(dstRb, "Copy Rigidbody");
                EditorUtility.CopySerialized(srcRb, dstRb);
            }

            var srcJoint = sourceBone.GetComponent<ConfigurableJoint>();
            if (srcJoint != null)
            {
                var dstJoint = destBone.GetComponent<ConfigurableJoint>();
                if (dstJoint == null)
                    dstJoint = Undo.AddComponent<ConfigurableJoint>(destBone.gameObject);
                Undo.RecordObject(dstJoint, "Copy ConfigurableJoint");
                if (copyJointLimitsAndDrives)
                    EditorUtility.CopySerialized(srcJoint, dstJoint);
                dstJoint.connectedBody = destParentRb;
                dstJoint.autoConfigureConnectedAnchor = true;
            }

            if (!copyColliders) return;

            CopyCollidersOfType<BoxCollider>(sourceBone, destBone);
            CopyCollidersOfType<CapsuleCollider>(sourceBone, destBone);
            CopyCollidersOfType<SphereCollider>(sourceBone, destBone);
        }

        static void CopyCollidersOfType<T>(Transform sourceBone, Transform destBone) where T : Collider
        {
            var srcCols = sourceBone.GetComponents<T>();
            if (srcCols == null || srcCols.Length == 0) return;

            var existing = destBone.GetComponents<T>();
            for (int i = 0; i < srcCols.Length; i++)
            {
                T dst;
                if (i < existing.Length)
                    dst = existing[i];
                else
                    dst = Undo.AddComponent<T>(destBone.gameObject);
                Undo.RecordObject(dst, "Copy Collider");
                EditorUtility.CopySerialized(srcCols[i], dst);
            }
        }

        /// <summary>Resolve parent rigidbody for a humanoid bone on the destination animator.</summary>
        public static Rigidbody ResolveParentRigidbody(Animator destAnimator, Transform destBone, Rigidbody rootRb)
        {
            if (destBone == null) return rootRb;
            if (destBone.parent != null)
            {
                var parentRb = destBone.parent.GetComponentInParent<Rigidbody>();
                if (parentRb != null && parentRb.transform != destBone)
                    return parentRb;
            }
            return rootRb;
        }
    }
}
#endif
