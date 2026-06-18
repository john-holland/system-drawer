using System.Text;
using UnityEngine;

namespace SdfMax
{
    public sealed class SdfMaxSurfaceMeshDebugReport
    {
        public struct GridSample
        {
            public int X;
            public int Y;
            public int Z;
            public Vector3 LocalPos;
            public Vector3 WorldPos;
            public float Field;
            public bool Inside;
        }

        public struct VertexSample
        {
            public int Index;
            public Vector3 LocalPos;
            public Vector3 WorldPos;
            public float FieldAtWorld;
            public float RadialDistance;
            public Vector3 Normal;
        }

        public struct ColumnProbe
        {
            public int GridX;
            public int GridZ;
            public float FieldMinY;
            public float FieldMidY;
            public float FieldMaxY;
            public bool VariesWithY;
        }

        public Bounds LocalBounds;
        public Matrix4x4 LocalToWorld;
        public float IsoLevel;
        public int GridResX;
        public int GridResY;
        public int GridResZ;
        public int InsideCount;
        public int OutsideCount;
        public float FieldMin;
        public float FieldMax;
        public int FaceQuadsX;
        public int FaceQuadsY;
        public int FaceQuadsZ;
        public int MeshVertexCount;
        public int MeshTriangleCount;
        public float VertexRadiusMin;
        public float VertexRadiusMax;
        public float VertexRadiusAvg;
        public float ReferenceRadius = -1f;
        public Vector3 ReferenceCenter;
        public GridSample[] GridSamples;
        public ColumnProbe[] ColumnProbes;
        public VertexSample[] VertexSamples;

        public void LogToConsole(string label = "SdfMax Surface Mesh Debug")
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine($"=== {label} ===");
            sb.AppendLine($"Local bounds: center={LocalBounds.center} size={LocalBounds.size}");
            sb.AppendLine($"LocalToWorld: pos={LocalToWorld.GetColumn(3)} rot={LocalToWorld.rotation.eulerAngles}");
            sb.AppendLine($"Grid: {GridResX}x{GridResY}x{GridResZ} iso={IsoLevel} inside={InsideCount} outside={OutsideCount}");
            sb.AppendLine($"Field range: min={FieldMin:F4} max={FieldMax:F4}");
            sb.AppendLine($"Face quads by axis: X={FaceQuadsX} Y={FaceQuadsY} Z={FaceQuadsZ} (voxel mesher; dominant X/Z => vertical straw walls)");
            sb.AppendLine($"Mesh: verts={MeshVertexCount} tris={MeshTriangleCount / 3}");
            sb.AppendLine($"Vertex radius from ref center {ReferenceCenter}: min={VertexRadiusMin:F2} avg={VertexRadiusAvg:F2} max={VertexRadiusMax:F2}");
            if (ReferenceRadius > 0f)
                sb.AppendLine($"Reference radius: {ReferenceRadius:F2} (delta avg={VertexRadiusAvg - ReferenceRadius:F2})");

            if (ColumnProbes != null)
            {
                sb.AppendLine("Column probes (same field at min/mid/max Y => 2D SDF extruded along Y):");
                for (int i = 0; i < ColumnProbes.Length; i++)
                {
                    var c = ColumnProbes[i];
                    sb.AppendLine(
                        $"  col ({c.GridX},{c.GridZ}): yMin={c.FieldMinY:F4} yMid={c.FieldMidY:F4} yMax={c.FieldMaxY:F4} variesY={c.VariesWithY}");
                }
            }

            if (VertexSamples != null)
            {
                sb.AppendLine("Mesh vertex samples:");
                for (int i = 0; i < VertexSamples.Length; i++)
                {
                    var v = VertexSamples[i];
                    sb.AppendLine(
                        $"  v{v.Index}: local={v.LocalPos} world={v.WorldPos} r={v.RadialDistance:F2} field={v.FieldAtWorld:F4} normal={v.Normal}");
                }
            }

            if (GridSamples != null && GridSamples.Length > 0)
            {
                sb.AppendLine("Grid samples (subset):");
                for (int i = 0; i < GridSamples.Length; i++)
                {
                    var s = GridSamples[i];
                    sb.AppendLine(
                        $"  ({s.X},{s.Y},{s.Z}) local={s.LocalPos} world={s.WorldPos} field={s.Field:F4} inside={s.Inside}");
                }
            }

            Debug.Log(sb.ToString());
        }
    }
}
