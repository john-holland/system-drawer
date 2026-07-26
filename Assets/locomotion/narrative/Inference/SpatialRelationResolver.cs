using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>Maps layout frames to 3D/4D placement instructions.</summary>
    public static class SpatialRelationResolver
    {
        public struct ResolveContext
        {
            public Vector3 defaultCenter;
            public Vector3 defaultSize;
            public float tStart;
            public float tEnd;
            public Vector3? causalityPosition;
            public Vector3? playerPosition;
            public int randomSeed;
        }

        public static List<LayoutPlacementInstruction> ResolveTree(LayoutPlacementFrame root, ResolveContext ctx)
        {
            var list = new List<LayoutPlacementInstruction>();
            if (root == null)
                return list;
            ResolveRecursive(root, ctx, list);
            return list;
        }

        static void ResolveRecursive(LayoutPlacementFrame frame, ResolveContext ctx, List<LayoutPlacementInstruction> outList)
        {
            if (frame == null)
                return;

            foreach (var entity in frame.entities)
            {
                if (string.IsNullOrWhiteSpace(entity))
                    continue;
                outList.Add(ResolveFrame(frame, entity, ctx));
            }

            foreach (var child in frame.children)
                ResolveRecursive(child, ctx, outList);
        }

        static LayoutPlacementInstruction ResolveFrame(LayoutPlacementFrame frame, string entity, ResolveContext ctx)
        {
            var inst = new LayoutPlacementInstruction
            {
                entities = new List<string> { entity },
                anchorKey = frame.anchor,
                relationName = frame.relation.ToString(),
                causalityLeafId = frame.causalityLeafId,
                useRandomSlot = !frame.HasSpatialRelation,
                requiresPathSolve = IsRoadEntity(entity) && (frame.relation == LayoutSpatialRelation.Through || frame.relation == LayoutSpatialRelation.None || frame.relation == LayoutSpatialRelation.With)
            };

            Vector3 anchorCenter = ResolveAnchorCenter(frame, ctx);
            Vector3 anchorSize = ctx.defaultSize.sqrMagnitude > 0 ? ctx.defaultSize : Vector3.one * 10f;

            ApplyRelationOffset(frame.relation, ref anchorCenter, anchorSize);
            inst.anchorCenter = anchorCenter;
            inst.anchorSize = anchorSize;
            inst.bounds4 = new LayoutPlacementInstruction.Bounds4VolumeHint
            {
                center = anchorCenter,
                size = anchorSize,
                tMin = ctx.tStart,
                tMax = ctx.tEnd
            };

            inst.startWorld = anchorCenter;
            inst.goalWorld = anchorCenter + Vector3.forward * Mathf.Max(anchorSize.z, 10f);
            if (frame.relation == LayoutSpatialRelation.Through && ctx.causalityPosition.HasValue)
                inst.goalWorld = ctx.causalityPosition.Value;
            if (ctx.playerPosition.HasValue && frame.relation == LayoutSpatialRelation.Through)
                inst.startWorld = ctx.playerPosition.Value;

            // "roads … there" may bind causality anchor without an explicit Through relation token.
            if (IsRoadEntity(entity)
                && WithLemmaRegistry.IsCausalityDeictic(frame.anchor)
                && ctx.causalityPosition.HasValue)
            {
                inst.requiresPathSolve = true;
                inst.goalWorld = ctx.causalityPosition.Value;
                if (ctx.playerPosition.HasValue)
                    inst.startWorld = ctx.playerPosition.Value;
            }

            return inst;
        }

        static Vector3 ResolveAnchorCenter(LayoutPlacementFrame frame, ResolveContext ctx)
        {
            if (WithLemmaRegistry.IsPlayerDeictic(frame.anchor) && ctx.playerPosition.HasValue)
                return ctx.playerPosition.Value;
            if (WithLemmaRegistry.IsCausalityDeictic(frame.anchor) && ctx.causalityPosition.HasValue)
                return ctx.causalityPosition.Value;
            if (ctx.causalityPosition.HasValue && frame.relation == LayoutSpatialRelation.Through)
                return ctx.causalityPosition.Value;
            return ctx.defaultCenter;
        }

        static void ApplyRelationOffset(LayoutSpatialRelation relation, ref Vector3 center, Vector3 size)
        {
            switch (relation)
            {
                case LayoutSpatialRelation.LeftOf:
                    center.x -= size.x;
                    break;
                case LayoutSpatialRelation.RightOf:
                    center.x += size.x;
                    break;
                case LayoutSpatialRelation.ForwardOf:
                    center.z += size.z;
                    break;
                case LayoutSpatialRelation.Behind:
                    center.z -= size.z;
                    break;
                case LayoutSpatialRelation.Near:
                    center += new Vector3(size.x * 0.25f, 0f, size.z * 0.25f);
                    break;
                case LayoutSpatialRelation.Above:
                    center.y += size.y;
                    break;
                case LayoutSpatialRelation.Below:
                    center.y -= size.y;
                    break;
                case LayoutSpatialRelation.Far:
                    center += new Vector3(size.x * 0.75f, 0f, size.z * 0.75f);
                    break;
                case LayoutSpatialRelation.Side:
                    center.x += size.x * 0.5f;
                    break;
            }
        }

        static bool IsRoadEntity(string entity)
        {
            if (string.IsNullOrWhiteSpace(entity))
                return false;
            string e = entity.ToLowerInvariant();
            return e.Contains("road") || e.Contains("street") || e.Contains("path") || e.Contains("highway");
        }

        public static LayoutSlotHint ToSlotHint(LayoutSpatialRelation relation)
        {
            var hint = new LayoutSlotHint { useRandomSlot = relation == LayoutSpatialRelation.None || relation == LayoutSpatialRelation.With };
            switch (relation)
            {
                case LayoutSpatialRelation.LeftOf:
                    hint.fitX = LayoutFitAxis.Left;
                    hint.placementModeName = "Left";
                    break;
                case LayoutSpatialRelation.RightOf:
                    hint.fitX = LayoutFitAxis.Right;
                    hint.placementModeName = "Right";
                    break;
                case LayoutSpatialRelation.ForwardOf:
                    hint.fitZ = LayoutFitAxis.Forward;
                    hint.placementModeName = "Forward";
                    break;
                case LayoutSpatialRelation.Behind:
                    hint.fitZ = LayoutFitAxis.Backward;
                    hint.placementModeName = "Down";
                    break;
                default:
                    hint.fitX = LayoutFitAxis.Center;
                    hint.fitY = LayoutFitAxis.Center;
                    hint.fitZ = LayoutFitAxis.Center;
                    hint.placementModeName = "In";
                    break;
            }
            return hint;
        }
    }
}
