using Locomotion.Camera;
using Locomotion.Camera.Strategies;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Concavity-aware object focus for open/close camera stops.</summary>
    public sealed class OpenCloseFocusStrategy : ICameraFocusStrategy
    {
        public OpenCloseTopologyNode stop;
        public OpenCloseTopologyNode parentStop;
        public Vector3 extraOffset = new Vector3(0f, 0.5f, 0f);

        readonly ObjectFocusStrategy _fallback = new ObjectFocusStrategy();

        public CameraFocusMode Mode => CameraFocusMode.ObjectFocus;

        public CameraRigPose ComputePose(CameraPathingContext ctx)
        {
            if (stop == null)
                return _fallback.ComputePose(ctx);

            var computed = OpenCloseCameraStop.Compute(stop, parentStop, ctx.camera != null ? ctx.camera.fieldOfView : 60f);
            return new CameraRigPose
            {
                position = computed.position + extraOffset,
                rotation = computed.rotation,
                fieldOfView = computed.fieldOfView,
                focusMode = Mode,
            };
        }

        public float ScoreCandidate(CameraRigPose pose, CameraPathingContext ctx) => 1f;
    }
}
