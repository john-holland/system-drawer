using NUnit.Framework;
using UnityEngine;

public class NonIkRagdollAnimatorPhysicsTests
{
    [Test]
    public void Play_SetsRigidbodiesKinematic_RestoreOnStop()
    {
        var root = new GameObject("ragdoll");
        var child = new GameObject("limb");
        child.transform.SetParent(root.transform);
        var rb = child.AddComponent<Rigidbody>();
        rb.isKinematic = false;

        var ragdoll = root.AddComponent<RagdollSystem>();
        ragdoll.ragdollRoot = root.transform;

        var anim = new NonIkRagdollAnimator();
        var clip = new AnimationClip { name = "test", legacy = true };

        anim.Play(ragdoll, clip);
        Assert.IsTrue(rb.isKinematic);

        anim.Stop();
        Assert.IsFalse(rb.isKinematic);

        Object.DestroyImmediate(root);
    }
}
