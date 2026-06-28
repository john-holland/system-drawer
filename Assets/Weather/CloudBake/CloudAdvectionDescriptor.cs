using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weather.CloudBake
{
    public enum CloudAdvectionMode
    {
        AnchorReset,
        WeatherSolverAdvection
    }

    [Serializable]
    public sealed class CloudBakeAnchor
    {
        public List<Vector3> sphereCentersWorld = new List<Vector3>();
        public List<ManifoldAnchorCell> manifoldCells = new List<ManifoldAnchorCell>();
        public Bounds shellBounds;
        public int frameIndex;
        public float narrativeTime;

        public static CloudBakeAnchor Capture(
            CloudHalfShellStack stack,
            WeatherPhysicsManifold manifold = null,
            int frameIndex = 0,
            float narrativeTime = 0f)
        {
            var anchor = new CloudBakeAnchor
            {
                shellBounds = stack.shellBounds,
                frameIndex = frameIndex,
                narrativeTime = narrativeTime
            };
            for (int i = 0; i < stack.spheres.Count; i++)
            {
                var center = stack.spheres[i].center;
                anchor.sphereCentersWorld.Add(center);
                if (manifold != null)
                {
                    anchor.manifoldCells.Add(new ManifoldAnchorCell
                    {
                        worldPosition = center,
                        data = manifold.GetDataAtPosition(center)
                    });
                }
            }
            return anchor;
        }

        public int ComputeHash()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + frameIndex;
                for (int i = 0; i < sphereCentersWorld.Count; i++)
                {
                    var p = sphereCentersWorld[i];
                    h = h * 31 + Mathf.RoundToInt(p.x * 100f);
                    h = h * 31 + Mathf.RoundToInt(p.y * 100f);
                    h = h * 31 + Mathf.RoundToInt(p.z * 100f);
                }
                return h;
            }
        }

        public void Restore(CloudHalfShellStack stack)
        {
            stack.RestorePositions(sphereCentersWorld);
        }

        public void RestoreManifold(WeatherPhysicsManifold manifold)
        {
            if (manifold == null || manifoldCells == null)
                return;
            for (int i = 0; i < manifoldCells.Count; i++)
            {
                var cell = manifoldCells[i];
                manifold.SetDataAtPosition(cell.worldPosition, cell.data);
            }
        }

        public float MaxAnchorDistance(CloudHalfShellStack stack)
        {
            if (stack == null || sphereCentersWorld.Count == 0)
                return 0f;
            float max = 0f;
            int n = Mathf.Min(sphereCentersWorld.Count, stack.spheres.Count);
            for (int i = 0; i < n; i++)
            {
                float d = Vector3.Distance(sphereCentersWorld[i], stack.spheres[i].center);
                if (d > max)
                    max = d;
            }
            return max;
        }
    }

    [Serializable]
    public sealed class CloudAdvectionDescriptor
    {
        public Vector3 viewerOrigin;
        public Vector3 viewerForward = Vector3.forward;
        public Vector3 viewerUp = Vector3.up;
        public Vector3 advectionVectorWorld;
        public Vector3 advectionVectorViewer;
        public Vector3 semiLagrangianBacktrace;
        public bool allowFloatAway;
        public CloudAdvectionMode mode = CloudAdvectionMode.AnchorReset;
        public CloudBakeAnchor anchor;
    }

    [Serializable]
    public sealed class CloudPerspectiveBakeConfig
    {
        public bool allowFloatAway;
        public float advectionDeltaTime = 1f / 30f;
        public bool useExecutorAdvection;
        public bool warmStartScalarsOnly = true;
        public int maxIterations = 32;
        public float sigmaMax = 0.5f;
        public float noiseGamma = 2f;
        public float noiseScale = 1f;
        public int spheresPerColumn = 3;
        public float lossGradientWeight = 1f;
        public float lossPixelWeight = 0.5f;
        public float lossDensityWeight = 0.3f;
        public float lossShadowWeight = 0.4f;
        public float lossPhysicsWeight = 0.1f;
        public CloudHalfShellConvexion convexion = new CloudHalfShellConvexion();
    }

    [Serializable]
    public struct CloudColumnSample
    {
        public int rayIndex;
        public int u;
        public int v;
        public Vector3 worldHit;
        public float columnDepth;
        public Color referenceColor;
        public int targetGradientBand;
        public int columnIndex;
        public float targetOpacity;
    }
}
