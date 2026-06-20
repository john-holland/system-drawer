using NUnit.Framework;
using UnityEngine;

public class NonIkRagdollAnimatorTests
{
    [Test]
    public void PlayStop_TogglesSuppressMotorActuation()
    {
        var go = new GameObject("ragdoll");
        var ragdoll = go.AddComponent<RagdollSystem>();
        ragdoll.ragdollRoot = go.transform;
        var anim = new NonIkRagdollAnimator();
        var clip = new AnimationClip();
        clip.name = "test";
        clip.legacy = true;

        Assert.IsFalse(ragdoll.suppressMotorActuation);
        anim.Play(ragdoll, clip);
        Assert.IsTrue(ragdoll.suppressMotorActuation);
        Assert.IsTrue(anim.IsPlaying);
        anim.Stop();
        Assert.IsFalse(ragdoll.suppressMotorActuation);
        Assert.IsFalse(anim.IsPlaying);
        Object.DestroyImmediate(go);
    }
}
