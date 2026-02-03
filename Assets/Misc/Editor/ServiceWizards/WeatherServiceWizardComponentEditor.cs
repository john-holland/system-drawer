using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WeatherServiceWizardComponent))]
public class WeatherServiceWizardComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var w = (WeatherServiceWizardComponent)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Assign from System Drawer", GUILayout.Height(22)))
        {
            var service = SystemDrawerService.FindInScene();
            if (service != null)
            {
                var go = service.Get<GameObject>(WeatherServiceWizardComponent.ServiceKey);
                if (go != null)
                {
                    Undo.RecordObject(w, "Assign from System Drawer");
                    w.weatherSystemObject = go;
                    EditorUtility.SetDirty(w);
                }
            }
        }
        if (GUILayout.Button("Open Weather Service Wizard", GUILayout.Height(22)))
        {
            var windowType = System.Type.GetType("Weather.WeatherServiceWizard, Weather.Editor");
            if (windowType != null)
            {
                var show = windowType.GetMethod("ShowWindow", System.Type.EmptyTypes);
                if (show != null) show.Invoke(null, null);
            }
            else
                Debug.LogWarning("[Misc] Weather package not found. Add Weather assembly for Weather Service Wizard.");
        }
    }
}
