using UnityEngine;

namespace Locomotion.Camera.Strategies
{
    public sealed class ObjectFocusStrategy : ICameraFocusStrategy
    {
        public Vector3 offset = new Vector3(0f, 1.5f, -4f);

        public CameraFocusMode Mode => CameraFocusMode.ObjectFocus;

        public CameraRigPose ComputePose(CameraPathingContext ctx)
        {
            var target = ctx.objectTarget != null ? ctx.objectTarget : ctx.characterRoot;
            if (target == null)
                return CameraRigPose.FromCamera(ctx.camera, Mode);

            var bounds = new Bounds(target.position, Vector3.one);
            var renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                if (r != null) bounds.Encapsulate(r.bounds);

            Vector3 focus = bounds.center;
            Vector3 camPos = focus + target.TransformVector(offset);
            Quaternion rot = Quaternion.LookRotation(focus - camPos, Vector3.up);
            return new CameraRigPose
            {
                position = camPos,
                rotation = rot,
                fieldOfView = ctx.camera != null ? ctx.camera.fieldOfView : 60f,
                focusMode = Mode,
            };
        }

        public float ScoreCandidate(CameraRigPose pose, CameraPathingContext ctx) => 1f;
    }
}
