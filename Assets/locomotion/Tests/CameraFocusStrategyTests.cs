using Locomotion.Camera;
using Locomotion.Camera.Strategies;
using NUnit.Framework;
using UnityEngine;

public class CameraFocusStrategyTests
{
    [Test]
    public void CharacterFocus_LooksAtHead()
    {
        var root = new GameObject("actor");
        var head = new GameObject("head");
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 1.7f, 0);

        var ctx = new CameraPathingContext { characterRoot = root.transform, headSocket = head.transform };
        var pose = new CharacterFocusStrategy().ComputePose(ctx);
        Vector3 fwd = pose.rotation * Vector3.forward;
        Assert.That(Vector3.Dot(fwd, (head.transform.position - pose.position).normalized), Is.GreaterThan(0.9f));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void ActorVision_Memorability_InRange()
    {
        var ctx = new CameraPathingContext { actorVisionSalience = 0.4f };
        float m = ActorVisionTrainingFocusStrategy.ComputeMemorabilityMl(ctx);
        Assert.That(m, Is.InRange(0f, 1f));
    }
}
