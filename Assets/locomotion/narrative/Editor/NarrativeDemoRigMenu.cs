#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    public static class NarrativeDemoRigMenu
    {
        [MenuItem("GameObject/Locomotion/Narrative/Create Demo Rig", false, 10)]
        public static void CreateDemoRig()
        {
            var go = new GameObject("NarrativeDemoRig");
            Undo.RegisterCreatedObjectUndo(go, "Create Narrative Demo Rig");
            go.AddComponent<Locomotion.Narrative.NarrativeDemoRig>();
            Selection.activeGameObject = go;
        }
    }
}
#endif

