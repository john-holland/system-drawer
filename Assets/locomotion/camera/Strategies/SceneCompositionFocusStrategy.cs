using UnityEngine;

namespace Locomotion.Camera.Strategies
{
    /// <summary>Heuristic contrast + rule-of-thirds composition scoring.</summary>
    public sealed class SceneCompositionFocusStrategy : ICameraFocusStrategy
    {
        public float orbitDistance = 8f;
        public float heightOffset = 2f;

        public CameraFocusMode Mode => CameraFocusMode.SceneFocus;

        public CameraRigPose ComputePose(CameraPathingContext ctx)
        {
            Vector3 anchor = ctx.objectTarget != null
                ? ctx.objectTarget.position
                : (ctx.characterRoot != null ? ctx.characterRoot.position : Vector3.zero);

            Vector3 camPos = anchor + new Vector3(orbitDistance * 0.6f, heightOffset, -orbitDistance * 0.8f);
            if (ctx.camera != null)
            {
                var t = ctx.camera.transform;
                camPos = anchor + (t.position - anchor).normalized * orbitDistance;
                camPos.y = anchor.y + heightOffset;
            }

            Vector3 lookTarget = anchor + new Vector3(orbitDistance * 0.15f, heightOffset * 0.3f, 0f);
            return new CameraRigPose
            {
                position = camPos,
                rotation = Quaternion.LookRotation(lookTarget - camPos, Vector3.up),
                fieldOfView = ctx.camera != null ? ctx.camera.fieldOfView : 50f,
                focusMode = Mode,
            };
        }

        public float ScoreCandidate(CameraRigPose pose, CameraPathingContext ctx)
        {
            float contrast = EstimateContrastHeuristic(pose);
            float thirds = ThirdsOffsetScore(pose);
            return contrast * 0.6f + thirds * 0.4f;
        }

        static float EstimateContrastHeuristic(CameraRigPose pose)
        {
            Vector3 fwd = pose.rotation * Vector3.forward;
            float elevation = Mathf.Abs(Vector3.Dot(fwd, Vector3.up));
            return Mathf.Clamp01(1f - elevation * 0.5f);
        }

        static float ThirdsOffsetScore(CameraRigPose pose)
        {
            Vector3 right = pose.rotation * Vector3.right;
            float lateral = Mathf.Abs(Vector3.Dot(right, Vector3.right));
            return Mathf.Clamp01(0.5f + lateral * 0.5f);
        }
    }
}
