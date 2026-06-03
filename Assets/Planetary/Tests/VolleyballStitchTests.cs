using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Planetary.Tests
{
    public class VolleyballStitchTests
    {
        [Test]
        public void WeldCorners_ReducesDuplicatePositions()
        {
            var verts = new List<Vector3>[6];
            var tris = new List<int>[6];
            for (int f = 0; f < 6; f++)
            {
                verts[f] = new List<Vector3> { Vector3.zero, Vector3.one };
                tris[f] = new List<int>();
            }
            VolleyballCornerStitcher.WeldCorners(verts, tris);
            Assert.AreEqual(verts[0][0], verts[1][0]);
        }

        [Test]
        public void SphericalRoundTrip_IsClose()
        {
            var sc = new SphericalCoordinates(45f, 90f, 100f);
            Vector3 w = sc.ToWorldPosition(Vector3.zero, Vector3.up, 0f);
            var back = SphericalCoordinates.FromWorldPosition(w, Vector3.zero, Vector3.up, 0f);
            Assert.AreEqual(45f, back.LatitudeDeg, 1f);
            Assert.AreEqual(90f, back.LongitudeDeg, 1f);
        }
    }
}
