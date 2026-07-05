using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Open.Topology
{
    /// <summary>Detected concave / enclosed volume with openings.</summary>
    [System.Serializable]
    public sealed class EnclosedVolume
    {
        public Bounds bounds;
        public Vector3 center;
        public float volume;
        public List<OpeningLoop> openings = new List<OpeningLoop>();
        public bool hasVerticalEntrance;
        public float lowestPoint;
        public float highestOpening;
    }
}
