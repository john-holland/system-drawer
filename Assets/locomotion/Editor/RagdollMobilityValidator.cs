#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pre-flight mobility checks for ragdoll IK training and fitting.
/// </summary>
public static class RagdollMobilityValidator
{
    public struct Report
    {
        public List<string> warnings;
        public int rigidbodyCount;
        public int colliderCount;
        public int kinematicCount;
        public int constrainedCount;
        public bool hasGround;

        public bool HasWarnings => warnings != null && warnings.Count > 0;
    }

    public static Report Validate(Transform ragdollRoot, bool hybridBuildAddsJointsNotColliders = true)
    {
        var report = new Report { warnings = new List<string>() };
        if (ragdollRoot == null)
        {
            report.warnings.Add("Ragdoll root is null.");
            return report;
        }

        Rigidbody[] rbs = ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
        Collider[] cols = ragdollRoot.GetComponentsInChildren<Collider>(true);
        report.rigidbodyCount = rbs.Length;
        report.colliderCount = cols.Length;

        if (rbs.Length == 0)
            report.warnings.Add("No Rigidbody components under ragdoll root.");

        for (int i = 0; i < rbs.Length; i++)
        {
            Rigidbody rb = rbs[i];
            if (rb == null)
                continue;
            if (rb.isKinematic)
                report.kinematicCount++;
            if (rb.constraints != RigidbodyConstraints.None)
                report.constrainedCount++;

            bool hasCollider = rb.GetComponent<Collider>() != null;
            if (!hasCollider)
            {
                Collider[] childCols = rb.GetComponentsInChildren<Collider>();
                hasCollider = childCols != null && childCols.Length > 0;
            }

            if (!hasCollider)
                report.warnings.Add($"Rigidbody '{rb.name}' has no collider on limb.");
        }

        if (report.kinematicCount == rbs.Length && rbs.Length > 0)
            report.warnings.Add("All rigidbodies are kinematic; mobility training will not move joints.");

        Transform rootRb = ragdollRoot.GetComponentInChildren<Rigidbody>()?.transform;
        if (rootRb != null && rootRb.GetComponent<Rigidbody>()?.constraints != RigidbodyConstraints.None)
            report.warnings.Add("Root rigidbody has non-None constraints (may block locomotion).");

        report.hasGround = Physics.Raycast(ragdollRoot.position + Vector3.up * 0.1f, Vector3.down, 5f);
        if (!report.hasGround)
            report.warnings.Add("No ground detected within 5 m below ragdoll root.");

        if (hybridBuildAddsJointsNotColliders && report.colliderCount < report.rigidbodyCount)
            report.warnings.Add("Hybrid ragdoll build adds joints but not colliders — add colliders before locomotion training.");

        return report;
    }
}
#endif
