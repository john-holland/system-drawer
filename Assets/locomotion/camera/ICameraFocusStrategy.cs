using UnityEngine;

namespace Locomotion.Camera
{
    public interface ICameraFocusStrategy
    {
        CameraFocusMode Mode { get; }
        CameraRigPose ComputePose(CameraPathingContext ctx);
        float ScoreCandidate(CameraRigPose pose, CameraPathingContext ctx);
    }

    public sealed class CameraPathingContext
    {
        public UnityEngine.Camera camera;
        public Transform objectTarget;
        public Transform characterRoot;
        public Transform headSocket;
        public Transform firstPersonPivot;
        public HierarchicalPathingOctTree pathingOctTree;
        public float actorVisionSalience;
        public float memorabilityMl;
    }
}
