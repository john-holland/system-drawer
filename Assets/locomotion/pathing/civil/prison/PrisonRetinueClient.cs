using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Calls /api/society/cities/{id}/prisons/{stableId}/retinue for request/sync/merge.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Prison Retinue Client")]
public sealed class PrisonRetinueClient : MonoBehaviour
{
    public string apiBaseUrl = "http://127.0.0.1:5050";
    public string cityId;
    public string stableId;
    public CivilVenueNode venue;

    public void RequestSync() => StartCoroutine(Post("sync", null));
    public void RequestMerge() => StartCoroutine(Post("merge", null));
    public void RequestPrompt(string prompt) => StartCoroutine(Post("request", prompt));

    public void ApplyBundle(PersonaRequestBundle bundle)
    {
        if (bundle == null || venue == null) return;
        venue.lastBundle = bundle;
        venue.kind = CivilSystemKind.Prison;
        if (venue.retinue == null)
            venue.retinue = new List<RetinuePeckingEntry>();
        for (int i = 0; i < venue.retinue.Count; i++)
        {
            var actor = venue.retinue[i]?.actor;
            if (actor == null) continue;
            var sheet = actor.GetComponent<LifeSystemsSheet>();
            LifeSystemsGovGloveBias.ApplyBaselineBias(sheet, bundle.societyFeatures, bundle.needSatisfied01);
        }
    }

    IEnumerator Post(string action, string prompt)
    {
        if (string.IsNullOrEmpty(cityId) || string.IsNullOrEmpty(stableId))
            yield break;
        var url = $"{apiBaseUrl.TrimEnd('/')}/api/society/cities/{UnityWebRequest.EscapeURL(cityId)}/prisons/{UnityWebRequest.EscapeURL(stableId)}/retinue";
        string body = prompt != null
            ? $"{{\"action\":\"{action}\",\"prompt\":\"{Escape(prompt)}\"}}"
            : $"{{\"action\":\"{action}\"}}";
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
            yield break;
        var bundle = PersonaRequestBundle.CreateDefault(stableId, CivilSystemKind.Prison);
        bundle.cityId = cityId;
        bundle.venueStableId = stableId;
        bundle.govAgencyId = "corrections";
        ApplyBundle(bundle);
    }

    static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
}
