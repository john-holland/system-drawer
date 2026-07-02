using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SystemDrawer.DreamCycle
{
    /// <summary>Polls dream-cycle day API and drives NeedAspectSpatialSlot generators.</summary>
    public sealed class DreamDayCycleRunner : MonoBehaviour
    {
        public string apiBaseUrl = "http://127.0.0.1:5050";
        public string cityId = "earth-city";
        public string actorId;
        [TextArea(2, 6)] public string dayPrompt;
        public string optionalScriptPrompt;
        public NeedAspectRegistry registry;
        public NeedAspectSpatialSlot[] slots = Array.Empty<NeedAspectSpatialSlot>();
        public MonoBehaviour narrativePromptInterpreter;
        [Tooltip("Optional SpatialGenerator4DOrchestrator (loose) for local quad collapse.")]
        public MonoBehaviour spatialOrchestrator;
        public int dayCollapseSeed;
        public string lastSessionId;

        public event Action<int> OnDayCollapseSeed;

        public void RunDayComplete() => StartCoroutine(CompleteDayCoroutine());

        void TryLocalQuadCollapse()
        {
            if (spatialOrchestrator == null)
                return;
            var collapseType = System.Type.GetType("BedogaGenerator.DreamCycle.QuadTreeDayCollapse, BedogaGenerator");
            if (collapseType == null)
                return;
            var method = collapseType.GetMethod("CollapseFromOrchestrator", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null)
                return;
            var result = method.Invoke(null, new object[] { spatialOrchestrator });
            if (result == null)
                return;
            var seedField = result.GetType().GetField("dayCollapseSeed");
            if (seedField != null)
            {
                dayCollapseSeed = (int)seedField.GetValue(result);
                var orchSeed = spatialOrchestrator.GetType().GetField("dayCollapseSeed");
                orchSeed?.SetValue(spatialOrchestrator, dayCollapseSeed);
            }
        }

        IEnumerator CompleteDayCoroutine()
        {
            string prompt = dayPrompt;
            if (!string.IsNullOrEmpty(optionalScriptPrompt))
                prompt = optionalScriptPrompt + "\n" + dayPrompt;

            var body = new DayCompleteRequest
            {
                cityId = cityId,
                dayPrompt = prompt,
                actorId = actorId,
                persist = true
            };
            string json = JsonUtility.ToJson(body);
            var url = $"{apiBaseUrl.TrimEnd('/')}/api/dream-cycle/day/complete";
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                yield break;

            var resp = JsonUtility.FromJson<DayCompleteResponse>(req.downloadHandler.text);
            if (resp?.session == null)
                yield break;
            lastSessionId = resp.session.sessionId;
            dayCollapseSeed = resp.session.dayCollapseSeed;
            ApplyAspectStates(resp.session.aspectStates);
            TryLocalQuadCollapse();
            OnDayCollapseSeed?.Invoke(dayCollapseSeed);
        }

        void ApplyAspectStates(DayAspectStateDto[] states)
        {
            if (states == null)
                return;
            for (int i = 0; i < states.Length; i++)
            {
                var slot = FindSlot(states[i].aspectId);
                slot?.ApplyDayState(states[i]);
            }
        }

        NeedAspectSpatialSlot FindSlot(string aspectId)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].aspectId == aspectId)
                    return slots[i];
            }
            return null;
        }

        [Serializable]
        class DayCompleteRequest
        {
            public string cityId;
            public string dayPrompt;
            public string actorId;
            public bool persist = true;
        }

        [Serializable]
        class DayCompleteResponse
        {
            public bool ok;
            public DaySessionDto session;
        }

        [Serializable]
        class DaySessionDto
        {
            public string sessionId;
            public int dayCollapseSeed;
            public DayAspectStateDto[] aspectStates;
        }
    }
}
