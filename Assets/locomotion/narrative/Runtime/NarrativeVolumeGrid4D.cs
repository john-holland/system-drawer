using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// 4D grid (x,y,z,t) for narrative occupancy and causal gradient. Used by 4D Query API.
    /// Occupancy: max over volumes (1 inside any volume, 0 outside). Causal: depth/rank per cell when volumes have order.
    /// </summary>
    public class NarrativeVolumeGrid4D
    {
        private Bounds spatialBounds;
        private float tMin;
        private float tMax;
        private int resX, resY, resZ, resT;
        private float[] occupancy;
        private float[] causalDepth;
        private bool built;

        public Bounds SpatialBounds => spatialBounds;
        public float TMin => tMin;
        public float TMax => tMax;
        public int ResX => resX;
        public int ResY => resY;
        public int ResZ => resZ;
        public int ResT => resT;
        public bool IsBuilt => built;

        /// <summary>Configure grid. Call BuildFromVolumes to fill.</summary>
        public void Configure(Bounds spatialBounds, float tMin, float tMax, int resX, int resY, int resZ, int resT)
        {
            this.spatialBounds = spatialBounds;
            this.tMin = tMin;
            this.tMax = tMax;
            this.resX = Mathf.Max(1, resX);
            this.resY = Mathf.Max(1, resY);
            this.resZ = Mathf.Max(1, resZ);
            this.resT = Mathf.Max(1, resT);
            int count = this.resX * this.resY * this.resZ * this.resT;
            if (occupancy == null || occupancy.Length != count)
            {
                occupancy = new float[count];
                causalDepth = new float[count];
            }
            built = false;
        }

        /// <summary>Build occupancy (SDF max) and optional causal gradient from volumes. causalOrder: optional depth index per volume (same length as volumes).</summary>
        public void BuildFromVolumes(IEnumerable<Bounds4> volumes, IReadOnlyList<int> causalOrder = null)
        {
            if (occupancy == null)
                return;

            int count = occupancy.Length;
            for (int i = 0; i < count; i++)
            {
                occupancy[i] = 0f;
                causalDepth[i] = 0f;
            }

            int volIndex = 0;
            foreach (var vol in volumes)
            {
                int depth = (causalOrder != null && volIndex < causalOrder.Count) ? causalOrder[volIndex] : 0;
                RasterizeVolume(vol, depth);
                volIndex++;
            }

            built = true;
        }

        private void RasterizeVolume(Bounds4 vol, int depth)
        {
            Vector3 mn = vol.min;
            Vector3 mx = vol.max;
            float cellSizeX = spatialBounds.size.x / resX;
            float cellSizeY = spatialBounds.size.y / resY;
            float cellSizeZ = spatialBounds.size.z / resZ;
            Vector3 origin = spatialBounds.min;

            int ix0 = WorldToCell(origin.x, mn.x, cellSizeX);
            int ix1 = WorldToCell(origin.x, mx.x, cellSizeX);
            int iy0 = WorldToCell(origin.y, mn.y, cellSizeY);
            int iy1 = WorldToCell(origin.y, mx.y, cellSizeY);
            int iz0 = WorldToCell(origin.z, mn.z, cellSizeZ);
            int iz1 = WorldToCell(origin.z, mx.z, cellSizeZ);
            int it0 = TimeToSlice(vol.tMin);
            int it1 = TimeToSlice(vol.tMax);
            if (ix0 > ix1) { int t = ix0; ix0 = ix1; ix1 = t; }
            if (iy0 > iy1) { int t = iy0; iy0 = iy1; iy1 = t; }
            if (iz0 > iz1) { int t = iz0; iz0 = iz1; iz1 = t; }
            if (it0 > it1) { int t = it0; it0 = it1; it1 = t; }
            ix0 = Mathf.Clamp(ix0, 0, resX - 1);
            ix1 = Mathf.Clamp(ix1, 0, resX - 1);
            iy0 = Mathf.Clamp(iy0, 0, resY - 1);
            iy1 = Mathf.Clamp(iy1, 0, resY - 1);
            iz0 = Mathf.Clamp(iz0, 0, resZ - 1);
            iz1 = Mathf.Clamp(iz1, 0, resZ - 1);
            it0 = Mathf.Clamp(it0, 0, resT - 1);
            it1 = Mathf.Clamp(it1, 0, resT - 1);

            for (int it = it0; it <= it1; it++)
            {
                for (int iz = iz0; iz <= iz1; iz++)
                {
                    for (int iy = iy0; iy <= iy1; iy++)
                    {
                        for (int ix = ix0; ix <= ix1; ix++)
                        {
                            int idx = Index(ix, iy, iz, it);
                            occupancy[idx] = 1f;
                            if (depth > 0 || causalDepth[idx] == 0f)
                                causalDepth[idx] = Mathf.Max(causalDepth[idx], depth);
                        }
                    }
                }
            }
        }

        private static int WorldToCell(float origin, float world, float cellSize)
        {
            int c = (int)((world - origin) / cellSize);
            return c;
        }

        private int TimeToSlice(float t)
        {
            float u = (t - tMin) / Mathf.Max(tMax - tMin, 0.001f);
            int c = (int)(u * resT);
            return Mathf.Clamp(c, 0, resT - 1);
        }

        private int Index(int ix, int iy, int iz, int it)
        {
            return ix + resX * (iy + resY * (iz + resZ * it));
        }

        /// <summary>Sample occupancy (0..1) and causal depth at (x,y,z,t). Returns false if not configured or out of range.</summary>
        public bool Sample4D(float x, float y, float z, float t, out float outOccupancy, out float outCausalDepth)
        {
            outOccupancy = 0f;
            outCausalDepth = 0f;
            if (!built || occupancy == null)
                return false;

            float cellSizeX = spatialBounds.size.x / resX;
            float cellSizeY = spatialBounds.size.y / resY;
            float cellSizeZ = spatialBounds.size.z / resZ;
            Vector3 origin = spatialBounds.min;
            int ix = (int)((x - origin.x) / cellSizeX);
            int iy = (int)((y - origin.y) / cellSizeY);
            int iz = (int)((z - origin.z) / cellSizeZ);
            int it = TimeToSlice(t);

            if (ix < 0 || ix >= resX || iy < 0 || iy >= resY || iz < 0 || iz >= resZ)
                return true;

            int idx = Index(ix, iy, iz, it);
            outOccupancy = occupancy[idx];
            outCausalDepth = causalDepth[idx];
            return true;
        }

        /// <summary>Sample at world position and time.</summary>
        public bool Sample4D(Vector3 position, float t, out float outOccupancy, out float outCausalDepth)
        {
            return Sample4D(position.x, position.y, position.z, t, out outOccupancy, out outCausalDepth);
        }

        /// <summary>True if (position, t) is inside any volume (occupancy > 0).</summary>
        public bool IsInsideNarrativeVolume(Vector3 position, float t)
        {
            Sample4D(position, t, out float occ, out _);
            return occ > 0.5f;
        }

        /// <summary>Fill 3D slice at time t. occupancyOut and causalOut must be length resX*resY*resZ.</summary>
        public void GetSliceAtT(float t, float[] occupancyOut, float[] causalOut)
        {
            if (!built || occupancy == null || occupancyOut == null || causalOut == null)
                return;
            int need = resX * resY * resZ;
            if (occupancyOut.Length < need || causalOut.Length < need)
                return;
            Vector3 origin = spatialBounds.min;
            float cellSizeX = spatialBounds.size.x / resX;
            float cellSizeY = spatialBounds.size.y / resY;
            float cellSizeZ = spatialBounds.size.z / resZ;
            for (int iz = 0; iz < resZ; iz++)
            {
                for (int iy = 0; iy < resY; iy++)
                {
                    for (int ix = 0; ix < resX; ix++)
                    {
                        float x = origin.x + (ix + 0.5f) * cellSizeX;
                        float y = origin.y + (iy + 0.5f) * cellSizeY;
                        float z = origin.z + (iz + 0.5f) * cellSizeZ;
                        int idx = Index(ix, iy, iz, TimeToSlice(t));
                        occupancyOut[ix + resX * (iy + resY * iz)] = occupancy[idx];
                        causalOut[ix + resX * (iy + resY * iz)] = causalDepth[idx];
                    }
                }
            }
        }
    }
}
