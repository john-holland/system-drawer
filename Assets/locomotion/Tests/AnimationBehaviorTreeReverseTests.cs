using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class AnimationBehaviorTreeReverseTests
{
    [Test]
    public void SequenceRoot_StartsAtLastChildWhenDirectionNegative()
    {
        var abtGo = new GameObject("ABT");
        var abt = abtGo.AddComponent<AnimationBehaviorTree>();
        abt.playbackDirection = -1;

        var rootGo = new GameObject("Root");
        rootGo.transform.SetParent(abtGo.transform);
        var root = rootGo.AddComponent<AnimationBehaviorTreeNode>();
        root.nodeType = NodeType.Sequence;
        root.rootBehaviorTree = abt;

        for (int i = 0; i < 3; i++)
        {
            var childGo = new GameObject($"Frame_{i}");
            childGo.transform.SetParent(rootGo.transform);
            var child = childGo.AddComponent<AnimationBehaviorTreeNode>();
            child.frameIndex = i;
            child.rootBehaviorTree = abt;
            root.children.Add(child);
        }

        var tree = abtGo.AddComponent<BehaviorTree>();
        tree.rootNode = root;
        root.OnEnter(tree);

        FieldInfo idxField = typeof(AnimationBehaviorTreeNode).GetField(
            "_sequenceChildIndex",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(idxField);
        Assert.AreEqual(2, (int)idxField.GetValue(root));

        Object.DestroyImmediate(abtGo);
    }
}
