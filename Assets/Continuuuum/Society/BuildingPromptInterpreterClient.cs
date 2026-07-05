using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Continuuuum.Society
{
    /// <summary>Calls /api/society/buildings/{id}/prompt for sync/merge/request.</summary>
    public class BuildingPromptInterpreterClient : MonoBehaviour
    {
        public string apiBaseUrl = "http://127.0.0.1:5050";
        public string cityId;
        public string stableId;

        public void RequestSync()
        {
            StartCoroutine(PostPrompt("sync", null));
        }

        public void RequestPrompt(string prompt)
        {
            StartCoroutine(PostPrompt("request", prompt));
        }

        IEnumerator PostPrompt(string action, string prompt)
        {
            var url = $"{apiBaseUrl.TrimEnd('/')}/api/society/cities/{UnityWebRequest.EscapeURL(cityId)}/buildings/{UnityWebRequest.EscapeURL(stableId)}/prompt";
            var body = prompt != null
                ? $"{{\"action\":\"{action}\",\"prompt\":\"{prompt}\"}}"
                : $"{{\"action\":\"{action}\"}}";
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                Debug.Log($"[BuildingPromptInterpreter] {req.downloadHandler.text}");
        }
    }
}
