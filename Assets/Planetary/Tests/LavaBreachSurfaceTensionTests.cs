using NUnit.Framework;
using Planetary.Voxel;
using UnityEngine;

namespace Planetary.Tests
{
    public class LavaBreachSurfaceTensionTests
    {
        [Test]
        public void HighSurfaceTension_RaisesBreachThreshold()
        {
            var map = new LoopEdgeMap();
            map.AddEdge(0, 1, LoopEdgePermeability.Permeable);
            Assert.IsFalse(map.TryDetectBreach(0, 1, 1f, 0f));
            Assert.IsTrue(map.TryDetectBreach(0, 1, 100f, 1f));
        }
    }
}
