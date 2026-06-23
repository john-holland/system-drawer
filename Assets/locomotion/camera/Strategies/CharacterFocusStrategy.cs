using UnityEngine;

namespace Locomotion.Camera.Strategies
{
    public sealed class CharacterFocusStrategy : ICameraFocusStrategy
    {
        public Vector3 orbitOffset = new Vector3(0f, 1.6f, -3.5f);

        public CameraFocusMode Mode => CameraFocusMode.Character;

        public CameraRigPose ComputePose(CameraPathingContext ctx)
        {
            var root = ctx.characterRoot;
            if (root == null)
                return CameraRigPose.FromCamera(ctx.camera, Mode);

            Vector3 lookAt = ctx.headSocket != null ? ctx.headSocket.position : root.position + Vector3.up * 1.5f;
            Vector3 camPos = lookAt + root.TransformVector(orbitOffset);
            return new CameraRigPose
            {
                position = camPos,
                rotation = Quaternion.LookRotation(lookAt - camPos, Vector3.up),
                fieldOfView = ctx.camera != null ? ctx.camera.fieldOfView : 55f,
                focusMode = Mode,
            };
        }

        public float ScoreCandidate(CameraRigPose pose, CameraPathingContext ctx) => 1f;
    }
}
