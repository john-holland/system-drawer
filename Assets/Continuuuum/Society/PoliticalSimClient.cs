using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Continuuuum.Society
{
    /// <summary>Polls continuuuum society API for snapshots, city-scape, and timeline frames.</summary>
    public class PoliticalSimClient : MonoBehaviour
    {
        public string apiBaseUrl = "http://127.0.0.1:5050";
        public string cityId;
        public CityScapeProfile cityScapeProfile;
        public float pollIntervalSeconds = 5f;

        public event Action<string> OnSnapshotJson;

        float _lastPoll;

        void Update()
        {
            if (string.IsNullOrEmpty(cityId)) return;
            if (Time.time - _lastPoll < pollIntervalSeconds) return;
            _lastPoll = Time.time;
            StartCoroutine(FetchSnapshot());
        }

        public void FetchCityScapeNow()
        {
            StartCoroutine(FetchCityScape());
        }

        IEnumerator FetchSnapshot()
        {
            var url = $"{apiBaseUrl.TrimEnd('/')}/api/society/cities/{UnityWebRequest.EscapeURL(cityId)}/snapshot";
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                OnSnapshotJson?.Invoke(req.downloadHandler.text);
        }

        IEnumerator FetchCityScape()
        {
            var url = $"{apiBaseUrl.TrimEnd('/')}/api/society/cities/{UnityWebRequest.EscapeURL(cityId)}/cityscape";
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;
            var wrapper = JsonUtility.FromJson<CityScapeApiResponse>(req.downloadHandler.text);
            if (cityScapeProfile == null)
                cityScapeProfile = ScriptableObject.CreateInstance<CityScapeProfile>();
            cityScapeProfile.ImportFromDto(wrapper.profile, cityId, wrapper.version);
            var sg4d = GetComponent<SpatialGenerator4D>();
            if (sg4d != null)
                cityScapeProfile.ApplyTo(sg4d);
        }

        [Serializable]
        class CityScapeApiResponse
        {
            public int version;
            public CityScapeProfileDto profile;
        }
    }
}
