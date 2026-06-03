using UnityEngine;

namespace Planetary
{
    public static class PlanetCubeSphere6Face
    {
        public static Vector3 CubeToSphere(Vector3 cube, float radius)
        {
            Vector3 c = cube.normalized;
            float x2 = c.x * c.x;
            float y2 = c.y * c.y;
            float z2 = c.z * c.z;
            Vector3 s = new Vector3(
                c.x * Mathf.Sqrt(1f - (y2 + z2) * 0.5f + (y2 * z2) / 3f),
                c.y * Mathf.Sqrt(1f - (z2 + x2) * 0.5f + (z2 * x2) / 3f),
                c.z * Mathf.Sqrt(1f - (x2 + y2) * 0.5f + (x2 * y2) / 3f));
            return s * radius;
        }

        public static Vector3 FaceUvToCube(PlanetFaceId face, float u, float v)
        {
            u = u * 2f - 1f;
            v = v * 2f - 1f;
            switch (face)
            {
                case PlanetFaceId.PosX: return new Vector3(1f, v, u);
                case PlanetFaceId.NegX: return new Vector3(-1f, v, -u);
                case PlanetFaceId.PosY: return new Vector3(u, 1f, v);
                case PlanetFaceId.NegY: return new Vector3(u, -1f, -v);
                case PlanetFaceId.PosZ: return new Vector3(u, v, 1f);
                default: return new Vector3(-u, v, -1f);
            }
        }
    }
}
