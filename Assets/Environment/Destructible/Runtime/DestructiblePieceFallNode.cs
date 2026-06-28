using UnityEngine;

namespace DestructibleEnvironment
{
    public class DestructiblePieceFallNode : BehaviorTreeNode
    {
        public int pieceId = -1;
        public DestructiblePlaybackController playback;

        bool _settled;

        public override void OnEnter(BehaviorTree tree)
        {
            _settled = false;
        }

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            if (_settled)
                return BehaviorTreeStatus.Success;

            DestructibleFallContext ctx = ResolveContext();
            if (ctx == null || ctx.Pool == null || ctx.Bake == null)
                return BehaviorTreeStatus.Failure;

            if (!ctx.PieceIdToSlot.ContainsKey(pieceId))
                return BehaviorTreeStatus.Success;

            if (!ctx.AreSupportsSettled(pieceId))
                return BehaviorTreeStatus.Running;

            if (!ctx.PieceIdToSlot.TryGetValue(pieceId, out int slot))
                return BehaviorTreeStatus.Failure;

            DestructiblePoolSlot poolSlot = ctx.Pool.GetSlot(slot);
            if (poolSlot == null || !poolSlot.IsActive)
                return BehaviorTreeStatus.Failure;

            if (!ctx.Bake.TryGetPiece(pieceId, out DestructiblePieceRecord piece))
                return BehaviorTreeStatus.Failure;

            if (!ctx.FallStates.TryGetValue(pieceId, out DestructibleFallContext.PieceFallState state))
                state = new DestructibleFallContext.PieceFallState();

            Vector3 gravity = ctx.GravityDir.sqrMagnitude > 1e-6f ? ctx.GravityDir.normalized : Vector3.down;
            Vector3 worldPos = poolSlot.Transform.position;
            Vector3 rayOrigin = worldPos - gravity * 0.05f;
            float rayLen = piece.groundRayMaxDistance;

            bool hasHit = Physics.Raycast(
                rayOrigin,
                gravity,
                out RaycastHit hit,
                rayLen,
                ctx.GroundMask,
                QueryTriggerInteraction.Ignore);

            Vector3 neighborNormal = ctx.NeighborSeparationNormal(pieceId, worldPos);
            float weightAbove = ctx.WeightAbove(pieceId);
            float weightAccel = weightAbove * 0.05f;

            state.Velocity += gravity * (Physics.gravity.magnitude * Time.deltaTime + weightAccel);
            state.Velocity += neighborNormal * (ctx.NeighborSeparationBias * Time.deltaTime);

            if (state.Velocity.magnitude > ctx.FallSpeedCap)
                state.Velocity = state.Velocity.normalized * ctx.FallSpeedCap;

            Vector3 nextPos = worldPos + state.Velocity * Time.deltaTime;

            if (hasHit)
            {
                float dist = hit.distance;
                if (Mathf.Abs(dist - state.LastGroundDistance) < ctx.GroundSnapDistance &&
                    state.Velocity.magnitude <= ctx.SettleVelocityThreshold)
                {
                    nextPos = hit.point + hit.normal * ctx.GroundSnapDistance;
                    state.Velocity = Vector3.zero;
                    _settled = true;
                    ctx.SettledPieceIds.Add(pieceId);

                    if (piece.normalizedVolume < ctx.RubbleNormalizedVolumeThreshold)
                        ctx.Pool.HandoffToDynamic(slot);
                    else
                        ctx.Pool.SetKinematic(slot, true);
                }
                else if (dist <= ctx.GroundSnapDistance + 0.01f)
                {
                    nextPos = hit.point + hit.normal * ctx.GroundSnapDistance;
                    state.Velocity = Vector3.ProjectOnPlane(state.Velocity, hit.normal) * 0.5f;
                }

                state.LastGroundDistance = dist;
                state.LastGroundPoint = hit.point;
                state.HasGround = true;
            }

            poolSlot.Transform.position = nextPos;
            if (state.Velocity.sqrMagnitude > 1e-4f)
                poolSlot.Transform.rotation = Quaternion.LookRotation(state.Velocity.normalized, -gravity);

            ctx.FallStates[pieceId] = state;
            return _settled ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
        }

        DestructibleFallContext ResolveContext()
        {
            if (playback != null)
                return playback.Context;
            return GetComponentInParent<DestructiblePlaybackController>()?.Context;
        }
    }
}
