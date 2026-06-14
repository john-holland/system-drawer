using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    public enum LayoutSpatialRelation
    {
        None, With, LeftOf, RightOf, ForwardOf, Behind, Through, Near, Along, Inside, Outside
    }

    public enum LayoutFitAxis
    {
        Center, Left, Right, Down, Up, Backward, Forward
    }

    [Serializable]
    public struct LayoutSlotHint
    {
        public LayoutFitAxis fitX;
        public LayoutFitAxis fitY;
        public LayoutFitAxis fitZ;
        public string placementModeName;
        public bool useRandomSlot;
    }

    [Serializable]
    public class LayoutPlacementFrame
    {
        public LayoutSpatialRelation relation = LayoutSpatialRelation.None;
        public List<string> entities = new List<string>();
        public string anchor;
        public string causalityLeafId;
        public List<LayoutPlacementFrame> children = new List<LayoutPlacementFrame>();
        public bool HasSpatialRelation => relation != LayoutSpatialRelation.None && relation != LayoutSpatialRelation.With;
    }

    [Serializable]
    public struct LayoutPlacementInstruction
    {
        public string relationName;
        public List<string> entities;
        public string anchorKey;
        public Vector3 anchorCenter;
        public Vector3 anchorSize;
        public Vector3 startWorld;
        public Vector3 goalWorld;
        public Bounds4VolumeHint bounds4;
        public bool useRandomSlot;
        public bool requiresPathSolve;
        public string causalityLeafId;

        public struct Bounds4VolumeHint
        {
            public Vector3 center;
            public Vector3 size;
            public float tMin;
            public float tMax;

            public Bounds4 ToBounds4() => new Bounds4(center, size, tMin, tMax);

            public Bounds4AxisAlignedVolume ToVolume() => new Bounds4AxisAlignedVolume(ToBounds4());
        }
    }

    public interface IRoadLayoutPlacer
    {
        bool TryPlaceRoad(LayoutPlacementInstruction instruction, out string roadSegmentId);
    }

    public static class LayoutPlacementBroadcast
    {
        public static event Action<object> RootParsed;
        public static void NotifyRootParsed(object layoutRoot) => RootParsed?.Invoke(layoutRoot);
    }
}
