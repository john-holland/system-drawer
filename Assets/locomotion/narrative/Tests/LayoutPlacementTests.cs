using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Narrative.Tests
{
    public class LayoutPlacementTests
    {
        [Test]
        public void ParseWithRoadsBuildings_TwoEntitiesWithRelation()
        {
            Assert.IsTrue(WithLayoutExprParser.TryParse("(with roads buildings)", out var root));
            Assert.IsNotNull(root);
            Assert.AreEqual(LayoutSpatialRelation.With, root.relation);
            Assert.GreaterOrEqual(root.entities.Count, 2);
        }

        [Test]
        public void ParseEnglishWorldWithRoadsAndBuildings()
        {
            Assert.IsTrue(WithLayoutExprParser.TryParse("a world with roads, and buildings", out var root));
            Assert.IsNotNull(root);
            Assert.GreaterOrEqual(root.children.Count, 1);
        }

        [Test]
        public void ParseLeftOfTownHall_ResolvesLeftOffset()
        {
            Assert.IsTrue(WithLayoutExprParser.TryParse("(roads (left-of town-hall))", out var root));
            var ctx = new SpatialRelationResolver.ResolveContext
            {
                defaultCenter = Vector3.zero,
                defaultSize = Vector3.one * 10f
            };
            var instructions = SpatialRelationResolver.ResolveTree(root, ctx);
            bool foundLeft = false;
            foreach (var inst in instructions)
            {
                if (inst.relationName == "LeftOf" && inst.anchorCenter.x < 0f)
                    foundLeft = true;
            }
            Assert.IsTrue(foundLeft);
        }

        [Test]
        public void ThroughThere_UsesCausalityPosition()
        {
            Assert.IsTrue(WithLayoutExprParser.TryParse("roads through there", out var root));
            var causality = new Vector3(5f, 0f, 12f);
            var ctx = new SpatialRelationResolver.ResolveContext
            {
                defaultCenter = Vector3.zero,
                defaultSize = Vector3.one * 10f,
                causalityPosition = causality
            };
            var instructions = SpatialRelationResolver.ResolveTree(root, ctx);
            bool found = false;
            foreach (var inst in instructions)
            {
                if (inst.requiresPathSolve && Vector3.Distance(inst.goalWorld, causality) < 0.01f)
                    found = true;
            }
            Assert.IsTrue(found);
        }

        [Test]
        public void ParseHereHere_ReducesToCompoundDeicticKey()
        {
            Assert.IsTrue(WithLayoutExprParser.TryParse("roads here here", out var root));
            Assert.AreEqual(1, root.entities.Count);
            Assert.AreEqual("roads", root.entities[0]);
            Assert.AreEqual("here-here", root.anchor);
            Assert.AreEqual(0, root.children.Count);
        }

        [Test]
        public void ParseHereThere_SpawnsCoequalAnchorChildren()
        {
            Assert.IsTrue(WithLayoutExprParser.TryParse("a world with roads here there", out var root));
            Assert.AreEqual(1, root.children.Count);
            var clause = root.children[0];
            Assert.AreEqual("roads", clause.entities[0]);
            Assert.AreEqual("there", clause.anchor);
            Assert.AreEqual(1, clause.children.Count);
            Assert.AreEqual("here", clause.children[0].anchor);
            Assert.AreEqual("roads", clause.children[0].entities[0]);
        }

        [Test]
        public void ParseHereThere_ResolvesDistinctAnchorCenters()
        {
            Assert.IsTrue(WithLayoutExprParser.TryParse("roads here there", out var root));
            var player = new Vector3(1f, 0f, 2f);
            var causality = new Vector3(9f, 0f, 8f);
            var ctx = new SpatialRelationResolver.ResolveContext
            {
                defaultCenter = Vector3.zero,
                defaultSize = Vector3.one * 10f,
                playerPosition = player,
                causalityPosition = causality
            };
            var instructions = SpatialRelationResolver.ResolveTree(root, ctx);
            bool foundHere = false;
            bool foundThere = false;
            foreach (var inst in instructions)
            {
                if (inst.anchorKey == "here" && Vector3.Distance(inst.anchorCenter, player) < 0.01f)
                    foundHere = true;
                if (inst.anchorKey == "there" && Vector3.Distance(inst.anchorCenter, causality) < 0.01f)
                    foundThere = true;
            }
            Assert.IsTrue(foundHere);
            Assert.IsTrue(foundThere);
        }

        [Test]
        public void PathReplacementGate_BlocksUntilCausalityDepth()
        {
            PathReplacementGate.Unlock();
            PathReplacementGate.LockUntilCausalityDepth(2);
            Assert.IsTrue(PathReplacementGate.IsLocked);
            Assert.IsFalse(PathReplacementGate.CanReplacePath());
            PathReplacementGate.SetCausalityDepth(2);
            Assert.IsFalse(PathReplacementGate.IsLocked);
        }

        [Test]
        public void RandomPlacementPolicy_ReturnsDistinctIndices()
        {
            var node = new GameObject("n").AddComponent<SGBehaviorTreeNode>();
            node.placeSearchMode = SGBehaviorTreeNode.PlaceSearchMode.Random;
            int a = LayoutPlacementPolicy.ResolvePlacementIndex(node, null, 0, 42);
            int b = LayoutPlacementPolicy.ResolvePlacementIndex(node, null, 1, 42);
            Assert.AreNotEqual(a, b);
            Object.DestroyImmediate(node.gameObject);
        }
    }
}
