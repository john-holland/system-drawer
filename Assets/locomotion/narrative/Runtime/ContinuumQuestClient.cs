using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Locomotion.Narrative
{
    /// <summary>HTTP client for quest compile and spatial-nodes API.</summary>
    public static class ContinuumQuestClient
    {
        public static IEnumerator Compile(string text, string setId, Action<QuestCompileResponse> onDone)
        {
            string baseUrl = ContinuumApiConfig.GetApiBaseUrl().TrimEnd('/');
            var payload = new QuestCompileRequest { text = text, setId = setId };
            yield return Post(baseUrl + "/api/quest/compile", JsonUtility.ToJson(payload), onDone);
        }

        public static IEnumerator FetchSpatialNodes(string spatial4dId, float narrativeT, Action<QuestSpatialNodesResponse> onDone)
        {
            string baseUrl = ContinuumApiConfig.GetApiBaseUrl().TrimEnd('/');
            string url = baseUrl + "/api/quest/spatial-nodes?spatial4dId=" + Uri.EscapeDataString(spatial4dId ?? "");
            if (narrativeT >= 0f)
                url += "&narrativeT=" + narrativeT.ToString("F3");
            using (var req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    onDone?.Invoke(null);
                    yield break;
                }
                onDone?.Invoke(JsonUtility.FromJson<QuestSpatialNodesResponse>(req.downloadHandler.text));
            }
        }

        static IEnumerator Post<T>(string url, string json, Action<T> onDone) where T : class
        {
            using (var req = new UnityWebRequest(url, "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json ?? "{}");
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    onDone?.Invoke(null);
                    yield break;
                }
                onDone?.Invoke(JsonUtility.FromJson<T>(req.downloadHandler.text));
            }
        }
    }

    [Serializable]
    public class QuestCompileRequest
    {
        public string text;
        public string setId;
    }

    [Serializable]
    public class QuestCompileResponse
    {
        public bool ok;
    }

    [Serializable]
    public class QuestSpatialNodesResponse
    {
        public bool ok;
        public QuestSpatialNodeView[] nodes;
    }

    [Serializable]
    public class QuestSpatialNodeView
    {
        public string id;
        public string label;
        public string source;
        public string spatial4dId;
    }
}
