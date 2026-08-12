using System.Collections;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>
    /// Registers as Continuuuum <see cref="IDimensionalOpenCloseRunner"/> and bakes
    /// <see cref="OpenCloseTopologyAsset"/> stops for dimensional enter/exit.
    /// </summary>
    public sealed class DimensionalOpenCloseRunner : MonoBehaviour, IDimensionalOpenCloseRunner
    {
        const string BakeChildName = "DimensionalOpenCloseBt";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterStatic()
        {
            DimensionalOpenCloseRunnerHost.Instance = new StaticRunner();
        }

        sealed class StaticRunner : IDimensionalOpenCloseRunner
        {
            public void Begin(GameObject host, ScriptableObject topologyAsset, bool entering, int runtimeMilliseconds)
            {
                if (host == null || topologyAsset == null)
                    return;
                var topology = topologyAsset as OpenCloseTopologyAsset;
                if (topology == null)
                {
                    Debug.LogWarning(
                        $"[DimensionalOpenCloseRunner] Expected OpenCloseTopologyAsset, got {topologyAsset.GetType().Name}");
                    return;
                }

                var runner = host.GetComponent<DimensionalOpenCloseRunner>()
                             ?? host.AddComponent<DimensionalOpenCloseRunner>();
                runner.BeginInternal(topology, entering, runtimeMilliseconds);
            }
        }

        public void Begin(GameObject host, ScriptableObject topologyAsset, bool entering, int runtimeMilliseconds)
        {
            // Interface path when component is the registered instance.
            if (topologyAsset is OpenCloseTopologyAsset topology)
                BeginInternal(topology, entering, runtimeMilliseconds);
        }

        void BeginInternal(OpenCloseTopologyAsset topology, bool entering, int runtimeMilliseconds)
        {
            Transform bakeParent = transform.Find(BakeChildName);
            if (bakeParent == null)
            {
                var go = new GameObject(BakeChildName);
                go.transform.SetParent(transform, false);
                bakeParent = go.transform;
            }

            var lemma = OpenCloseLemmaProperties.Defaults;
            if (!entering)
            {
                // Close path: prefer compileCloseAmbulation / reverse stack semantics from asset.
                lemma.compileCloseAmbulation = true;
            }

            OpenCloseTopologyBtBuilder.Bake(
                bakeParent,
                topology,
                lemma,
                actor: transform,
                clearChildren: true);

            StopAllCoroutines();
            if (runtimeMilliseconds >= 0)
                StartCoroutine(TeardownAfterMs(bakeParent, runtimeMilliseconds));
        }

        static IEnumerator TeardownAfterMs(Transform bakeParent, int runtimeMilliseconds)
        {
            float seconds = Mathf.Max(0f, runtimeMilliseconds / 1000f);
            if (seconds > 0f)
                yield return new WaitForSeconds(seconds);
            OpenCloseTopologyBtBuilder.ClearChildren(bakeParent);
        }
    }
}
