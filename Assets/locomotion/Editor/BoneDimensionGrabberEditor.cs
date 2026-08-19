#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoneDimensionGrabber))]
public sealed class BoneDimensionGrabberEditor : Editor
{
    void OnSceneGUI()
    {
        var grabber = (BoneDimensionGrabber)target;
        var map = grabber.ResolveBoneMap();
        if (map == null || map.entries == null)
            return;

        for (int i = 0; i < map.entries.Count; i++)
        {
            var e = map.entries[i];
            if (e == null || e.transform == null)
                continue;
            EditorGUI.BeginChangeCheck();
            Vector3 pos = Handles.PositionHandle(e.transform.position, e.transform.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(e.transform, "Bone dimension grab");
                e.transform.position = pos;
                var off = grabber.GetOrCreate(e.traitId);
                if (off != null)
                {
                    off.position = e.transform.localPosition;
                    off.euler = e.transform.localEulerAngles;
                    EditorUtility.SetDirty(grabber);
                }
            }
            Handles.Label(e.transform.position + Vector3.up * grabber.gizmoRadius, e.traitId ?? "");
        }
    }
}
#endif
