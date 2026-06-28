using System.Collections.Generic;
using UnityEngine;
using Weather.Lod;

namespace Weather.CloudBake
{
    public static class CloudHalfShellBuilder
    {
        public static CloudHalfShellStack Build(
            IReadOnlyList<CloudColumnSample> columns,
            WeatherPhysicsManifold manifold,
            float cloudBaseM,
            float cloudTopM,
            int spheresPerColumn = 3,
            CloudViewerSpec viewer = null,
            CloudHalfShellConvexion convexion = null)
        {
            var stack = new CloudHalfShellStack
            {
                cloudBaseM = cloudBaseM,
                cloudTopM = cloudTopM,
                spheresPerColumn = Mathf.Clamp(spheresPerColumn, 1, 8)
            };
            if (columns == null || columns.Count == 0)
                return stack;

            spheresPerColumn = stack.spheresPerColumn;
            float layerHeight = Mathf.Max(1f, (cloudTopM - cloudBaseM) / spheresPerColumn);

            for (int c = 0; c < columns.Count; c++)
            {
                var col = columns[c];
                if (col.targetOpacity < 0.05f)
                    continue;

                Vector3 rayDir = Vector3.up;

                for (int s = 0; s < spheresPerColumn; s++)
                {
                    float t = (s + 0.5f) / spheresPerColumn;
                    float y = cloudBaseM + layerHeight * s + layerHeight * 0.5f;
                    float radius = Mathf.Lerp(8f, 24f, t) * (0.5f + col.targetOpacity);
                    Vector3 center = new Vector3(col.worldHit.x, y, col.worldHit.z);

                    var data = manifold != null
                        ? manifold.GetDataAtPosition(center)
                        : new ManifoldCellData { density = 0.5f, mode = WeatherMode.Cloud };

                    stack.spheres.Add(new CloudSpherePrimitive
                    {
                        center = center,
                        radius = radius,
                        density = Mathf.Max(0.1f, data.density > 0f ? data.density : 0.5f),
                        moisture = 0.5f + col.targetOpacity * 0.3f,
                        waterCoupling = s == 0 ? 0.4f : 0.15f,
                        propertyClass = CloudPropertyClass.Generic,
                        columnIndex = c,
                        stackIndex = s
                    });
                }
            }

            stack.ComputeBounds();
            if (convexion != null && !convexion.IsNeutral)
                CloudHalfShellConvexionUtility.Apply(stack, viewer, convexion);
            return stack;
        }

        public static void PaintIntoManifold(
            CloudHalfShellStack stack,
            WeatherPhysicsManifold manifold,
            Water water,
            float definitionLevel = 0.75f)
        {
            if (stack == null || manifold == null)
                return;

            var regression = new SphericalHyperplaneRegression();
            var samples = new List<ManifoldSample>();
            foreach (var sphere in stack.spheres)
            {
                samples.Add(new ManifoldSample
                {
                    position = sphere.center,
                    data = new ManifoldCellData
                    {
                        velocity = Vector3.zero,
                        temperature = 10f,
                        pressure = 1010f,
                        density = sphere.density,
                        mode = WeatherMode.Cloud
                    }
                });
            }

            regression.FitFromSamples(stack.shellBounds.center, samples, 0.25f, 4);
            regression.PaintIntoManifold(manifold, stack.shellBounds, definitionLevel);

            if (water != null)
            {
                CoupleWaterBelowBase(stack, manifold, water);
            }
        }

        static void CoupleWaterBelowBase(CloudHalfShellStack stack, WeatherPhysicsManifold manifold, Water water)
        {
            float baseY = stack.cloudBaseM;
            foreach (var sphere in stack.spheres)
            {
                if (sphere.stackIndex != 0)
                    continue;
                Vector3 below = sphere.center - Vector3.up * 5f;
                var cell = manifold.GetDataAtPosition(below);
                cell.mode = WeatherMode.Water;
                cell.density = Mathf.Lerp(cell.density, sphere.waterCoupling, 0.5f);
                manifold.SetDataAtPosition(below, cell);
            }
        }
    }
}
