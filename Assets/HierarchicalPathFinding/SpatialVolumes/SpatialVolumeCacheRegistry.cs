using System.Collections.Generic;
using UnityEngine;

namespace SpatialVolumes
{
    public sealed class SpatialVolumeCacheEntry
    {
        public int BuildVersion;
        public Bounds WorldBounds;
        public int LeafCount;
        public int TransformVersion;
    }

    public static class SpatialVolumeCacheRegistry
    {
        sealed class Slot
        {
            public ISpatialVolumeBackend Backend;
            public SpatialVolumeCacheEntry Entry;
            public int BackendKind;
            public int ContentHash;
            public int TransformVersion;
        }

        static readonly Dictionary<int, Slot> Slots = new Dictionary<int, Slot>();
        static int _globalVersion;

        public static int ComputeTransformVersion(Transform t)
        {
            if (t == null)
                return 0;
            Matrix4x4 m = t.localToWorldMatrix;
            unchecked
            {
                int h = 17;
                for (int i = 0; i < 16; i++)
                    h = h * 31 + m[i].GetHashCode();
                return h;
            }
        }

        static int ContentHash(SpatialVolumeProvider provider)
        {
            unchecked
            {
                int h = (int)provider.backend * 31;
                if (provider.backend == VolumeBackend.MeshConvexTree && provider.meshCollider != null && provider.meshCollider.sharedMesh != null)
                    h = h * 31 + provider.meshCollider.sharedMesh.GetInstanceID();
                else if (provider.composition != null)
                    h = h * 31 + provider.composition.GetInstanceID();
                if (provider.profile != null)
                    h = h * 31 + provider.profile.GetInstanceID();
                h = h * 31 + provider.SurfaceMeshVersion;
                return h;
            }
        }

        public static bool EnsureBuilt(SpatialVolumeProvider provider, bool force = false)
        {
            if (provider == null)
                return false;

            int id = provider.GetInstanceID();
            int tv = ComputeTransformVersion(provider.transform);
            int ch = ContentHash(provider);

            Slots.TryGetValue(id, out Slot existing);

            if (!force && existing != null &&
                existing.Backend != null && existing.BackendKind == (int)provider.backend &&
                existing.ContentHash == ch &&
                (!provider.SyncSDFTreeShape || existing.TransformVersion == tv))
            {
                return existing.Entry != null && existing.Entry.LeafCount > 0;
            }

            if (provider.SyncSDFTreeShape && existing != null && existing.TransformVersion != tv &&
                provider.backend == VolumeBackend.MeshConvexTree)
            {
                existing.Backend?.Invalidate();
            }

            var ctx = provider.CreateBuildContext();
            ctx.LastLocalToWorld = provider.transform.localToWorldMatrix;

            ISpatialVolumeBackend backend = provider.backend == VolumeBackend.MeshConvexTree
                ? new MeshConvexTreeBackend()
                : (ISpatialVolumeBackend)new SdfMaxCompositionBackend();

            bool ok = backend.EnsureBuilt(ctx);
            int leaves = 0;
            var scratch = new List<SpatialVolumeLeaf>();
            backend.CollectLeaves(backend.WorldBounds, 0f, scratch);
            leaves = scratch.Count;

            Slots[id] = new Slot
            {
                Backend = backend,
                BackendKind = (int)provider.backend,
                ContentHash = ch,
                TransformVersion = tv,
                Entry = new SpatialVolumeCacheEntry
                {
                    BuildVersion = backend.BuildVersion,
                    WorldBounds = backend.WorldBounds,
                    LeafCount = leaves,
                    TransformVersion = tv
                }
            };

            _globalVersion++;
            return ok;
        }

        public static bool TryGetBackend(SpatialVolumeProvider provider, out ISpatialVolumeBackend backend)
        {
            backend = null;
            if (provider == null)
                return false;
            if (!Slots.TryGetValue(provider.GetInstanceID(), out Slot slot) || slot?.Backend == null)
                return EnsureBuilt(provider) && Slots.TryGetValue(provider.GetInstanceID(), out slot) && slot.Backend != null;
            backend = slot.Backend;
            return backend != null;
        }

        public static void Invalidate(SpatialVolumeProvider provider)
        {
            if (provider == null)
                return;
            int id = provider.GetInstanceID();
            if (Slots.TryGetValue(id, out Slot slot))
            {
                slot.Backend?.Invalidate();
                Slots.Remove(id);
            }
        }

        public static void InvalidateAll()
        {
            foreach (var kv in Slots)
                kv.Value.Backend?.Invalidate();
            Slots.Clear();
        }
    }
}
