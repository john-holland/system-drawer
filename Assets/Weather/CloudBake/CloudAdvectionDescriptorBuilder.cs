using UnityEngine;
using Weather.Executor;

namespace Weather.CloudBake
{
    public static class CloudAdvectionDescriptorBuilder
    {
        public static CloudAdvectionDescriptor Build(
            CloudViewerSpec viewer,
            CloudHalfShellStack stack,
            WeatherPhysicsManifold manifold,
            Wind wind,
            CloudPerspectiveBakeConfig config,
            CloudBakeAnchor anchor = null)
        {
            var desc = new CloudAdvectionDescriptor
            {
                viewerOrigin = viewer != null ? viewer.ResolveOrigin() : Vector3.zero,
                viewerForward = viewer != null ? viewer.ResolveForward() : Vector3.forward,
                viewerUp = viewer != null ? viewer.ResolveUp() : Vector3.up,
                allowFloatAway = config != null && config.allowFloatAway,
                mode = config != null && config.allowFloatAway
                    ? CloudAdvectionMode.WeatherSolverAdvection
                    : CloudAdvectionMode.AnchorReset,
                anchor = anchor
            };

            if (desc.mode == CloudAdvectionMode.AnchorReset)
            {
                desc.advectionVectorWorld = Vector3.zero;
                desc.advectionVectorViewer = Vector3.zero;
                return desc;
            }

            Vector3 centroid = stack != null && stack.spheres.Count > 0
                ? stack.shellBounds.center
                : desc.viewerOrigin;
            float midAlt = stack != null ? (stack.cloudBaseM + stack.cloudTopM) * 0.5f : centroid.y;

            Vector3 vel = Vector3.zero;
            if (manifold != null)
                vel = manifold.GetDataAtPosition(centroid).velocity;
            if (wind != null)
                vel += wind.GetWindAtPosition(centroid, midAlt);

            desc.advectionVectorWorld = vel;
            desc.advectionVectorViewer = WorldToViewer(vel, desc.viewerForward, desc.viewerUp);
            float dt = config != null ? config.advectionDeltaTime : 1f / 30f;
            desc.semiLagrangianBacktrace = centroid - vel * dt;
            return desc;
        }

        public static void ApplyAdvectionStep(
            CloudAdvectionDescriptor desc,
            CloudHalfShellStack stack,
            WeatherPhysicsManifold manifold,
            Wind wind,
            Cloud cloud,
            float deltaTime,
            CloudPerspectiveBakeConfig config = null,
            WeatherExecutorService executor = null)
        {
            if (desc == null || stack == null || desc.mode != CloudAdvectionMode.WeatherSolverAdvection)
                return;

            if (config != null && config.useExecutorAdvection && executor != null && manifold != null)
            {
                var egg = executor.GetOrCreateEgg("cloud_bake");
                egg.transform.position = stack.shellBounds.center;
                egg.radii = stack.shellBounds.extents;
                manifold.SetEggLodActive(true, stack.shellBounds);
                executor.TickClient(deltaTime);
            }
            else if (manifold != null)
            {
                manifold.AdvectFieldsInBounds(deltaTime, stack.shellBounds);
            }

            Vector3 vel = desc.advectionVectorWorld;
            for (int i = 0; i < stack.spheres.Count; i++)
            {
                var s = stack.spheres[i];
                s.center -= vel * deltaTime;
                stack.spheres[i] = s;
            }

            if (cloud != null && CloudBakeSession.AllowFloatAway)
                cloud.ApplyWind(wind);

            stack.ComputeBounds();
        }

        public static void ApplyAnchorReset(
            CloudBakeAnchor anchor,
            CloudHalfShellStack stack,
            WeatherPhysicsManifold manifold = null)
        {
            anchor?.Restore(stack);
            anchor?.RestoreManifold(manifold);
        }

        static Vector3 WorldToViewer(Vector3 world, Vector3 forward, Vector3 up)
        {
            Vector3 right = Vector3.Cross(up, forward).normalized;
            up = Vector3.Cross(forward, right);
            return new Vector3(Vector3.Dot(world, right), Vector3.Dot(world, up), Vector3.Dot(world, forward));
        }

        public static string ToJson(CloudAdvectionDescriptor desc)
        {
            if (desc == null)
                return "{}";
            return JsonUtility.ToJson(desc, true);
        }
    }
}
