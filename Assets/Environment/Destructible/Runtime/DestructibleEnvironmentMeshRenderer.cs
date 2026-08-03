using System.Collections.Generic;
using UnityEngine;
using Weather;

namespace DestructibleEnvironment
{
    public class DestructibleFallContext
    {
        public DestructibleRigidbodyPool Pool;
        public DestructibleBakeAsset Bake;
        public Vector3 GravityDir = Vector3.down;
        public float FallSpeedCap = 12f;
        public float GroundSnapDistance = 0.05f;
        public float SettleVelocityThreshold = 0.15f;
        public float NeighborSeparationBias = 0.35f;
        public float RubbleNormalizedVolumeThreshold = 0.08f;
        public int GroundMask = ~0;

        public readonly Dictionary<int, int> PieceIdToSlot = new Dictionary<int, int>();
        public readonly HashSet<int> SettledPieceIds = new HashSet<int>();
        public readonly Dictionary<int, PieceFallState> FallStates = new Dictionary<int, PieceFallState>();

        public struct PieceFallState
        {
            public Vector3 Velocity;
            public Vector3 LastGroundPoint;
            public float LastGroundDistance;
            public bool HasGround;
        }

        public bool AreSupportsSettled(int pieceId)
        {
            if (Bake == null || !Bake.TryGetPiece(pieceId, out DestructiblePieceRecord piece))
                return true;

            int[] supports = piece.supportPieceIds;
            if (supports == null)
                return true;

            for (int i = 0; i < supports.Length; i++)
            {
                if (!SettledPieceIds.Contains(supports[i]))
                    return false;
            }

            return true;
        }

        public float WeightAbove(int pieceId)
        {
            if (Bake == null)
                return 0f;

            float mass = 0f;
            for (int i = 0; i < Bake.pieces.Count; i++)
            {
                DestructiblePieceRecord p = Bake.pieces[i];
                if (p.pieceId == pieceId)
                    continue;
                if (SettledPieceIds.Contains(p.pieceId))
                    continue;

                int[] supports = p.supportPieceIds;
                if (supports == null)
                    continue;

                for (int s = 0; s < supports.Length; s++)
                {
                    if (supports[s] == pieceId)
                    {
                        mass += p.massEstimate;
                        break;
                    }
                }
            }

            return mass;
        }

        public Vector3 NeighborSeparationNormal(int pieceId, Vector3 worldPos)
        {
            if (Bake == null || !Bake.TryGetPiece(pieceId, out DestructiblePieceRecord piece))
                return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int[] neighbors = piece.neighborPieceIds;
            if (neighbors == null)
                return sum;

            for (int i = 0; i < neighbors.Length; i++)
            {
                if (SettledPieceIds.Contains(neighbors[i]))
                    continue;
                if (!Bake.TryGetPiece(neighbors[i], out DestructiblePieceRecord n))
                    continue;
                Vector3 away = worldPos - n.localCentroid;
                if (away.sqrMagnitude < 1e-6f)
                    continue;
                sum += away.normalized;
            }

            return sum.sqrMagnitude > 1e-6f ? sum.normalized : Vector3.zero;
        }
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("Environment/Destructible Environment Mesh Renderer")]
    public class DestructibleEnvironmentMeshRenderer : MonoBehaviour
    {
        [Header("Sources")]
        public MeshFilter[] sourceMeshFilters;
        public Renderer[] sourceRenderers;
        public Collider[] sourceColliders;
        public bool autoDiscoverChildren = true;

        [Header("Bake")]
        public DestructibleBakeAsset bake;
        public DestructibleMaterialProfile materialProfile;

        [Header("Environment")]
        public WeatherPhysicsManifold manifold;

        [Header("Break")]
        public AnimationCurve pieceRetentionCurve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 3f);
        public float gravityBias = 0.35f;
        public float impactFalloffM = 2f;
        public Vector3 gravityDir = Vector3.down;

        [Header("Activation")]
        public bool activateOnCollision = true;
        public float minImpulseN = 50f;

        [Header("Fall")]
        public float fallSpeedCap = 12f;
        public float groundSnapDistance = 0.05f;
        public float settleVelocityThreshold = 0.15f;
        public float neighborSeparationBias = 0.35f;
        public float rubbleNormalizedVolumeThreshold = 0.08f;
        public LayerMask groundMask = ~0;

        [Header("Runtime")]
        public Transform debrisRoot;
        public DestructiblePlaybackController playbackController;

        DestructibleRigidbodyPool _pool;
        bool _activated;
        readonly List<int> _activePieceIds = new List<int>();

        public bool IsActivated => _activated;
        public IReadOnlyList<int> ActivePieceIds => _activePieceIds;
        public DestructibleFallContext FallContext { get; private set; }
        public DestructibleRigidbodyPool Pool => _pool;

        void Awake()
        {
            if (autoDiscoverChildren)
                DiscoverSources();

            EnsureDebrisRoot();
            EnsurePool();
            EnsurePlayback();
        }

        void DiscoverSources()
        {
            if (sourceMeshFilters == null || sourceMeshFilters.Length == 0)
                sourceMeshFilters = GetComponentsInChildren<MeshFilter>(true);
            if (sourceRenderers == null || sourceRenderers.Length == 0)
                sourceRenderers = GetComponentsInChildren<Renderer>(true);
            if (sourceColliders == null || sourceColliders.Length == 0)
                sourceColliders = GetComponentsInChildren<Collider>(true);
        }

        void EnsureDebrisRoot()
        {
            if (debrisRoot != null)
                return;
            var go = new GameObject("DestructibleDebris");
            go.transform.SetParent(transform, false);
            debrisRoot = go.transform;
        }

        void EnsurePool()
        {
            if (_pool != null)
                return;

            int slots = bake != null && bake.poolSlotCount > 0 ? bake.poolSlotCount : 16;
            PhysicsMaterial pm = null;
            if (sourceColliders != null && sourceColliders.Length > 0 && sourceColliders[0] != null)
                pm = sourceColliders[0].sharedMaterial;
            _pool = new DestructibleRigidbodyPool(debrisRoot, slots, pm);
        }

        void EnsurePlayback()
        {
            if (playbackController != null)
                return;
            playbackController = GetComponentInChildren<DestructiblePlaybackController>(true);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!activateOnCollision || _activated)
                return;
            if (collision.impulse.magnitude < minImpulseN)
                return;
            Vector3 hitPt = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            ImpulseMaterialMemory.NotifyImpulse(gameObject, collision.impulse.magnitude, hitPt);
            Activate(DestructibleImpactContext.FromCollision(collision, gravityDir));
        }

        public void Activate(in DestructibleImpactContext ctx)
        {
            if (_activated || bake == null || bake.pieces == null || bake.pieces.Count == 0)
                return;

            ImpulseMaterialMemory.NotifyImpulse(gameObject, ctx.impulseN, ctx.worldPoint);
            _activated = true;
            DiscoverSources();

            HashSet<int> detached = DestructibleBreakEvaluator.EvaluateDetachedPieces(
                bake,
                ctx,
                materialProfile,
                manifold,
                pieceRetentionCurve,
                gravityBias,
                impactFalloffM,
                transform.localToWorldMatrix);

            if (detached.Count == 0)
            {
                _activated = false;
                return;
            }

            DisableSources();
            EnsurePool();
            _pool.DeactivateAll();
            _activePieceIds.Clear();

            FallContext = BuildFallContext();
            Material[] sharedMats = ResolveSharedMaterials();

            int slot = 0;
            foreach (int pieceId in bake.fallOrder)
            {
                if (!detached.Contains(pieceId))
                    continue;
                if (!bake.TryGetPiece(pieceId, out DestructiblePieceRecord piece))
                    continue;
                if (slot >= _pool.SlotCount)
                    break;

                Vector3 worldPos = transform.TransformPoint(piece.localCentroid);
                var pose = new Pose(worldPos, transform.rotation);
                if (_pool.AssignPiece(slot, piece.pieceMesh, sharedMats, pose, pieceId))
                {
                    FallContext.PieceIdToSlot[pieceId] = slot;
                    _activePieceIds.Add(pieceId);
                    slot++;
                }
            }

            EnsurePlayback();
            if (playbackController != null)
            {
                playbackController.Bind(this, FallContext, _activePieceIds);
                if (playbackController.behaviorTree != null)
                {
                    playbackController.behaviorTree.currentNode = playbackController.behaviorTree.rootNode;
                    playbackController.behaviorTree.decisionTime = 0f;
                }
                playbackController.enabled = true;
                playbackController.BeginPlayback();
            }
        }

        DestructibleFallContext BuildFallContext()
        {
            return new DestructibleFallContext
            {
                Pool = _pool,
                Bake = bake,
                GravityDir = gravityDir.sqrMagnitude > 1e-6f ? gravityDir.normalized : Vector3.down,
                FallSpeedCap = fallSpeedCap,
                GroundSnapDistance = groundSnapDistance,
                SettleVelocityThreshold = settleVelocityThreshold,
                NeighborSeparationBias = neighborSeparationBias,
                RubbleNormalizedVolumeThreshold = rubbleNormalizedVolumeThreshold,
                GroundMask = groundMask
            };
        }

        Material[] ResolveSharedMaterials()
        {
            if (sourceRenderers != null)
            {
                for (int i = 0; i < sourceRenderers.Length; i++)
                {
                    if (sourceRenderers[i] != null)
                        return sourceRenderers[i].sharedMaterials;
                }
            }
            return new[] { new Material(Shader.Find("Standard")) };
        }

        void DisableSources()
        {
            if (sourceRenderers != null)
            {
                for (int i = 0; i < sourceRenderers.Length; i++)
                {
                    if (sourceRenderers[i] != null)
                        sourceRenderers[i].enabled = false;
                }
            }

            if (sourceColliders != null)
            {
                for (int i = 0; i < sourceColliders.Length; i++)
                {
                    if (sourceColliders[i] != null)
                        sourceColliders[i].enabled = false;
                }
            }
        }

#if UNITY_EDITOR
        public void EditorPreBake(DestructibleBakeAsset targetAsset)
        {
            DiscoverSources();
            if (targetAsset == null)
                return;

            var sources = new List<DestructiblePreBakePipeline.SourceMeshEntry>();
            if (sourceMeshFilters != null)
            {
                for (int i = 0; i < sourceMeshFilters.Length; i++)
                {
                    MeshFilter mf = sourceMeshFilters[i];
                    if (mf == null || mf.sharedMesh == null)
                        continue;
                    sources.Add(new DestructiblePreBakePipeline.SourceMeshEntry
                    {
                        mesh = mf.sharedMesh,
                        localToWorld = mf.transform.localToWorldMatrix,
                        worldToLocal = transform.worldToLocalMatrix
                    });
                }
            }

            DestructiblePreBakePipeline.PopulateBakeAsset(
                targetAsset,
                sources,
                materialProfile,
                gravityDir,
                targetAsset.maxDepth,
                targetAsset.minLeafExtent,
                targetAsset.maxTrianglesPerLeaf,
                targetAsset.minRubbleVolume);

            targetAsset.sourceLossyScale = transform.lossyScale;
            bake = targetAsset;
            EnsurePool();
        }
#endif
    }
}
