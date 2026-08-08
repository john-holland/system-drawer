#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Locomotion.EditorTools;
using Locomotion.Musculature;

public sealed class RagdollFromScratchReplicatorTests
{
    sealed class DonorCustomMarker : MonoBehaviour
    {
        public int value = 7;
    }

    [Test]
    public void PhysicsCopy_CopiesMassJointLimitAndCapsuleHeight()
    {
        var srcGo = new GameObject("srcBone");
        var dstGo = new GameObject("dstBone");
        var parentGo = new GameObject("parent");
        var parentRb = parentGo.AddComponent<Rigidbody>();
        parentRb.mass = 5f;

        var srcRb = srcGo.AddComponent<Rigidbody>();
        srcRb.mass = 2.5f;
        var srcJoint = srcGo.AddComponent<ConfigurableJoint>();
        srcJoint.connectedBody = parentRb;
        var limit = srcJoint.lowAngularXLimit;
        limit.limit = -35f;
        srcJoint.lowAngularXLimit = limit;
        var srcCap = srcGo.AddComponent<CapsuleCollider>();
        srcCap.height = 1.25f;
        srcCap.radius = 0.2f;

        RagdollPhysicsCopy.CopyBonePhysics(srcGo.transform, dstGo.transform, parentRb, copyColliders: true);

        var dstRb = dstGo.GetComponent<Rigidbody>();
        Assert.IsNotNull(dstRb);
        Assert.AreEqual(2.5f, dstRb.mass, 0.001f);

        var dstJoint = dstGo.GetComponent<ConfigurableJoint>();
        Assert.IsNotNull(dstJoint);
        Assert.AreEqual(parentRb, dstJoint.connectedBody);
        Assert.AreEqual(-35f, dstJoint.lowAngularXLimit.limit, 0.001f);

        var dstCap = dstGo.GetComponent<CapsuleCollider>();
        Assert.IsNotNull(dstCap);
        Assert.AreEqual(1.25f, dstCap.height, 0.001f);

        Object.DestroyImmediate(srcGo);
        Object.DestroyImmediate(dstGo);
        Object.DestroyImmediate(parentGo);
    }

    [Test]
    public void Stripper_RemovesCustomComponent_AndRecordsLeftover()
    {
        var root = new GameObject("Root");
        var child = new GameObject("Hips");
        child.transform.SetParent(root.transform, false);
        child.AddComponent<Rigidbody>();
        child.AddComponent<DonorCustomMarker>().value = 9;

        var map = RagdollComponentStripper.StripAndCollectLeftovers(root);

        Assert.IsNotNull(child.GetComponent<Rigidbody>());
        Assert.IsNull(child.GetComponent<DonorCustomMarker>());
        Assert.GreaterOrEqual(map.entries.Count, 1);

        bool found = false;
        for (int i = 0; i < map.entries.Count; i++)
        {
            var e = map.entries[i];
            if (e.hierarchyPath == null || !e.hierarchyPath.EndsWith("Hips")) continue;
            for (int t = 0; t < e.componentTypes.Count; t++)
            {
                if (e.componentTypes[t] != null && e.componentTypes[t].Contains(nameof(DonorCustomMarker)))
                    found = true;
            }
        }
        Assert.IsTrue(found, "Leftover map should list DonorCustomMarker under Hips. Map:\n" + map.ToReadableText());

        Object.DestroyImmediate(root);
    }

    [Test]
    public void Stripper_KeepsSkinnedMeshAndAnimator()
    {
        var root = new GameObject("Actor");
        root.AddComponent<Animator>();
        var meshGo = new GameObject("Body");
        meshGo.transform.SetParent(root.transform, false);
        meshGo.AddComponent<SkinnedMeshRenderer>();
        meshGo.AddComponent<DonorCustomMarker>();

        RagdollComponentStripper.StripAndCollectLeftovers(root);

        Assert.IsNotNull(root.GetComponent<Animator>());
        Assert.IsNotNull(meshGo.GetComponent<SkinnedMeshRenderer>());
        Assert.IsNull(meshGo.GetComponent<DonorCustomMarker>());

        Object.DestroyImmediate(root);
    }

    [Test]
    public void Stripper_KeepsFingersAndHairDrivers()
    {
        var root = new GameObject("Actor");
        var fingerGo = new GameObject("Index");
        fingerGo.transform.SetParent(root.transform, false);
        fingerGo.AddComponent<RagdollFinger>();
        var digitGo = new GameObject("Digit0");
        digitGo.transform.SetParent(fingerGo.transform, false);
        digitGo.AddComponent<RagdollDigit>();
        var hairGo = new GameObject("Hair");
        hairGo.transform.SetParent(root.transform, false);
        hairGo.AddComponent<HairPlumePhysicsDriver>();
        hairGo.AddComponent<HairBodyCapsuleBinder>();
        hairGo.AddComponent<DonorCustomMarker>();

        RagdollComponentStripper.StripAndCollectLeftovers(root);

        Assert.IsNotNull(fingerGo.GetComponent<RagdollFinger>());
        Assert.IsNotNull(digitGo.GetComponent<RagdollDigit>());
        Assert.IsNotNull(hairGo.GetComponent<HairPlumePhysicsDriver>());
        Assert.IsNotNull(hairGo.GetComponent<HairBodyCapsuleBinder>());
        Assert.IsNull(hairGo.GetComponent<DonorCustomMarker>());

        Object.DestroyImmediate(root);
    }
}
#endif
