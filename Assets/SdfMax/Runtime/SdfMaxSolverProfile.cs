using UnityEngine;

namespace SdfMax
{
    [CreateAssetMenu(fileName = "SdfMaxSolverProfile", menuName = "SDF Max/Solver Profile")]
    public sealed class SdfMaxSolverProfile : ScriptableObject
    {
        [Header("Sampling")]
        public float feather = 0.05f;
        public float blendK = 0.1f;
        public float sampleEpsilon = 0.001f;

        [Header("Integral convex tree")]
        public int maxDepth = 8;
        public float minLeafExtent = 0.1f;

        [Header("Grid cache")]
        public bool useGridCache = true;
        public int gridResX = 16;
        public int gridResY = 16;
        public int gridResZ = 16;

        [Header("Auto defaults")]
        public float defaultTMin;
        public float defaultTMax = 1f;

        [Header("Surface mesh")]
        public bool generateSurfaceMesh = true;
        [Range(4, 96)]
        public int surfaceGridRes = 32;
        public float surfaceIsoLevel;
        public bool recalculateNormals = true;
        public bool generateColliderMesh = true;
        public bool convexCollider;
    }
}
