using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Server-authoritative dialogue runner: Cave/API session open, choose, advance, audio via ActorSpeechPlayback.
    /// </summary>
    public sealed class DialogueRunner : MonoBehaviour
    {
        [Header("Session")]
        public string setId = "book-concert";
        public string continuuuumBaseUrl;

        [Header("Context")]
        public NarrativeExecutor executor;
        public NarrativeBindings bindings;

        [Header("Playback")]
        public bool waitForAudio = true;
        public float volume = 1f;

        string _sessionId;
        DialogueNodeView _currentNode;
        SpeechPlaybackHandle _playback;
        bool _running;

        public string SessionId => _sessionId;
        public DialogueNodeView CurrentNode => _currentNode;

        void Awake()
        {
            if (executor == null)
                executor = FindAnyObjectByType<NarrativeExecutor>();
            if (bindings == null)
                bindings = FindAnyObjectByType<NarrativeBindings>();
            if (string.IsNullOrEmpty(continuuuumBaseUrl))
                continuuuumBaseUrl = ContinuuuumApiConfig.GetApiBaseUrl();
        }

        public void OpenSession(Action<DialogueSessionResponse> onComplete = null)
        {
            StartCoroutine(OpenSessionCoroutine(onComplete));
        }

        public void Choose(string answerId, Action<DialogueSessionResponse> onComplete = null)
        {
            StartCoroutine(ChooseCoroutine(answerId, onComplete));
        }

        public void Advance(Action<DialogueSessionResponse> onComplete = null)
        {
            StartCoroutine(AdvanceCoroutine(onComplete));
        }

        public void SyncGoals(System.Collections.Generic.Dictionary<string, bool> goals, Action<DialogueSessionResponse> onComplete = null)
        {
            StartCoroutine(SyncGoalsCoroutine(goals, onComplete));
        }

        IEnumerator OpenSessionCoroutine(Action<DialogueSessionResponse> onComplete)
        {
            string body = JsonUtility.ToJson(new DialogueOpenRequest { setId = setId, traceId = Guid.NewGuid().ToString("N") });
            yield return PostJson("/api/dialogue/session/open", body, resp =>
            {
                ApplySession(resp);
                onComplete?.Invoke(resp);
            });
        }

        IEnumerator ChooseCoroutine(string answerId, Action<DialogueSessionResponse> onComplete)
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                onComplete?.Invoke(null);
                yield break;
            }
            string body = JsonUtility.ToJson(new DialogueChooseRequest { answerId = answerId });
            yield return PostJson($"/api/dialogue/session/{_sessionId}/choose", body, resp =>
            {
                ApplySession(resp);
                onComplete?.Invoke(resp);
            });
        }

        IEnumerator AdvanceCoroutine(Action<DialogueSessionResponse> onComplete)
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                onComplete?.Invoke(null);
                yield break;
            }
            yield return PostJson($"/api/dialogue/session/{_sessionId}/advance", "{}", resp =>
            {
                ApplySession(resp);
                onComplete?.Invoke(resp);
            });
        }

        IEnumerator SyncGoalsCoroutine(System.Collections.Generic.Dictionary<string, bool> goals, Action<DialogueSessionResponse> onComplete)
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                onComplete?.Invoke(null);
                yield break;
            }
            var req = new DialogueGoalsRequest { goals = DialogueGoalsRequest.FromDict(goals) };
            yield return PostJson($"/api/dialogue/session/{_sessionId}/goals/sync", JsonUtility.ToJson(req), resp =>
            {
                ApplySession(resp);
                onComplete?.Invoke(resp);
            });
        }

        void ApplySession(DialogueSessionResponse resp)
        {
            if (resp == null || !resp.ok)
                return;
            _sessionId = resp.sessionId;
            _currentNode = resp.currentNode;
            if (_currentNode != null && (_currentNode.presentation == "audio" || !string.IsNullOrEmpty(_currentNode.audioRef)))
                PlayCurrentLine();
        }

        public void PlayCurrentLine()
        {
            if (_currentNode == null)
                return;
            ApplyVoiceActorLine(_currentNode);
            var ctx = BuildContext();
            AudioClip clip = null;
            if (!string.IsNullOrEmpty(_currentNode.audioRef))
                clip = DialogueAudioLoader.LoadSync(_currentNode.audioRef, continuuuumBaseUrl);
            if (clip == null && !string.IsNullOrEmpty(_currentNode.text))
            {
                Debug.Log("[DialogueRunner] Line (no audio): " + _currentNode.text);
                return;
            }
            if (clip == null)
                return;
            _playback = ActorSpeechPlayback.Play(
                ctx,
                _currentNode.speakerKey,
                clip,
                ActorSpeechPlayback.ParseVisMode(_currentNode.visMode),
                volume);
        }

        void ApplyVoiceActorLine(DialogueNodeView node)
        {
            if (node == null)
                return;
            if (string.IsNullOrEmpty(node.dialogActorId) && node.kind != "voice_actor_line")
                return;
            var lines = FindObjectsByType<VoiceActorLineComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line == null)
                    continue;
                bool actorMatch = string.IsNullOrEmpty(node.dialogActorId) || line.dialogActorId == node.dialogActorId;
                if (!actorMatch)
                    continue;
                if (!string.IsNullOrEmpty(node.audioRef))
                    line.uscAudioId = node.audioRef;
                line.charStart = node.charStart;
                line.charEnd = node.charEnd;
                line.quoteText = node.text;
                if (string.IsNullOrEmpty(node.audioRef) && !string.IsNullOrEmpty(line.uscAudioId))
                    node.audioRef = line.uscAudioId;
                if (string.IsNullOrEmpty(node.speakerKey) && !string.IsNullOrEmpty(line.dialogActorId))
                    node.speakerKey = line.dialogActorId;
                break;
            }
        }

        public bool IsAudioPlaying() => _playback != null && _playback.IsPlaying;

        NarrativeExecutionContext BuildContext()
        {
            if (executor != null)
                return new NarrativeExecutionContext(executor.clock, bindings ?? executor.bindings, null);
            return new NarrativeExecutionContext(null, bindings, null);
        }

        IEnumerator PostJson(string path, string json, Action<DialogueSessionResponse> onDone)
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
                    Debug.LogWarning("[DialogueRunner] Request failed: " + req.error);
                    onDone?.Invoke(null);
                    yield break;
                }
                onDone?.Invoke(JsonUtility.FromJson<DialogueSessionResponse>(req.downloadHandler.text));
            }
        }

        public void LoadCompiledLocal(string lemmaText, string defaultSetId = null)
        {
            var compiled = DialogueSpanParser.Compile(lemmaText, defaultSetId ?? setId);
            if (compiled.nodes.Count > 0)
            {
                _currentNode = DialogueNodeView.FromDto(compiled.nodes[0]);
                setId = compiled.setId;
            }
        }
    }

    [Serializable]
    public class DialogueSessionResponse
    {
        public bool ok;
        public string sessionId;
        public string setId;
        public DialogueNodeView currentNode;
        public DialogueChoiceView[] choices;
    }

    [Serializable]
    public class DialogueNodeView
    {
        public string id;
        public string text;
        public string presentation;
        public string answerId;
        public string speakerKey;
        public string visMode;
        public string audioRef;
        public string goal;
        public string kind;
        public string dialogActorId;
        public int charStart;
        public int charEnd;
        public float seconds;

        public static DialogueNodeView FromDto(DialogueNodeDto dto)
        {
            if (dto == null) return null;
            return new DialogueNodeView
            {
                id = dto.id,
                text = dto.text,
                presentation = dto.presentation,
                answerId = dto.answerId,
                speakerKey = dto.speakerKey,
                visMode = dto.visMode,
                audioRef = dto.audioRef,
                goal = dto.goal,
                kind = dto.kind,
                dialogActorId = dto.dialogActorId,
                charStart = dto.charStart,
                charEnd = dto.charEnd,
                seconds = dto.seconds
            };
        }
    }

    [Serializable]
    public class DialogueChoiceView
    {
        public string answerId;
        public string text;
        public string nodeId;
        public string speakerKey;
        public string presentation;
        public string audioRef;
        public string visMode;
    }

    [Serializable]
    class DialogueOpenRequest
    {
        public string setId;
        public string traceId;
    }

    [Serializable]
    class DialogueChooseRequest
    {
        public string answerId;
    }

    [Serializable]
    class DialogueGoalsRequest
    {
        public GoalEntry[] goals;

        public static GoalEntry[] FromDict(System.Collections.Generic.Dictionary<string, bool> dict)
        {
            if (dict == null) return Array.Empty<GoalEntry>();
            var list = new System.Collections.Generic.List<GoalEntry>();
            foreach (var kv in dict)
                list.Add(new GoalEntry { key = kv.Key, value = kv.Value });
            return list.ToArray();
        }
    }

    [Serializable]
    class GoalEntry
    {
        public string key;
        public bool value;
    }
}
