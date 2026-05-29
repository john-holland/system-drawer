#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

/// <summary>Edit-mode smoke tests for player ragdoll BT wiring and safe execution when features are disabled.</summary>
public class PlayerRagdollPlayerSmokeTests
{
    [Test]
    public void PlayerVocabBuiltIn_RefreshWiring_AssignsBrainBehaviorTree_FromTemplate()
    {
        var actor = new GameObject("Actor");
        var brain = actor.AddComponent<Brain>();

        var template = new GameObject("FPTemplate");
        template.AddComponent<RagdollPlayerInputBuffer>();
        var tbt = template.AddComponent<BehaviorTree>();
        var seq = template.AddComponent<RagdollPlayerSequenceNode>();
        tbt.decisionTime = 0f;
        tbt.rootNode = seq;

        var vocab = actor.AddComponent<PlayerVocabBuiltIn>();
        vocab.targetBrain = brain;
        vocab.firstPersonTreePrefab = tbt;
        vocab.defaultPerspective = RagdollPlayerPerspective.FirstPerson;
        vocab.RefreshWiring();

        Assert.IsNotNull(brain.behaviorTree);
        Assert.AreNotSame(tbt, brain.behaviorTree);
        Assert.IsNotNull(brain.behaviorTree.GetComponent<RagdollPlayerInputBuffer>());

        Object.DestroyImmediate(actor);
        Object.DestroyImmediate(template);
    }

    [Test]
    public void RagdollPlayerSequence_AllOptionsOff_Execute_DoesNotThrow()
    {
        var root = new GameObject("BT_Root");
        var buffer = root.AddComponent<RagdollPlayerInputBuffer>();
        buffer.options.enableMovement = false;
        buffer.options.enableMouseLook = false;
        buffer.options.enableAnimations = false;

        var bt = root.AddComponent<BehaviorTree>();
        bt.decisionTime = 0f;
        var seq = root.AddComponent<RagdollPlayerSequenceNode>();
        bt.rootNode = seq;

        var readGo = new GameObject("Read");
        readGo.transform.SetParent(root.transform, false);
        readGo.AddComponent<ReadRagdollPlayerMovementInputNode>();

        var lookGo = new GameObject("Look");
        lookGo.transform.SetParent(root.transform, false);
        lookGo.AddComponent<MouseLookFirstPersonNode>();

        var moveGo = new GameObject("Move");
        moveGo.transform.SetParent(root.transform, false);
        moveGo.AddComponent<ApplyRagdollLocomotionNode>();

        var animGo = new GameObject("Anim");
        animGo.transform.SetParent(root.transform, false);
        animGo.AddComponent<DriveLocomotionAnimationNode>();

        seq.children.Clear();
        seq.children.Add(readGo.GetComponent<ReadRagdollPlayerMovementInputNode>());
        seq.children.Add(lookGo.GetComponent<MouseLookFirstPersonNode>());
        seq.children.Add(moveGo.GetComponent<ApplyRagdollLocomotionNode>());
        seq.children.Add(animGo.GetComponent<DriveLocomotionAnimationNode>());

        Assert.DoesNotThrow(() => bt.Execute());
        Assert.AreEqual(BehaviorTreeStatus.Success, bt.lastStatus);

        Object.DestroyImmediate(root);
    }
}
#endif
