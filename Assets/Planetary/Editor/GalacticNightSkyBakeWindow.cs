using Planetary.Celestial;
using UnityEditor;
using UnityEngine;

namespace Planetary.Editor
{
    public sealed class GalacticNightSkyBakeWindow : EditorWindow
    {
        PlanetBody _observerPlanet;
        string _observerBodyId = "little-prince";
        float _anchorLat;
        float _anchorLon;
        float _anchorAltM;
        Transform _galacticOrigin;
        string _apiBaseUrl = "http://127.0.0.1:5050";
        GalacticBodyRegistry _registry;

        [MenuItem("Window/System Drawer/Planet/Galactic Night Sky Bake")]
        public static void Open() => GetWindow<GalacticNightSkyBakeWindow>("Galactic Night Sky Bake");

        void OnGUI()
        {
            _observerPlanet = (PlanetBody)EditorGUILayout.ObjectField("Observer Planet", _observerPlanet, typeof(PlanetBody), true);
            _observerBodyId = EditorGUILayout.TextField("Observer Body Id", _observerBodyId);
            _anchorLat = EditorGUILayout.FloatField("Anchor Lat", _anchorLat);
            _anchorLon = EditorGUILayout.FloatField("Anchor Lon", _anchorLon);
            _anchorAltM = EditorGUILayout.FloatField("Anchor Alt M", _anchorAltM);
            _galacticOrigin = (Transform)EditorGUILayout.ObjectField("Galactic Origin", _galacticOrigin, typeof(Transform), true);
            _apiBaseUrl = EditorGUILayout.TextField("API Base URL", _apiBaseUrl);
            _registry = (GalacticBodyRegistry)EditorGUILayout.ObjectField("Registry", _registry, typeof(GalacticBodyRegistry), true);

            if (GUILayout.Button("Bake From Observer"))
                BakeNow(false);
            if (GUILayout.Button("Bake And Upload"))
                BakeNow(true);
        }

        void BakeNow(bool upload)
        {
            Vector3 observer = _observerPlanet != null
                ? _observerPlanet.PlanetCenter + Vector3.up * (_observerPlanet.PlanetRadius + _anchorAltM)
                : Vector3.zero;
            var catalog = new System.Collections.Generic.List<GalacticBodyRecord>();
            if (_registry != null)
            {
                foreach (var r in _registry.AllRecords)
                    catalog.Add(r);
            }
            if (catalog.Count == 0)
            {
                catalog.Add(new GalacticBodyRecord
                {
                    bodyId = "sol",
                    kind = GalacticBodyKind.Star,
                    galacticPosition = Vector3.zero,
                    radiusM = 696340000f
                });
            }

            var record = GalacticNightSkyBakeSession.BakeEquirect(
                observer,
                _observerBodyId,
                _anchorLat,
                _anchorLon,
                _anchorAltM,
                catalog,
                _galacticOrigin);

            Debug.Log($"Baked night sky: {record.localPath} stars={record.starCount}");
            AssetDatabase.Refresh();

            if (upload)
            {
                var client = new GameObject("GalacticUploadRunner").AddComponent<GalacticUploadRunner>();
                client.apiBaseUrl = _apiBaseUrl;
                client.record = record;
            }
        }
    }

    sealed class GalacticUploadRunner : MonoBehaviour
    {
        public string apiBaseUrl;
        public GalacticNightSkyCacheRecord record;

        void Start()
        {
            StartCoroutine(GalacticNightSkyUploader.UploadCache(apiBaseUrl, record, (ok, msg) =>
            {
                Debug.Log(ok ? $"Uploaded night sky cache: {msg}" : $"Upload failed: {msg}");
                Destroy(gameObject);
            }));
        }
    }
}
