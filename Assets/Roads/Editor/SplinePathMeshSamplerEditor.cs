using UnityEditor;
using UnityEngine;

namespace Roads.Editor
{
    [CustomEditor(typeof(SplinePathMeshSampler))]
    public class SplinePathMeshSamplerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var sampler = (SplinePathMeshSampler)target;
            if (GUILayout.Button("Preview Ribbon Mesh"))
            {
                var mesh = sampler.BuildCombinedMesh();
                Debug.Log($"Ribbon verts: {mesh.vertexCount}, bounds: {sampler.ComputeWorldBounds()}");
            }
        }
    }
}
