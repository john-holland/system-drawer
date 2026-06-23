using Locomotion.Senses;
using UnityEngine;

namespace Locomotion.Camera.Strategies
{
    /// <summary>Weights framing by Eyes/Sensor detections and brain attention signals.</summary>
    public sealed class ActorVisionTrainingFocusStrategy : ICameraFocusStrategy
    {
        public float orbitDistance = 5f;

        public CameraFocusMode Mode => CameraFocusMode.MlActorVisionTrainingFocus;

        public CameraRigPose ComputePose(CameraPathingContext ctx)
        {
            Vector3 focus = ResolveVisionFocus(ctx);
            Vector3 camPos = focus - (ctx.camera != null ? ctx.camera.transform.forward : Vector3.forward) * orbitDistance;
            camPos.y = focus.y + 1.2f;
            ctx.memorabilityMl = ComputeMemorabilityMl(ctx);
            return new CameraRigPose
            {
                position = camPos,
                rotation = Quaternion.LookRotation(focus - camPos, Vector3.up),
                fieldOfView = ctx.camera != null ? ctx.camera.fieldOfView : 52f,
                focusMode = Mode,
            };
        }

        public float ScoreCandidate(CameraRigPose pose, CameraPathingContext ctx)
        {
            return ComputeMemorabilityMl(ctx);
        }

        public static float ComputeMemorabilityMl(CameraPathingContext ctx)
        {
            float salience = ctx.actorVisionSalience;
            if (salience <= 0f)
                salience = EstimateSalienceFromEyes(ctx);

            float brainBoost = 0f;
            var brain = ctx.characterRoot != null ? ctx.characterRoot.GetComponentInChildren<Brain>() : null;
            if (brain != null)
                brainBoost = Mathf.Clamp01(brain.GetThoughtHistorySnapshot().Count * 0.08f);

            return Mathf.Clamp01(salience * 0.7f + brainBoost * 0.3f);
        }

        static float EstimateSalienceFromEyes(CameraPathingContext ctx)
        {
            var eyes = ctx.characterRoot != null ? ctx.characterRoot.GetComponentInChildren<Eyes>() : null;
            if (eyes == null || ctx.camera == null)
                return 0.35f;

            int hits = 0;
            if (eyes.leftEye != null && eyes.leftEye.sensorData.detectedObjects != null)
                hits += eyes.leftEye.sensorData.detectedObjects.Count;
            if (eyes.rightEye != null && eyes.rightEye.sensorData.detectedObjects != null)
                hits += eyes.rightEye.sensorData.detectedObjects.Count;

            return Mathf.Clamp01(hits * 0.12f);
        }

        static Vector3 ResolveVisionFocus(CameraPathingContext ctx)
        {
            var eyes = ctx.characterRoot != null ? ctx.characterRoot.GetComponentInChildren<Eyes>() : null;
            if (eyes?.leftEye != null && eyes.leftEye.sensorData.detectedObjects != null
                && eyes.leftEye.sensorData.detectedObjects.Count > 0)
            {
                var go = eyes.leftEye.sensorData.detectedObjects[0];
                if (go != null)
                    return go.transform.position;
            }

            return CentroidFocusStrategy.ComputeFrustumCentroid(ctx);
        }
    }
}
