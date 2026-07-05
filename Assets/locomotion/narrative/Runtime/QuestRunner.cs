using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Locomotion.Narrative
{
    /// <summary>Server-authoritative quest session client.</summary>
    public sealed class QuestRunner : MonoBehaviour
    {
        public string setId = "little-prince-tour";
        public string continuuuumBaseUrl;
        public NarrativeExecutor executor;
        public NarrativeBindings bindings;

        string _sessionId;
        QuestObjectiveView _activeObjective;

        public event Action<QuestObjectiveView> OnObjectiveActivated;
        public event Action<QuestObjectiveView> OnObjectiveCompleted;
        public event Action OnMapDirty;

        public string SessionId => _sessionId;
        public QuestObjectiveView ActiveObjective => _activeObjective;

        void Awake()
        {
            if (executor == null)
                executor = FindAnyObjectByType<NarrativeExecutor>();
            if (bindings == null)
                bindings = FindAnyObjectByType<NarrativeBindings>();
            if (string.IsNullOrEmpty(continuuuumBaseUrl))
                continuuuumBaseUrl = ContinuuuumApiConfig.GetApiBaseUrl();
        }

        public void OpenQuestSet(Action<QuestSessionResponse> onComplete = null) =>
            StartCoroutine(OpenCoroutine(onComplete));

        public void ActivateObjective(string objectiveId, Action<QuestSessionResponse> onComplete = null) =>
            StartCoroutine(ActivateCoroutine(objectiveId, onComplete));

        public void CompleteObjective(string objectiveId, Action<QuestSessionResponse> onComplete = null) =>
            StartCoroutine(CompleteCoroutine(objectiveId, onComplete));

        public void SyncGoals(Dictionary<string, bool> goals, Action<QuestSessionResponse> onComplete = null) =>
            StartCoroutine(SyncGoalsCoroutine(goals, onComplete));

        IEnumerator OpenCoroutine(Action<QuestSessionResponse> onComplete)
        {
            string body = JsonUtility.ToJson(new QuestOpenRequest { setId = setId, traceId = Guid.NewGuid().ToString("N") });
            yield return PostJson("/api/quest/session/open", body, resp =>
            {
                ApplySession(resp);
                onComplete?.Invoke(resp);
            });
        }

        IEnumerator ActivateCoroutine(string objectiveId, Action<QuestSessionResponse> onComplete)
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                onComplete?.Invoke(null);
                yield break;
            }
            string body = JsonUtility.ToJson(new QuestObjectiveRequest { objectiveId = objectiveId, sessionId = _sessionId });
            yield return PostJson($"/api/quest/session/{_sessionId}/objective/activate", body, resp =>
            {
                ApplySession(resp);
                OnObjectiveActivated?.Invoke(_activeObjective);
                OnMapDirty?.Invoke();
                onComplete?.Invoke(resp);
            });
        }

        IEnumerator CompleteCoroutine(string objectiveId, Action<QuestSessionResponse> onComplete)
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                onComplete?.Invoke(null);
                yield break;
            }
            string body = JsonUtility.ToJson(new QuestObjectiveRequest { objectiveId = objectiveId, sessionId = _sessionId });
            yield return PostJson($"/api/quest/session/{_sessionId}/objective/complete", body, resp =>
            {
                var prev = _activeObjective;
                ApplySession(resp);
                OnObjectiveCompleted?.Invoke(prev);
                OnMapDirty?.Invoke();
                onComplete?.Invoke(resp);
            });
        }

        IEnumerator SyncGoalsCoroutine(Dictionary<string, bool> goals, Action<QuestSessionResponse> onComplete)
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                onComplete?.Invoke(null);
                yield break;
            }
            var req = new QuestGoalsRequest { goals = QuestGoalsRequest.FromDict(goals) };
            yield return PostJson($"/api/quest/session/{_sessionId}/goals/sync", JsonUtility.ToJson(req), resp =>
            {
                ApplySession(resp);
                onComplete?.Invoke(resp);
            });
        }

        void ApplySession(QuestSessionResponse resp)
        {
            if (resp == null || !resp.ok)
                return;
            _sessionId = resp.sessionId;
            _activeObjective = resp.activeObjective;
        }

        IEnumerator PostJson(string path, string json, Action<QuestSessionResponse> onDone)
        {
            string url = continuuuumBaseUrl.TrimEnd('/') + path;
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
                    Debug.LogWarning("[QuestRunner] Request failed: " + req.error);
                    onDone?.Invoke(null);
                    yield break;
                }
                onDone?.Invoke(JsonUtility.FromJson<QuestSessionResponse>(req.downloadHandler.text));
            }
        }

        public bool EvaluatePredicate(QuestNodeDto node, Dictionary<string, bool> goalFlags, List<string> completions4d)
        {
            if (node == null)
                return true;
            if (!string.IsNullOrEmpty(node.predicate4d) &&
                (goalFlags == null || !goalFlags.TryGetValue(node.predicate4d, out bool v) || !v))
                return false;
            if (!string.IsNullOrEmpty(node.completion4d) &&
                (completions4d == null || !completions4d.Contains(node.completion4d)))
                return false;
            return true;
        }
    }

    [Serializable]
    public class QuestSessionResponse
    {
        public bool ok;
        public string sessionId;
        public string setId;
        public string title;
        public QuestObjectiveView activeObjective;
    }

    [Serializable]
    public class QuestObjectiveView
    {
        public string id;
        public string objectiveId;
        public string text;
        public string summary;
        public string spatial4dId;
        public string predicate4d;
        public string completion4d;
        public string travelBinding;
        public string mapLayer;
    }

    [Serializable]
    class QuestOpenRequest
    {
        public string setId;
        public string traceId;
    }

    [Serializable]
    class QuestObjectiveRequest
    {
        public string sessionId;
        public string objectiveId;
    }

    [Serializable]
    class QuestGoalsRequest
    {
        public QuestGoalEntry[] goals;

        public static QuestGoalEntry[] FromDict(Dictionary<string, bool> dict)
        {
            if (dict == null) return Array.Empty<QuestGoalEntry>();
            var list = new List<QuestGoalEntry>();
            foreach (var kv in dict)
                list.Add(new QuestGoalEntry { key = kv.Key, value = kv.Value });
            return list.ToArray();
        }
    }

    [Serializable]
    class QuestGoalEntry
    {
        public string key;
        public bool value;
    }
}
