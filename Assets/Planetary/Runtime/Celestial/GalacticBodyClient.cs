using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Planetary.Celestial
{
    /// <summary>Loads galactic body registry from Continuuuum API.</summary>
    public sealed class GalacticBodyClient : MonoBehaviour
    {
        public string apiBaseUrl = "http://127.0.0.1:5050";
        public bool fetchOnEnable = true;

        public event Action<IReadOnlyList<GalacticBodyRecord>> OnBodiesLoaded;

        public void FetchBodiesNow() => StartCoroutine(FetchBodies());

        void OnEnable()
        {
            if (fetchOnEnable)
                FetchBodiesNow();
        }

        IEnumerator FetchBodies()
        {
            var url = $"{apiBaseUrl.TrimEnd('/')}/api/galactic/bodies";
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                yield break;
            var wrapper = JsonUtility.FromJson<GalacticBodiesResponse>(req.downloadHandler.text);
            if (wrapper?.items == null)
                yield break;
            var list = new List<GalacticBodyRecord>(wrapper.items.Length);
            for (int i = 0; i < wrapper.items.Length; i++)
                list.Add(wrapper.items[i].ToRecord());
            GalacticBodyRegistry.Instance?.LoadFromApi(list);
            OnBodiesLoaded?.Invoke(list);
        }

        [Serializable]
        class GalacticBodiesResponse
        {
            public GalacticBodyDto[] items;
        }

        [Serializable]
        public class GalacticBodyDto
        {
            public string bodyId;
            public string kind;
            public string displayName;
            public double galacticX;
            public double galacticY;
            public double galacticZ;
            public double massKg;
            public float radiusM;
            public float radiationLevel;
            public float gravityWellStrength;
            public string societyPlanetId;
            public string uscAssetId;
            public string scenePrefabRef;
            public string lemmaColorId;
            public string lemmaVisibilityId;
            public bool immovable;

            public GalacticBodyRecord ToRecord() => new GalacticBodyRecord
            {
                bodyId = bodyId,
                kind = GalacticBodyRecord.ParseKind(kind),
                displayName = displayName,
                galacticPosition = new Vector3((float)galacticX, (float)galacticY, (float)galacticZ),
                massKg = massKg,
                radiusM = radiusM,
                radiationLevel = radiationLevel,
                gravityWellStrength = gravityWellStrength,
                societyPlanetId = societyPlanetId,
                uscAssetId = uscAssetId,
                scenePrefabRef = scenePrefabRef,
                lemmaColorId = lemmaColorId,
                lemmaVisibilityId = lemmaVisibilityId,
                immovable = immovable
            };
        }
    }
}
