using Locomotion.Camera;
using NUnit.Framework;
using UnityEngine;

public class CameraTransitionControllerTests
{
    [Test]
    public void Transition_ReachesGoalPose()
    {
        var go = new GameObject("cam");
        var cam = go.AddComponent<Camera>();
        cam.transform.position = Vector3.zero;
        var ctrl = go.AddComponent<CameraTransitionController>();
        ctrl.targetCamera = cam;

        var to = new CameraRigPose
        {
            position = new Vector3(0, 2, -5),
            rotation = Quaternion.LookRotation(Vector3.forward),
            fieldOfView = 40f,
            focusMode = CameraFocusMode.SceneFocus,
        };
        ctrl.RequestTransition(to, TransitionProfile.Default(0.01f));
        for (int i = 0; i < 5; i++)
            ctrl.SendMessage("Update");

        Assert.That(Vector3.Distance(cam.transform.position, to.position), Is.LessThan(0.5f));
        Object.DestroyImmediate(go);
    }
}
