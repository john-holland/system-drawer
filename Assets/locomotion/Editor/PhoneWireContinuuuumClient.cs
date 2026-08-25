using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Editor GET/PUT for Continuuuum phone-wire associations.</summary>
public static class PhoneWireContinuuuumClient
{
    public static string LastJson;
    public static string LastError;

    static string Base() => EditorPrefs.GetString("lemmaApiBase", "http://127.0.0.1:5050").TrimEnd('/');

    public static void Pull()
    {
        LastError = null;
        try
        {
            using var req = UnityWebRequest.Get(Base() + "/api/civil/phone-wire-associations");
            var op = req.SendWebRequest();
            while (!op.isDone) { }
            LastJson = req.downloadHandler?.text;
            if (req.result != UnityWebRequest.Result.Success)
                LastError = req.error;
        }
        catch (System.Exception e)
        {
            LastError = e.Message;
        }
    }

    public static void PushScene()
    {
        LastError = null;
        foreach (var pole in PhonePoleIndex.All)
        {
            if (pole == null) continue;
            PutJson("/api/civil/phone-poles/" + pole.poleId, PoleJson(pole));
        }
        foreach (var wire in StreetWireIndex.All)
        {
            if (wire == null) continue;
            PutJson("/api/civil/phone-wires/" + wire.wireId, WireJson(wire));
        }
        var ends = Object.FindObjectsByType<StreetWireEnd>(FindObjectsSortMode.None);
        for (int i = 0; i < ends.Length; i++)
        {
            var e = ends[i];
            if (e == null) continue;
            PutJson("/api/civil/phone-wire-associations", AssocJson(e, null));
        }
    }

    public static string AutoFill(IntersectionLot lot)
    {
        string poles = "";
        if (lot != null)
        {
            var near = PhonePoleIndex.QueryNear(lot.transform.position, 40f);
            if (near.Count >= 2)
                poles = "?poleId=" + near[0].poleId + "&toPoleId=" + near[1].poleId + "&intersectionLotId=" + lot.lotId;
        }
        try
        {
            using var req = new UnityWebRequest(Base() + "/api/civil/phone-wire-associations/auto" + poles, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            var op = req.SendWebRequest();
            while (!op.isDone) { }
            LastJson = req.downloadHandler?.text;
            return LastJson;
        }
        catch (System.Exception e)
        {
            LastError = e.Message;
            return null;
        }
    }

    static void PutJson(string path, string json)
    {
        using var req = new UnityWebRequest(Base() + path, "PUT");
        byte[] body = Encoding.UTF8.GetBytes(json ?? "{}");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        var op = req.SendWebRequest();
        while (!op.isDone) { }
        LastJson = req.downloadHandler?.text;
        if (req.result != UnityWebRequest.Result.Success)
            LastError = req.error;
    }

    static string PoleJson(UtilityPoleAssembly p)
    {
        Vector3 w = p.transform.position;
        return "{\"pole_id\":\"" + Esc(p.poleId) + "\",\"display_name\":\"" + Esc(p.name) +
               "\",\"world_json\":\"{\\\"x\\\":" + w.x + ",\\\"y\\\":" + w.y + ",\\\"z\\\":" + w.z + "}\"}";
    }

    static string WireJson(PowerLineSpan s)
    {
        return "{\"wire_id\":\"" + Esc(s.wireId) + "\",\"from_pole_id\":\"" + Esc(s.fromPoleId) +
               "\",\"to_pole_id\":\"" + Esc(s.toPoleId) + "\"}";
    }

    static string AssocJson(StreetWireEnd e, IntersectionLot lot)
    {
        return "{\"pole_id\":\"" + Esc(e.poleId) + "\",\"wire_id\":\"" + Esc(e.wireId) +
               "\",\"intersection_lot_id\":\"" + Esc(lot != null ? lot.lotId : "") +
               "\",\"wire_end_kind\":\"" + e.kind + "\",\"t01\":" + e.t01.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}";
    }

    static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
}
