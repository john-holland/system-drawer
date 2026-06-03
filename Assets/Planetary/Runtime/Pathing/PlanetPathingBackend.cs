using UnityEngine;

namespace Planetary
{
    /// <summary>Planet chunk tangent-space pathing adjacency across cube faces.</summary>
    public sealed class PlanetPathingBackend
    {
        public int ChunksPerFace = 2;
        public float PlanetRadius = 1000f;

        public bool TryGetNeighborChunk(PlanetFaceId face, int cx, int cy, int dx, int dy,
            out PlanetFaceId outFace, out int outCx, out int outCy)
        {
            outFace = face;
            outCx = cx + dx;
            outCy = cy + dy;
            if (outCx >= 0 && outCx < ChunksPerFace && outCy >= 0 && outCy < ChunksPerFace)
                return true;
            return TryCrossFace(face, cx, cy, dx, dy, out outFace, out outCx, out outCy);
        }

        static bool TryCrossFace(PlanetFaceId face, int cx, int cy, int dx, int dy,
            out PlanetFaceId outFace, out int outCx, out int outCy)
        {
            outFace = face;
            outCx = outCy = 0;
            if (dx > 0 && cx == 0) { outFace = OppositeX(face); outCx = 0; return true; }
            return false;
        }

        static PlanetFaceId OppositeX(PlanetFaceId f) =>
            f == PlanetFaceId.PosX ? PlanetFaceId.NegX : PlanetFaceId.PosX;
    }
}
