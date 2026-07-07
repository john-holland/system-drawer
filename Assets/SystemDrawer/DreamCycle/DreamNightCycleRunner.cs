using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Locomotion.DreamCycle;

namespace SystemDrawer.DreamCycle
{
    /// <summary>Completes dream-cycle night via REST and feeds SleepWaveStatRenderer.</summary>
    public sealed class DreamNightCycleRunner : MonoBehaviour
    {
        public string apiBaseUrl = "http://127.0.0.1:5050";
        public DreamDayCycleRunner dayRunner;
        public SleepWaveStatRenderer sleepRenderer;
        [SerializeField] MonoBehaviour dreamMemoryLstm;
        public string lastSleepSessionId;
        public int sleepSeed;
        public bool wakeFromNestedDream;
        public float[] waveSamples = System.Array.Empty<float>();

        public event System.Action<string> OnNightComplete;

        public void RunNightComplete() => StartCoroutine(CompleteNightCoroutine());

        IEnumerator CompleteNightCoroutine()
        {
            if (dayRunner == null || string.IsNullOrEmpty(dayRunner.lastSessionId))
                yield break;

            var body = new NightCompleteRequest
            {
                sessionId = dayRunner.lastSessionId,
                persist = true
            };
            string json = JsonUtility.ToJson(body);
            var url = $"{apiBaseUrl.TrimEnd('/')}/api/dream-cycle/night/complete";
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                yield break;

            var resp = JsonUtility.FromJson<NightCompleteResponse>(req.downloadHandler.text);
            if (resp?.night == null)
                yield break;

            lastSleepSessionId = resp.night.sleepSessionId;
            sleepSeed = resp.night.sleepSeed;
            wakeFromNestedDream = resp.night.wakeFromNestedDream;
            waveSamples = resp.night.waveSamples ?? System.Array.Empty<float>();
            if (sleepRenderer != null)
                sleepRenderer.SetWaveSamples(waveSamples);

            PushDreamMemory(waveSamples, wakeFromNestedDream);
            OnNightComplete?.Invoke(lastSleepSessionId);
        }

        void PushDreamMemory(float[] samples, bool nestedDream)
        {
            if (dreamMemoryLstm == null || samples == null || samples.Length == 0)
                return;
            var bufferProp = dreamMemoryLstm.GetType().GetField("buffer");
            var buffer = bufferProp?.GetValue(dreamMemoryLstm) as DreamMemoryBuffer;
            if (buffer == null)
                return;

            int dreamSeed = dayRunner != null ? dayRunner.dreamDayCollapseSeed : 0;
            int goodSeed = dayRunner != null ? dayRunner.goodDayCollapseSeed : 0;
            if (dreamSeed == 0 && dayRunner != null)
                dreamSeed = dayRunner.dayCollapseSeed;

            buffer.PushWaveBatch(
                samples,
                dreamSeed,
                string.Empty,
                goodSeed,
                nestedDream ? DreamMemoryLayer.DeveloperDream : DreamMemoryLayer.SingleDay,
                remOnly: nestedDream);
        }

        [System.Serializable]
        class NightCompleteRequest
        {
            public string sessionId;
            public bool persist = true;
        }

        [System.Serializable]
        class NightCompleteResponse
        {
            public bool ok;
            public NightSessionDto night;
        }

        [System.Serializable]
        class NightSessionDto
        {
            public string sleepSessionId;
            public int sleepSeed;
            public float[] waveSamples;
            public bool wakeFromNestedDream;
        }
    }
}
