using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Locomotion.Rendering
{
    /// <summary>
    /// Component that creates a transparent occlusion volume within defined bounds.
    /// Uses a dithered shader approach with gradient and random sampling for soft edges.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class TransparentOccluder : MonoBehaviour
    {
        [Header("Occlusion Bounds")]
        [Tooltip("The bounds of the occlusion volume in local space")]
        public Bounds bounds = new Bounds(Vector3.zero, Vector3.one);

        [Header("Dithering Settings")]
        [Range(0f, 1f)]
        [Tooltip("Overall dithering strength")]
        public float ditherIntensity = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Gradient-based dithering influence")]
        public float gradientStrength = 0.7f;

        [Range(1, 16)]
        [Tooltip("Number of random samples for noise")]
        public int randomSampleCount = 4;

        [Header("Backface Settings")]
        [Range(0f, 1f)]
        [Tooltip("How dark backfaces become (0 = transparent, 1 = black)")]
        public float backfaceDarkness = 0.8f;

        [Header("Fade Settings")]
        [Tooltip("Size of soft fade zone at bounds edges")]
        public float fadeZoneSize = 0.1f;

        [Range(0f, 90f)]
        [Tooltip("Angle threshold for lead face dithering")]
        public float leadFaceFadeAngle = 45f;

        [Header("Material")]
        [Tooltip("Material to use for rendering. If null, will create one automatically.")]
        public Material material;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh boxMesh;
        private Material instanceMaterial;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            
            // Create box mesh
            GenerateBoxMesh();
            
            // Setup material
            SetupMaterial();
        }

        private void OnEnable()
        {
            if (boxMesh != null)
            {
                meshFilter.mesh = boxMesh;
            }
            
            if (instanceMaterial != null)
            {
                meshRenderer.material = instanceMaterial;
            }
        }

        private void OnDisable()
        {
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                // Update mesh if bounds changed
                if (boxMesh != null)
                {
                    GenerateBoxMesh();
                }
                
                // Update material properties
                UpdateMaterialProperties();
            }
        }

        private void GenerateBoxMesh()
        {
            if (boxMesh == null)
            {
                boxMesh = new Mesh();
                boxMesh.name = "TransparentOccluderBox";
            }

            Vector3 center = bounds.center;
            Vector3 size = bounds.size;

            // Create box vertices
            Vector3[] vertices = new Vector3[8];
            vertices[0] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f); // Front Bottom Left
            vertices[1] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);  // Front Bottom Right
            vertices[2] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);   // Front Top Right
            vertices[3] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);  // Front Top Left
            vertices[4] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);  // Back Bottom Left
            vertices[5] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);   // Back Bottom Right
            vertices[6] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);    // Back Top Right
            vertices[7] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);   // Back Top Left

            // Create triangles for box (12 triangles, 2 per face)
            int[] triangles = new int[36];
            
            // Front face
            triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
            triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;
            
            // Back face
            triangles[6] = 4; triangles[7] = 5; triangles[8] = 6;
            triangles[9] = 4; triangles[10] = 6; triangles[11] = 7;
            
            // Top face
            triangles[12] = 3; triangles[13] = 6; triangles[14] = 2;
            triangles[15] = 3; triangles[16] = 7; triangles[17] = 6;
            
            // Bottom face
            triangles[18] = 0; triangles[19] = 1; triangles[20] = 5;
            triangles[21] = 0; triangles[22] = 5; triangles[23] = 4;
            
            // Right face
            triangles[24] = 1; triangles[25] = 2; triangles[26] = 6;
            triangles[27] = 1; triangles[28] = 6; triangles[29] = 5;
            
            // Left face
            triangles[30] = 0; triangles[31] = 7; triangles[32] = 3;
            triangles[33] = 0; triangles[34] = 4; triangles[35] = 7;

            // Calculate normals
            Vector3[] normals = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 normal = Vector3.zero;
                
                // Calculate normal based on which face the vertex belongs to
                if (Mathf.Approximately(vertex.x, center.x - size.x * 0.5f)) normal.x = -1;
                else if (Mathf.Approximately(vertex.x, center.x + size.x * 0.5f)) normal.x = 1;
                
                if (Mathf.Approximately(vertex.y, center.y - size.y * 0.5f)) normal.y = -1;
                else if (Mathf.Approximately(vertex.y, center.y + size.y * 0.5f)) normal.y = 1;
                
                if (Mathf.Approximately(vertex.z, center.z - size.z * 0.5f)) normal.z = -1;
                else if (Mathf.Approximately(vertex.z, center.z + size.z * 0.5f)) normal.z = 1;
                
                normals[i] = normal.normalized;
            }

            // Calculate UVs (for dithering calculations)
            Vector2[] uvs = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3 vertex = vertices[i];
                // Map vertex position to UV for gradient calculations
                uvs[i] = new Vector2(
                    (vertex.x - center.x + size.x * 0.5f) / size.x,
                    (vertex.y - center.y + size.y * 0.5f) / size.y
                );
            }

            boxMesh.vertices = vertices;
            boxMesh.triangles = triangles;
            boxMesh.normals = normals;
            boxMesh.uv = uvs;
            boxMesh.RecalculateBounds();
            boxMesh.RecalculateTangents();
        }

        private void SetupMaterial()
        {
            if (material == null)
            {
                // Try to load default material from Resources or Assets
                Material defaultMat = Resources.Load<Material>("Materials/TransparentOccluder");
                
#if UNITY_EDITOR
                if (defaultMat == null)
                {
                    // Try loading from asset path
                    defaultMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/locomotion/rendering/Materials/TransparentOccluder.mat");
                }
#endif
                
                if (defaultMat == null)
                {
                    Debug.LogWarning("TransparentOccluder: Default material not found. Please assign a material manually in the inspector.");
                    return;
                }
                material = defaultMat;
            }

            // Create material instance
            instanceMaterial = new Material(material);
            instanceMaterial.name = material.name + " (Instance)";
            
            // Set render queue for transparency
            instanceMaterial.renderQueue = 3000; // Transparent queue
            
            UpdateMaterialProperties();
        }

        private void UpdateMaterialProperties()
        {
            if (instanceMaterial == null) return;

            // Pass bounds to shader
            instanceMaterial.SetVector("_BoundsCenter", bounds.center);
            instanceMaterial.SetVector("_BoundsSize", bounds.size);
            
            // Pass dithering parameters
            instanceMaterial.SetFloat("_DitherIntensity", ditherIntensity);
            instanceMaterial.SetFloat("_GradientStrength", gradientStrength);
            instanceMaterial.SetInt("_RandomSampleCount", randomSampleCount);
            
            // Pass backface parameters
            instanceMaterial.SetFloat("_BackfaceDarkness", backfaceDarkness);
            
            // Pass fade parameters
            instanceMaterial.SetFloat("_FadeZoneSize", fadeZoneSize);
            instanceMaterial.SetFloat("_LeadFaceFadeAngle", leadFaceFadeAngle);
            
            // Pass transform for world space calculations
            instanceMaterial.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
            instanceMaterial.SetMatrix("_WorldToObject", transform.worldToLocalMatrix);
        }

        private void Update()
        {
            // Update material properties each frame (in case transform changes)
            if (instanceMaterial != null)
            {
                instanceMaterial.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
                instanceMaterial.SetMatrix("_WorldToObject", transform.worldToLocalMatrix);
            }
        }

        private void OnDestroy()
        {
            if (instanceMaterial != null)
            {
                Destroy(instanceMaterial);
            }
            
            if (boxMesh != null)
            {
                Destroy(boxMesh);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw bounds in editor
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
#endif
    }
}
