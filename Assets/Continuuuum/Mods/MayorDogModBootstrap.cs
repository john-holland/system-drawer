using UnityEngine;

namespace Continuuuum.Mods
{
    /// <summary>Scene bootstrap for Mayor Dog Mod cache (DontDestroyOnLoad).</summary>
    [DefaultExecutionOrder(-100)]
    public class MayorDogModBootstrap : MonoBehaviour
    {
        public static MayorDogModBootstrap Instance { get; private set; }

        public string userId = "player1";
        public string episodeId;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var client = GetComponent<MayorDogModClient>();
            if (client == null)
                client = gameObject.AddComponent<MayorDogModClient>();
            client.userId = userId;
            client.episodeId = episodeId;

            var cached = MayorDogModCache.Read();
            if (cached != null)
                MayorDogModApplicator.LoadFromManifest(cached);
        }
    }
}
