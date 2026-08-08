#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Locomotion.EditorTools;

/// <summary>EditMode tests for head-centroid Brain + dual LSTM AutoWire.</summary>
public sealed class RagdollBrainDualLstmTests
{
    [Test]
    public void EnsureBrainWithDualLstm_CreatesLeftRightPredictors_UnderHead()
    {
        var actor = new GameObject("Actor");
        actor.SetActive(false);
        var head = new GameObject("Head");
        head.transform.SetParent(actor.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        // No humanoid Animator — ResolveHead falls back; place a RagdollSystem.headComponent path via parenting name.
        // Without headComponent/Animator, Brain parents under actor; simulate head by naming and using Ensure after parenting Brain target.
        // Force head resolution via a minimal RagdollSystem + RagdollHead on the Head bone.
        var system = actor.AddComponent<RagdollSystem>();
        var ragdollHead = head.AddComponent<Locomotion.Musculature.RagdollHead>();
        system.headComponent = ragdollHead;

        Brain brain = RagdollAutoWire.EnsureBrainWithDualLstm(actor, new RagdollAutoWire.Report());
        Assert.IsNotNull(brain);
        Assert.AreSame(head.transform, brain.transform.parent);
        Assert.AreEqual(Vector3.zero, brain.transform.localPosition);
        Assert.IsTrue(brain.enableDualLSTM);
        Assert.IsNotNull(brain.leftLSTM);
        Assert.IsNotNull(brain.rightLSTM);
        Assert.AreEqual("LeftLSTM", brain.leftLSTM.gameObject.name);
        Assert.AreEqual("RightLSTM", brain.rightLSTM.gameObject.name);
        Assert.AreSame(brain.transform, brain.leftLSTM.transform.parent);
        Assert.AreSame(brain.transform, brain.rightLSTM.transform.parent);
        Assert.AreSame(actor, brain.attachedBodyPart);

        int brains = actor.GetComponentsInChildren<Brain>(true).Length;
        int left = 0;
        int right = 0;
        foreach (var p in actor.GetComponentsInChildren<LSTMPredictor>(true))
        {
            if (p.gameObject.name == "LeftLSTM") left++;
            if (p.gameObject.name == "RightLSTM") right++;
        }

        // Idempotent second call
        Brain again = RagdollAutoWire.EnsureBrainWithDualLstm(actor, new RagdollAutoWire.Report());
        Assert.AreSame(brain, again);
        Assert.AreEqual(brains, actor.GetComponentsInChildren<Brain>(true).Length);
        Assert.AreEqual(left, CountNamed(actor, "LeftLSTM"));
        Assert.AreEqual(right, CountNamed(actor, "RightLSTM"));
        Assert.AreEqual(1, left);
        Assert.AreEqual(1, right);

        Object.DestroyImmediate(actor);
    }

    [Test]
    public void EnsureBrainWithDualLstm_MigratesRootBrain_UnderHead()
    {
        var actor = new GameObject("Actor");
        actor.SetActive(false);
        var head = new GameObject("Head");
        head.transform.SetParent(actor.transform, false);

        var system = actor.AddComponent<RagdollSystem>();
        system.headComponent = head.AddComponent<Locomotion.Musculature.RagdollHead>();

        var rootBrain = actor.AddComponent<Brain>();
        var existingBt = actor.AddComponent<BehaviorTree>();
        rootBrain.behaviorTree = existingBt;

        Brain brain = RagdollAutoWire.EnsureBrainWithDualLstm(actor, new RagdollAutoWire.Report());
        Assert.IsNotNull(brain);
        Assert.AreNotSame(actor, brain.gameObject);
        Assert.IsNull(actor.GetComponent<Brain>());
        Assert.AreSame(existingBt, brain.behaviorTree);
        Assert.IsNotNull(brain.leftLSTM);
        Assert.IsNotNull(brain.rightLSTM);

        Object.DestroyImmediate(actor);
    }

    static int CountNamed(GameObject root, string name)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) n++;
        }
        return n;
    }
}
#endif
