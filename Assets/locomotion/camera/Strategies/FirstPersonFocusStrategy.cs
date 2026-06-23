using UnityEngine;

namespace Locomotion.Camera.Strategies
{
    public sealed class FirstPersonFocusStrategy : ICameraFocusStrategy
    {
        public CameraFocusMode Mode => CameraFocusMode.FirstPerson;

        public CameraRigPose ComputePose(CameraPathingContext ctx)
        {
            var pivot = ctx.firstPersonPivot ?? ctx.headSocket ?? ctx.characterRoot;
            if (pivot == null)
                return CameraRigPose.FromCamera(ctx.camera, Mode);

            return new CameraRigPose
            {
                position = pivot.position,
                rotation = pivot.rotation,
                fieldOfView = ctx.camera != null ? ctx.camera.fieldOfView : 70f,
                focusMode = Mode,
            };
        }

        public float ScoreCandidate(CameraRigPose pose, CameraPathingContext ctx) => 1f;
    }
}
