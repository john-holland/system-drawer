using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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
            waveSamples = resp.night.waveSamples ?? System.Array.Empty<float>();
            if (sleepRenderer != null)
                sleepRenderer.SetWaveSamples(waveSamples);

            PushDreamMemory(waveSamples);
            OnNightComplete?.Invoke(lastSleepSessionId);
        }

        void PushDreamMemory(float[] samples)
        {
            if (dreamMemoryLstm == null || samples == null || samples.Length == 0)
                return;
            var bufferProp = dreamMemoryLstm.GetType().GetField("buffer");
            var buffer = bufferProp?.GetValue(dreamMemoryLstm);
            if (buffer == null)
                return;
            var push = buffer.GetType().GetMethod("PushWaveBatch");
            push?.Invoke(buffer, new object[] { samples, dayRunner != null ? dayRunner.dayCollapseSeed : 0, string.Empty });
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
        }
    }
}
