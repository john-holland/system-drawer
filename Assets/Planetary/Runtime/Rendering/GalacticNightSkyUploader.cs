using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Planetary.Celestial
{
    public static class GalacticNightSkyUploader
    {
        [System.Serializable]
        class CachePostBody
        {
            public string cacheId;
            public string observerBodyId;
            public float anchorLat;
            public float anchorLon;
            public float anchorAltM;
            public string localPath;
            public int starCount;
            public int bakeVersion = 1;
        }

        public static IEnumerator UploadCache(
            string apiBaseUrl,
            GalacticNightSkyCacheRecord record,
            System.Action<bool, string> onComplete)
        {
            var body = new CachePostBody
            {
                cacheId = record.cacheId,
                observerBodyId = record.observerBodyId,
                anchorLat = record.anchorLat,
                anchorLon = record.anchorLon,
                anchorAltM = record.anchorAltM,
                localPath = record.localPath,
                starCount = record.starCount
            };
            string json = JsonUtility.ToJson(body);
            var url = $"{apiBaseUrl.TrimEnd('/')}/api/galactic/night-sky/caches";
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            bool ok = req.result == UnityWebRequest.Result.Success;
            onComplete?.Invoke(ok, ok ? req.downloadHandler.text : req.error);
        }
    }
}
