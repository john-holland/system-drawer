using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Thin client for Continuum dialogue speech synthesize/inpaint APIs (replaces local StubSoundToMLAdapter for TTS).
    /// </summary>
    public static class ContinuumDialogueSpeechClient
    {
        [Serializable]
        class SynthRequest
        {
            public string nodeId;
            public string text;
            public string voiceProfile;
            public string speakerKey;
            public string styleNotes;
        }

        [Serializable]
        public class SynthResponse
        {
            public bool ok;
            public string audioRef;
            public string speakerKey;
            public string error;
        }

        public static SynthResponse Synthesize(
            string text,
            string nodeId = null,
            string speakerKey = null,
            string voiceProfile = "default",
            string styleNotes = null,
            string baseUrl = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new SynthResponse { ok = false, error = "text required" };

            baseUrl = (baseUrl ?? ContinuumApiConfig.GetApiBaseUrl()).TrimEnd('/');
            var req = new SynthRequest
            {
                text = text,
                nodeId = nodeId,
                speakerKey = speakerKey,
                voiceProfile = voiceProfile,
                styleNotes = styleNotes
            };
            string json = JsonUtility.ToJson(req);
            using (var www = new UnityWebRequest(baseUrl + "/api/dialogue/speech/synthesize", "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(body);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SendWebRequest();
                while (!www.isDone) { }
#if UNITY_2020_2_OR_NEWER
                if (www.result != UnityWebRequest.Result.Success)
#else
                if (www.isNetworkError || www.isHttpError)
#endif
                    return new SynthResponse { ok = false, error = www.error };
                return JsonUtility.FromJson<SynthResponse>(www.downloadHandler.text);
            }
        }
    }
}
