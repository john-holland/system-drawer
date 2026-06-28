using NUnit.Framework;
using Planetary.Composition;
using SdfMax;
using UnityEngine;

namespace Planetary.Tests
{
    public class PlanetarySurfaceFrameTests
    {
        const float Radius = 1000f;
        static readonly Vector3 Center = Vector3.zero;
        static readonly Vector3 Pole = Vector3.up;

        [Test]
        public void PlanePlanetFace_NormalsFaceAwayFromCenter()
        {
            var chunks = PlanetMeshBuilder.BuildChunks(
                Radius,
                8,
                1,
                (_, __) => 0f,
                Center,
                Pole);

            Assert.Greater(chunks.Length, 0);
            foreach (var chunk in chunks)
            {
                var mesh = chunk.Mesh;
                var verts = mesh.vertices;
                var normals = mesh.normals;
                Assert.AreEqual(verts.Length, normals.Length);
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 expected = PlanetSurfaceFrame.OutwardNormal(verts[i], Center);
                    float dot = Vector3.Dot(normals[i].normalized, expected);
                    Assert.GreaterOrEqual(dot, 0.99f,
                        $"Vertex {i} on face {chunk.Face} normal should face away from planet center (dot={dot})");
                }
            }

            foreach (var chunk in chunks)
                Object.DestroyImmediate(chunk.Mesh);
        }

        [Test]
        public void PlanePlanetFace_SphericalUv_IncreasesWithLatitudeOnMeridian()
        {
            var chunks = PlanetMeshBuilder.BuildChunks(
                Radius,
                8,
                1,
                (_, __) => 0f,
                Center,
                Pole);

            float minV = 1f;
            float maxV = 0f;
            foreach (var chunk in chunks)
            {
                foreach (Vector2 uv in chunk.Mesh.uv)
                {
                    minV = Mathf.Min(minV, uv.y);
                    maxV = Mathf.Max(maxV, uv.y);
                }
                Object.DestroyImmediate(chunk.Mesh);
            }

            Assert.Less(minV, 0.2f, "Expected low-latitude UV coverage near south pole");
            Assert.Greater(maxV, 0.8f, "Expected high-latitude UV coverage near north pole");
        }

        [Test]
        public void ChangeOfBasis_UpAxisAlignsWithRadialOnEquator()
        {
            var sc = new SphericalCoordinates(0f, 0f, Radius);
            Vector3 surface = sc.ToWorldPosition(Center, Pole, 0f);
            Matrix4x4 basis = PlanetSurfaceFrame.ChangeOfBasisFromSpherical(surface, Center, Pole, 0f);

            Assert.IsTrue(PlanetSurfaceFrame.BasisUpFacesAwayFromCenter(basis, Center));
        }

        [Test]
        public void ChangeOfBasis_UpAxisAlignsWithRadialAtMultipleLatitudes()
        {
            float[] lats = { -60f, -30f, 0f, 30f, 60f };
            foreach (float lat in lats)
            {
                var sc = new SphericalCoordinates(lat, 45f, Radius);
                Vector3 surface = sc.ToWorldPosition(Center, Pole, 0f);
                Matrix4x4 basis = PlanetSurfaceFrame.ChangeOfBasisFromSpherical(surface, Center, Pole, 0f);
                Assert.IsTrue(
                    PlanetSurfaceFrame.BasisUpFacesAwayFromCenter(basis, Center),
                    $"Basis up should face outward at lat={lat}");
            }
        }

        [Test]
        public void WorldToSphericalUv_RoundTripsCardinalPoints()
        {
            Assert.That(PlanetSurfaceFrame.WorldToSphericalUv(
                new Vector3(0f, Radius, 0f), Center, Pole, 0f).y, Is.EqualTo(1f).Within(0.02f));
            Assert.That(PlanetSurfaceFrame.WorldToSphericalUv(
                new Vector3(0f, -Radius, 0f), Center, Pole, 0f).y, Is.EqualTo(0f).Within(0.02f));
        }
    }
}
