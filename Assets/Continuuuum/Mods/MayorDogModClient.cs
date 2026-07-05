using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Continuuuum.Mods
{
    /// <summary>Fetches mod bootstrap manifest from Continuuuum and caches for offline play.</summary>
    public class MayorDogModClient : MonoBehaviour
    {
        public string apiBaseUrl;
        public string userId = "player1";
        public string episodeId;
        public float reconnectPollSeconds = 30f;

        float _lastPoll;

        void Awake()
        {
            if (string.IsNullOrEmpty(apiBaseUrl))
                apiBaseUrl = ContinuuuumApiConfig.GetApiBaseUrl();
        }

        void Start()
        {
            StartCoroutine(FetchBootstrap(false));
        }

        void Update()
        {
            if (reconnectPollSeconds <= 0f) return;
            if (Time.time - _lastPoll < reconnectPollSeconds) return;
            _lastPoll = Time.time;
            if (Application.internetReachability == NetworkReachability.NotReachable) return;
            StartCoroutine(FetchBootstrap(true));
        }

        public void FetchNow()
        {
            StartCoroutine(FetchBootstrap(false));
        }

        IEnumerator FetchBootstrap(bool quiet)
        {
            var cached = MayorDogModCache.Read();
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (cached != null)
                {
                    MayorDogModApplicator.LoadFromManifest(cached);
                    if (!quiet) Debug.Log("Mayor Dog Mods: offline — using cached manifest.");
                }
                else if (!quiet)
                    Debug.Log("Mayor Dog Mods: offline with no cache — playing vanilla.");
                yield break;
            }

            var q = $"/api/mods/bootstrap?userId={UnityWebRequest.EscapeURL(userId ?? "anonymous")}";
            if (!string.IsNullOrEmpty(episodeId))
                q += $"&episodeId={UnityWebRequest.EscapeURL(episodeId)}";
            var url = $"{apiBaseUrl.TrimEnd('/')}{q}";
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("X-User-ID", userId ?? "anonymous");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var manifest = ParseManifest(req.downloadHandler.text);
                if (manifest != null)
                {
                    MayorDogModCache.Write(manifest);
                    MayorDogModApplicator.LoadFromManifest(manifest);
                    if (!quiet) Debug.Log($"Mayor Dog Mods: cached {manifest.packages?.Length ?? 0} package(s).");
                }
            }
            else if (cached != null)
            {
                MayorDogModApplicator.LoadFromManifest(cached);
                Debug.LogWarning($"Mayor Dog Mods: fetch failed ({req.error}); using cached manifest.");
            }
            else
            {
                Debug.LogWarning($"Mayor Dog Mods: fetch failed ({req.error}); no cache available.");
            }
        }

        static MayorDogModManifest ParseManifest(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var env = JsonUtility.FromJson<MayorDogModManifestEnvelope>(json);
            if (env == null) return null;
            return new MayorDogModManifest
            {
                schemaVersion = env.schemaVersion,
                cachedAt = env.cachedAt,
                episodeId = env.episodeId,
                userId = env.userId,
                packages = env.packages ?? System.Array.Empty<MayorDogModPackageDto>(),
                lemmaOverrides = env.lemmaOverrides ?? System.Array.Empty<MayorDogModOverrideDto>(),
                episodeOverrides = env.episodeOverrides ?? System.Array.Empty<MayorDogModOverrideDto>(),
            };
        }
    }
}
