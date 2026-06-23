using UnityEngine;

namespace Locomotion.Camera.Strategies
{
    public sealed class CentroidFocusStrategy : ICameraFocusStrategy
    {
        public float standOffDistance = 6f;

        public CameraFocusMode Mode => CameraFocusMode.CentroidFocus;

        public CameraRigPose ComputePose(CameraPathingContext ctx)
        {
            Vector3 centroid = ComputeFrustumCentroid(ctx);
            Vector3 camPos = centroid - (ctx.camera != null ? ctx.camera.transform.forward : Vector3.forward) * standOffDistance;
            return new CameraRigPose
            {
                position = camPos,
                rotation = Quaternion.LookRotation(centroid - camPos, Vector3.up),
                fieldOfView = ctx.camera != null ? ctx.camera.fieldOfView : 55f,
                focusMode = Mode,
            };
        }

        public float ScoreCandidate(CameraRigPose pose, CameraPathingContext ctx) => 1f;

        public static Vector3 ComputeFrustumCentroid(CameraPathingContext ctx)
        {
            if (ctx.pathingOctTree == null || ctx.camera == null)
            {
                return ctx.characterRoot != null ? ctx.characterRoot.position : Vector3.zero;
            }

            var leaves = FrustumAlignedOctreeBasis.CollectVisibleLeafCenters(ctx.camera, ctx.pathingOctTree.Leaves);
            if (leaves.Count == 0)
                return ctx.camera.transform.position + ctx.camera.transform.forward * 5f;

            Vector3 sum = Vector3.zero;
            foreach (var c in leaves)
                sum += c;
            return sum / leaves.Count;
        }
    }
}
