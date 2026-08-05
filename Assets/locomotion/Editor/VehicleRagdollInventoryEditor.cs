using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

[CustomEditor(typeof(VehicleRagdoll), true)]
public sealed class VehicleRagdollInventoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var vehicle = (VehicleRagdoll)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Continuuuum inventory", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Computed size", vehicle.ComputeInteriorSizeSum().ToString("0.##"));
        if (GUILayout.Button("Update size for Continuuuum"))
        {
            vehicle.RecalculateTotalInteriorSize();
            EditorUtility.SetDirty(vehicle);
            PushToContinuuuum(vehicle);
        }
    }

    static void PushToContinuuuum(VehicleRagdoll vehicle)
    {
        string baseUrl = EditorPrefs.GetString("lemmaApiBase", "http://127.0.0.1:5050").TrimEnd('/');
        string json = JsonUtility.ToJson(new ContinuuuumVehiclePayload
        {
            vehicleId = vehicle.vehicleId,
            displayName = vehicle.displayName,
            integrity01 = vehicle.integrity01,
            totalSize = vehicle.totalInteriorSize
        });
        // Prefer DTO with interiors via simple manual JSON for sections
        var dto = vehicle.ToDto();
        var sb = new StringBuilder();
        sb.Append("{\"vehicleId\":\"").Append(Escape(vehicle.vehicleId)).Append("\",");
        sb.Append("\"displayName\":\"").Append(Escape(vehicle.displayName)).Append("\",");
        sb.Append("\"integrity01\":").Append(vehicle.integrity01.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(",");
        sb.Append("\"totalSize\":").Append(vehicle.totalInteriorSize.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(",");
        sb.Append("\"interiors\":[");
        for (int i = 0; i < vehicle.interiors.Count; i++)
        {
            var s = vehicle.interiors[i];
            if (s == null) continue;
            if (i > 0) sb.Append(",");
            sb.Append("{\"sectionName\":\"").Append(Escape(s.sectionName)).Append("\",");
            sb.Append("\"capacity\":").Append(s.capacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(",\"items\":[]}");
        }
        sb.Append("]}");
        json = sb.ToString();

        var req = new UnityWebRequest($"{baseUrl}/api/civil/vehicle-inventory/{UnityWebRequest.EscapeURL(vehicle.vehicleId)}", "PUT");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        var op = req.SendWebRequest();
        while (!op.isDone) { }
#if UNITY_2020_1_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
            Debug.LogWarning($"Continuuuum vehicle inventory update failed: {req.error} {req.downloadHandler?.text}");
        else
            Debug.Log($"Updated Continuuuum vehicle inventory for {vehicle.vehicleId} size={vehicle.totalInteriorSize}");
        req.Dispose();
        _ = dto;
    }

    static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

    [System.Serializable]
    sealed class ContinuuuumVehiclePayload
    {
        public string vehicleId;
        public string displayName;
        public float integrity01;
        public float totalSize;
    }
}
