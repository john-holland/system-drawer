using System.Collections.Generic;
using DestructibleEnvironment;
using NUnit.Framework;
using UnityEngine;

namespace DestructibleEnvironment.Tests
{
    public class DestructibleConvexTreeBuilderTests
    {
        [Test]
        public void BuildFromMesh_BoxProducesMultipleLeaves()
        {
            Mesh mesh = CreateBoxMesh(2f);
            var result = DestructibleConvexTreeBuilder.BuildFromMesh(
                mesh,
                Matrix4x4.identity,
                maxDepth: 4,
                minLeafExtent: 0.25f,
                maxTrianglesPerLeaf: 8);

            Assert.Greater(result.Leaves.Count, 1);
            Assert.Greater(result.RootVolume, 0f);
        }

        [Test]
        public void ExtractSubmesh_PreservesTriangleCount()
        {
            Mesh mesh = CreateBoxMesh(1f);
            var tree = DestructibleConvexTreeBuilder.BuildFromMesh(mesh, Matrix4x4.identity, maxDepth: 2, minLeafExtent: 0.5f, maxTrianglesPerLeaf: 4);
            Assert.Greater(tree.Leaves.Count, 0);

            Mesh piece = DestructibleMeshExtractor.ExtractSubmesh(
                mesh,
                Matrix4x4.identity,
                Matrix4x4.identity,
                tree.Leaves[0].TriangleIndices,
                "piece0");

            Assert.NotNull(piece);
            Assert.Greater(piece.triangles.Length, 0);
        }

        public static Mesh CreateBoxMesh(float size)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh mesh = Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);
            Object.DestroyImmediate(cube);
            mesh.name = "TestBox";
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
                verts[i] *= size;
            mesh.vertices = verts;
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    public class DestructibleBreakEvaluatorTests
    {
        [Test]
        public void BreakEvaluator_LargePieceSurvivesHighRetention()
        {
            var bake = ScriptableObject.CreateInstance<DestructibleBakeAsset>();
            bake.rootVolume = 10f;
            bake.pieces = new List<DestructiblePieceRecord>
            {
                new DestructiblePieceRecord
                {
                    pieceId = 0,
                    normalizedVolume = 0.05f,
                    localCentroid = Vector3.zero
                },
                new DestructiblePieceRecord
                {
                    pieceId = 1,
                    normalizedVolume = 0.9f,
                    localCentroid = Vector3.one * 0.1f
                }
            };

            var retention = AnimationCurve.Linear(0f, 0.5f, 1f, 5f);
            var impact = new DestructibleImpactContext
            {
                worldPoint = Vector3.zero,
                impulseN = 500f,
                impulseDir = Vector3.forward,
                gravityDir = Vector3.down
            };

            HashSet<int> detached = DestructibleBreakEvaluator.EvaluateDetachedPieces(
                bake,
                impact,
                null,
                null,
                retention,
                gravityBias: 0.35f,
                impactFalloffM: 2f,
                Matrix4x4.identity);

            Assert.IsTrue(detached.Contains(0));
            Assert.IsFalse(detached.Contains(1));
            Object.DestroyImmediate(bake);
        }
    }

    public class DestructiblePieceGraphTests
    {
        [Test]
        public void FallOrder_SupportPieceComesAfterSupported()
        {
            var pieces = new List<DestructiblePieceRecord>
            {
                new DestructiblePieceRecord
                {
                    pieceId = 0,
                    localCentroid = new Vector3(0f, 0f, 0f),
                    localBounds = new Bounds(Vector3.zero, Vector3.one),
                    supportPieceIds = new[] { 1 }
                },
                new DestructiblePieceRecord
                {
                    pieceId = 1,
                    localCentroid = new Vector3(0f, 1f, 0f),
                    localBounds = new Bounds(new Vector3(0f, 1f, 0f), Vector3.one),
                    supportPieceIds = System.Array.Empty<int>()
                }
            };

            int[] order = DestructiblePieceGraph.ComputeFallOrder(pieces);
            int idx0 = System.Array.IndexOf(order, 0);
            int idx1 = System.Array.IndexOf(order, 1);
            Assert.Less(idx1, idx0);
        }
    }

    public class DestructibleRigidbodyPoolTests
    {
        [Test]
        public void RubbleHandoff_SmallPieceBecomesDynamic()
        {
            var root = new GameObject("pool_root");
            var pool = new DestructibleRigidbodyPool(root.transform, 1, null);
            var mesh = DestructibleConvexTreeBuilderTests.CreateBoxMesh(0.2f);

            Assert.IsTrue(pool.AssignPiece(0, mesh, null, new Pose(Vector3.zero, Quaternion.identity), 0));
            pool.HandoffToDynamic(0);
            Assert.IsFalse(pool.GetSlot(0).Rigidbody.isKinematic);
            Assert.IsTrue(pool.GetSlot(0).Rigidbody.useGravity);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void LargePiece_StaysKinematicAfterSettle()
        {
            var root = new GameObject("pool_root");
            var pool = new DestructibleRigidbodyPool(root.transform, 1, null);
            var mesh = DestructibleConvexTreeBuilderTests.CreateBoxMesh(2f);

            pool.AssignPiece(0, mesh, null, new Pose(Vector3.zero, Quaternion.identity), 0);
            pool.SetKinematic(0, true);
            Assert.IsTrue(pool.GetSlot(0).Rigidbody.isKinematic);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(mesh);
        }
    }

    public class DestructibleActivationTests
    {
        [Test]
        public void Activation_DisablesSourceRenderers()
        {
            var root = new GameObject("destructible");
            var destructible = root.AddComponent<DestructibleEnvironmentMeshRenderer>();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(root.transform, false);
            destructible.sourceRenderers = new[] { visual.GetComponent<Renderer>() };
            destructible.sourceColliders = new[] { visual.GetComponent<Collider>() };
            destructible.autoDiscoverChildren = false;

            var bake = ScriptableObject.CreateInstance<DestructibleBakeAsset>();
            var mesh = DestructibleConvexTreeBuilderTests.CreateBoxMesh(1f);
            bake.pieces = new List<DestructiblePieceRecord>
            {
                new DestructiblePieceRecord
                {
                    pieceId = 0,
                    normalizedVolume = 0.01f,
                    localCentroid = Vector3.zero,
                    pieceMesh = mesh,
                    localBounds = mesh.bounds,
                    groundRayMaxDistance = 5f
                }
            };
            bake.fallOrder = new[] { 0 };
            bake.poolSlotCount = 1;
            destructible.bake = bake;
            destructible.pieceRetentionCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 0.1f);
            destructible.minImpulseN = 1f;

            var impact = new DestructibleImpactContext
            {
                worldPoint = Vector3.zero,
                impulseN = 1000f,
                impulseDir = Vector3.forward,
                gravityDir = Vector3.down
            };

            destructible.Activate(impact);
            Assert.IsTrue(destructible.IsActivated);
            Assert.IsFalse(visual.GetComponent<Renderer>().enabled);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(bake);
            Object.DestroyImmediate(mesh);
        }
    }
}
