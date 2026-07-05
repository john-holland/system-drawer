using System;
using System.Collections;
using System.Collections.Generic;
using Continuuuum.Society;
using Locomotion.Narrative;
using UnityEngine;
using UnityEngine.Networking;

namespace Continuuuum.Society.Narrative
{
    [System.Serializable]
    public class RunCityRoutingAction : NarrativeActionSpec
    {
        public string cityId;
        public CityBehaviorTreeAsset routingAsset;
        public string apiBaseUrl = "http://127.0.0.1:5050";

        [NonSerialized] int _visitIndex;

        public override bool SupportsUndo => false;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (routingAsset == null || routingAsset.visitOrder.Count == 0)
                return BehaviorTreeStatus.Failure;

            if (_visitIndex >= routingAsset.visitOrder.Count)
                return BehaviorTreeStatus.Success;

            var visit = routingAsset.visitOrder[_visitIndex++];
            Debug.Log($"[RunCityRoutingAction] Visit {visit.stableId} ({visit.displayName})");
            return _visitIndex >= routingAsset.visitOrder.Count
                ? BehaviorTreeStatus.Success
                : BehaviorTreeStatus.Running;
        }

        public void Reset()
        {
            _visitIndex = 0;
        }
    }

    [System.Serializable]
    public class AdjustPoliticalCadenceAction : NarrativeActionSpec
    {
        public string cityId;
        public float solverCadenceNarrativeSeconds = 3600f;
        public string apiBaseUrl = "http://127.0.0.1:5050";

        public override bool SupportsUndo => false;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;
            var runner = UnityEngine.Object.FindAnyObjectByType<PoliticalCadenceRunner>();
            if (runner == null)
            {
                var go = new GameObject("PoliticalCadenceRunner");
                runner = go.AddComponent<PoliticalCadenceRunner>();
            }
            runner.StartPatch(cityId, apiBaseUrl, solverCadenceNarrativeSeconds);
            return BehaviorTreeStatus.Success;
        }
    }

    public class PoliticalCadenceRunner : MonoBehaviour
    {
        public void StartPatch(string cityId, string apiBase, float seconds)
        {
            StartCoroutine(PatchCadence(cityId, apiBase, seconds));
        }

        IEnumerator PatchCadence(string cityId, string apiBase, float seconds)
        {
            var url = $"{apiBase.TrimEnd('/')}/api/society/cities/{UnityWebRequest.EscapeURL(cityId)}/cadence";
            var json = JsonUtility.ToJson(new CadenceBody { solverCadenceNarrativeSeconds = seconds });
            using var req = new UnityWebRequest(url, "PATCH");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            Destroy(gameObject);
        }

        [System.Serializable]
        class CadenceBody
        {
            public float solverCadenceNarrativeSeconds;
        }
    }
}
