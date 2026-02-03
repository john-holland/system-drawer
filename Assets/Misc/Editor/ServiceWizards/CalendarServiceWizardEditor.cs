using UnityEngine;
using UnityEditor;
using Locomotion.Narrative;
using Locomotion.Narrative.EditorTools;

[CustomEditor(typeof(CalendarServiceWizard))]
public class CalendarServiceWizardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var w = (CalendarServiceWizard)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Assign from System Drawer", GUILayout.Height(22)))
        {
            var service = SystemDrawerService.FindInScene();
            if (service != null)
            {
                var obj = service.Get<MonoBehaviour>(CalendarServiceWizard.ServiceKey);
                if (obj != null)
                {
                    Undo.RecordObject(w, "Assign from System Drawer");
                    w.calendarAsset = obj;
                    EditorUtility.SetDirty(w);
                }
            }
        }
        if (GUILayout.Button("Open Calendar Wizard", GUILayout.Height(22)))
        {
            var cal = w.calendarAsset as NarrativeCalendarAsset;
            if (cal != null)
                NarrativeCalendarWizardWindow.ShowWindow(cal);
            else
                NarrativeCalendarWizardWindow.ShowWindow();
        }
    }
}
