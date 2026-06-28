using System.Collections.Generic;
using UnityEngine;
using Weather.Executor;

namespace Weather.CloudBake
{
    public sealed class CloudPerspectiveBakeSolver
    {
        public struct BakeResult
        {
            public CloudHalfShellStack stack;
            public CloudAdvectionDescriptor advection;
            public CloudBakeAnchor anchor;
            public List<float> lossHistory;
            public float finalLoss;
        }

        readonly CloudTodShadowModel _shadowModel = new CloudTodShadowModel();

        public BakeResult Bake(
            CloudViewerSpec viewer,
            CloudPerspectiveTarget target,
            List<CloudColumnSample> columns,
            WeatherPhysicsManifold manifold,
            Wind wind,
            Cloud cloud,
            Water water,
            CloudPerspectiveBakeConfig config,
            CloudHalfShellStack warmStart = null,
            WeatherExecutorService executor = null,
            int frameIndex = 0)
        {
            config ??= new CloudPerspectiveBakeConfig();
            _shadowModel.RefreshFromScene();

            CloudHalfShellStack stack = warmStart != null && config.warmStartScalarsOnly
                ? CloneStackScalars(warmStart, columns, config, viewer)
                : CloudHalfShellBuilder.Build(
                    columns, manifold, ResolveCloudBase(cloud), ResolveCloudTop(cloud),
                    config.spheresPerColumn, viewer, config.convexion);

            if (stack.spheres.Count == 0 && columns != null && columns.Count > 0)
                stack = CloudHalfShellBuilder.Build(
                    columns, manifold, ResolveCloudBase(cloud), ResolveCloudTop(cloud),
                    config.spheresPerColumn, viewer, config.convexion);

            var anchor = CloudBakeAnchor.Capture(stack, manifold, frameIndex);
            var desc = CloudAdvectionDescriptorBuilder.Build(viewer, stack, manifold, wind, config, anchor);
            var lossHistory = new List<float>();
            Vector3 viewDir = viewer != null ? viewer.ResolveForward() : Vector3.forward;

            CloudBakeSession.Begin(config.allowFloatAway);
            using var integration = new CloudBakeIntegration(manifold, executor, config);
            integration.BeginSession(stack);
            try
            {
                for (int i = 0; i < config.maxIterations; i++)
                {
                    if (stack.spheres.Count == 0)
                        break;

                    if (!config.allowFloatAway)
                        CloudAdvectionDescriptorBuilder.ApplyAnchorReset(anchor, stack, manifold);

                    float loss = EvaluateLoss(stack, target, columns, config);
                    lossHistory.Add(loss);

                    GradientStep(stack, target, columns, config, i);
                    for (int s = 0; s < stack.spheres.Count; s++)
                    {
                        var sphere = stack.spheres[s];
                        FresnelNoiseSchedule.PerturbSphere(ref sphere, viewDir, i + s, config.maxIterations,
                            config.sigmaMax, config.noiseGamma, config.noiseScale);
                        stack.spheres[s] = sphere;
                    }

                    if (config.allowFloatAway)
                    {
                        desc = CloudAdvectionDescriptorBuilder.Build(viewer, stack, manifold, wind, config, anchor);
                        CloudAdvectionDescriptorBuilder.ApplyAdvectionStep(
                            desc, stack, manifold, wind, cloud, config.advectionDeltaTime, config, executor);
                    }
                    else
                    {
                        CloudAdvectionDescriptorBuilder.ApplyAnchorReset(anchor, stack, manifold);
                    }

                    if (manifold != null)
                        KalmanBlendManifold(stack, manifold, target, i, config);

                    integration.AfterIteration(stack, anchor, null);
                }

                CloudHalfShellBuilder.PaintIntoManifold(stack, manifold, water);
            }
            finally
            {
                CloudBakeSession.End();
            }

            float finalLoss = lossHistory.Count > 0 ? lossHistory[lossHistory.Count - 1] : 0f;
            desc = CloudAdvectionDescriptorBuilder.Build(viewer, stack, manifold, wind, config, anchor);

            return new BakeResult
            {
                stack = stack,
                advection = desc,
                anchor = anchor,
                lossHistory = lossHistory,
                finalLoss = finalLoss
            };
        }

        static CloudHalfShellStack CloneStackScalars(
            CloudHalfShellStack warm,
            List<CloudColumnSample> columns,
            CloudPerspectiveBakeConfig config,
            CloudViewerSpec viewer)
        {
            var fresh = new CloudHalfShellStack
            {
                cloudBaseM = warm.cloudBaseM,
                cloudTopM = warm.cloudTopM
            };
            if (columns == null)
                return warm;

            var built = CloudHalfShellBuilder.Build(
                columns, null, warm.cloudBaseM, warm.cloudTopM,
                config.spheresPerColumn, viewer, config.convexion);
            int n = Mathf.Min(built.spheres.Count, warm.spheres.Count);
            for (int i = 0; i < built.spheres.Count; i++)
            {
                var s = built.spheres[i];
                if (i < n)
                {
                    s.density = warm.spheres[i].density;
                    s.moisture = warm.spheres[i].moisture;
                    s.radius = Mathf.Lerp(s.radius, warm.spheres[i].radius, 0.5f);
                }
                fresh.spheres.Add(s);
            }
            fresh.ComputeBounds();
            return fresh;
        }

        float EvaluateLoss(
            CloudHalfShellStack stack,
            CloudPerspectiveTarget target,
            List<CloudColumnSample> columns,
            CloudPerspectiveBakeConfig config)
        {
            float lGrad = GradientLoss(target);
            float lPixel = PixelLoss(stack, columns);
            float lDensity = DensityLoss(stack);
            float lShadow = _shadowModel.EvaluateShadowLoss(stack, target);
            float lPhysics = PhysicsLoss(stack, config);

            return lGrad * config.lossGradientWeight
                   + lPixel * config.lossPixelWeight
                   + lDensity * config.lossDensityWeight
                   + lShadow * config.lossShadowWeight
                   + lPhysics * config.lossPhysicsWeight;
        }

        static float GradientLoss(CloudPerspectiveTarget target)
        {
            if (target?.gradientBands == null)
                return 0f;
            var bands = target.gradientBands;
            float d1 = CloudTodShadowModel.ColorDeltaLab(bands.top, bands.mid);
            float d2 = CloudTodShadowModel.ColorDeltaLab(bands.mid, bands.bottom);
            return d1 + d2;
        }

        static float PixelLoss(CloudHalfShellStack stack, List<CloudColumnSample> columns)
        {
            if (columns == null || columns.Count == 0 || stack.spheres.Count == 0)
                return 0f;
            float loss = 0f;
            int n = 0;
            foreach (var col in columns)
            {
                int sphereIdx = col.columnIndex * stack.spheres.Count / Mathf.Max(1, columns.Count);
                sphereIdx = Mathf.Clamp(sphereIdx, 0, stack.spheres.Count - 1);
                float predicted = stack.spheres[sphereIdx].density;
                float target = col.targetOpacity;
                float d = predicted - target;
                loss += d * d;
                n++;
            }
            return n > 0 ? loss / n : 0f;
        }

        static float DensityLoss(CloudHalfShellStack stack)
        {
            if (stack.spheres.Count == 0)
                return 0f;
            float sum = 0f;
            foreach (var s in stack.spheres)
                sum += s.density;
            float mean = sum / stack.spheres.Count;
            return Mathf.Abs(mean - 0.5f);
        }

        static float PhysicsLoss(CloudHalfShellStack stack, CloudPerspectiveBakeConfig config)
        {
            if (!config.allowFloatAway)
                return 0f;
            float loss = 0f;
            foreach (var s in stack.spheres)
                loss += s.moisture * s.moisture;
            return loss / Mathf.Max(1, stack.spheres.Count);
        }

        static void GradientStep(
            CloudHalfShellStack stack,
            CloudPerspectiveTarget target,
            List<CloudColumnSample> columns,
            CloudPerspectiveBakeConfig config,
            int iteration)
        {
            float step = 0.05f * (1f - iteration / (float)Mathf.Max(1, config.maxIterations));
            for (int i = 0; i < stack.spheres.Count; i++)
            {
                var s = stack.spheres[i];
                s.density = Mathf.Clamp(s.density - step * (s.density - 0.5f), 0.05f, 2f);
                if (columns != null && i < columns.Count)
                    s.density = Mathf.Lerp(s.density, columns[i].targetOpacity, step);
                stack.spheres[i] = s;
            }
        }

        static void KalmanBlendManifold(
            CloudHalfShellStack stack,
            WeatherPhysicsManifold manifold,
            CloudPerspectiveTarget target,
            int iteration,
            CloudPerspectiveBakeConfig config)
        {
            float w = FresnelNoiseSchedule.KalmanBlendWeight(iteration, config.maxIterations, config.sigmaMax, config.noiseGamma);
            foreach (var sphere in stack.spheres)
            {
                var existing = manifold.GetDataAtPosition(sphere.center);
                var targetCell = new ManifoldCellData
                {
                    density = sphere.density,
                    temperature = existing.temperature,
                    pressure = existing.pressure,
                    velocity = existing.velocity,
                    mode = WeatherMode.Cloud
                };
                var blended = WeatherKalmanMerge.BlendCells(existing, targetCell, w);
                manifold.SetDataAtPosition(sphere.center, blended);
            }
        }

        static float ResolveCloudBase(Cloud cloud) =>
            cloud != null ? cloud.altitude.x : 1000f;

        static float ResolveCloudTop(Cloud cloud) =>
            cloud != null ? cloud.altitude.y : 2000f;
    }
}
