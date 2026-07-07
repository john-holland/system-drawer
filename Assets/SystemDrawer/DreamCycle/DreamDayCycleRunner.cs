using System;
using System.Collections;
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
        public DreamDaySimulationProfile profile;
        public NeedAspectRegistry registry;
        public NeedAspectSpatialSlot[] slots = Array.Empty<NeedAspectSpatialSlot>();
        public MonoBehaviour narrativePromptInterpreter;
        [Tooltip("Optional SpatialGenerator4DOrchestrator (loose) for local quad collapse.")]
        public MonoBehaviour spatialOrchestrator;
        public int dayCollapseSeed;
        public int goodDayCollapseSeed;
        public int dreamDayCollapseSeed;
        public string lastSessionId;
        public string lastOuterSessionId;
        public bool lastRunWasDoubleDay;

        public event Action<int> OnDayCollapseSeed;
        public event Action<int> OnGoodDayHorizonComplete;
        public event Action<int> OnDreamDayComplete;

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
            bool doubleDay = profile != null && profile.doubleDayEnabled;
            string dreamPrompt = profile != null && !string.IsNullOrEmpty(profile.dreamDayPrompt)
                ? profile.dreamDayPrompt
                : dayPrompt;
            if (!string.IsNullOrEmpty(optionalScriptPrompt))
                dreamPrompt = optionalScriptPrompt + "\n" + dreamPrompt;

            DayCompleteRequest body;
            if (doubleDay)
            {
                var horizon = profile.goodDayHorizon;
                body = new DayCompleteRequest
                {
                    cityId = cityId,
                    doubleDay = true,
                    dreamDayPrompt = dreamPrompt,
                    actorId = actorId,
                    persist = true,
                    goodDayHorizon = new GoodDayHorizonDto
                    {
                        minSatisfied = horizon.minSatisfied,
                        maxSatisfied = horizon.maxSatisfied,
                        blendSocietyWeight = horizon.blendSocietyWeight
                    }
                };
            }
            else
            {
                body = new DayCompleteRequest
                {
                    cityId = cityId,
                    dayPrompt = dreamPrompt,
                    actorId = actorId,
                    persist = true
                };
            }

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

            var session = resp.session;
            lastSessionId = session.sessionId;
            lastOuterSessionId = session.outerSessionId;
            lastRunWasDoubleDay = session.doubleDay;
            goodDayCollapseSeed = session.goodDayCollapseSeed;
            dreamDayCollapseSeed = session.dreamDayCollapseSeed > 0
                ? session.dreamDayCollapseSeed
                : session.dayCollapseSeed;
            dayCollapseSeed = dreamDayCollapseSeed;

            if (lastRunWasDoubleDay)
            {
                OnGoodDayHorizonComplete?.Invoke(goodDayCollapseSeed);
                OnDreamDayComplete?.Invoke(dreamDayCollapseSeed);
            }

            ApplyAspectStates(session.aspectStates);
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
            public string dreamDayPrompt;
            public string actorId;
            public bool persist = true;
            public bool doubleDay;
            public GoodDayHorizonDto goodDayHorizon;
        }

        [Serializable]
        class GoodDayHorizonDto
        {
            public float minSatisfied;
            public float maxSatisfied;
            public float blendSocietyWeight;
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
            public string outerSessionId;
            public int dayCollapseSeed;
            public int goodDayCollapseSeed;
            public int dreamDayCollapseSeed;
            public bool doubleDay;
            public DayAspectStateDto[] aspectStates;
        }
    }
}
