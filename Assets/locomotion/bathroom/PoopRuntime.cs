using UnityEngine;

/// <summary>Parameterized stool: wetness, smell, texture; SDF scale and/or rope-style coil in bowl.</summary>
[AddComponentMenu("Locomotion/Bathroom/Poop Runtime")]
public sealed class PoopRuntime : MonoBehaviour
{
    [Range(0f, 1f)] public float wetness01 = 0.4f;
    [Range(0f, 1f)] public float smell01 = 0.5f;
    public float textureScale = 1f;
    public bool useSdfMaxRandomization = true;
    public bool useRopeCoilPhysics = true;
    public UnityEngine.Object sdfComposition;
    public Transform coilRoot;
    public int coilSeed;
    public int coilSegments = 5;

    /// <summary>Spawn coil/SDF stool in bowl (or at world if bowl null). Clears prior coil children.</summary>
    public void SpawnInBowl(Transform bowl, int seed)
    {
        coilSeed = seed;
        if (bowl != null)
        {
            transform.SetParent(bowl, false);
            transform.localPosition = Vector3.up * (0.04f + wetness01 * 0.02f);
            transform.localRotation = Quaternion.identity;
        }

        if (coilRoot == null)
        {
            var existing = transform.Find("CoilRoot");
            coilRoot = existing != null ? existing : new GameObject("CoilRoot").transform;
            coilRoot.SetParent(transform, false);
        }

        ClearCoilChildren();

        var rng = new System.Random(seed);
        float scale = 0.028f * textureScale * (0.85f + wetness01 * 0.3f);

        if (useRopeCoilPhysics)
        {
            int n = Mathf.Clamp(coilSegments, 3, 12);
            for (int i = 0; i < n; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = $"dung_coil_{i}";
                go.transform.SetParent(coilRoot, false);
                float a = (float)rng.NextDouble() * Mathf.PI * 2f + i * 0.7f;
                float r = 0.03f + (float)rng.NextDouble() * 0.025f;
                go.transform.localScale = new Vector3(scale, scale * (1.1f + wetness01 * 0.4f), scale);
                go.transform.localPosition = new Vector3(Mathf.Cos(a) * r, i * scale * 0.55f, Mathf.Sin(a) * r);
                go.transform.localRotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 40f - 20f,
                    a * Mathf.Rad2Deg,
                    (float)rng.NextDouble() * 30f - 15f);
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.02f + wetness01 * 0.03f;
                rb.linearDamping = 2f + (1f - wetness01) * 4f;
                rb.angularDamping = 2f;
                if (i > 0)
                {
                    var joint = go.AddComponent<ConfigurableJoint>();
                    var prev = coilRoot.GetChild(i - 1);
                    joint.connectedBody = prev.GetComponent<Rigidbody>();
                    joint.xMotion = ConfigurableJointMotion.Limited;
                    joint.yMotion = ConfigurableJointMotion.Limited;
                    joint.zMotion = ConfigurableJointMotion.Limited;
                    var lim = new SoftJointLimit { limit = scale * 1.2f };
                    joint.linearLimit = lim;
                }
            }
        }

        if (useSdfMaxRandomization)
        {
            float sdfScale = 0.05f * textureScale * (0.8f + (seed % 100) * 0.004f + wetness01 * 0.15f);
            transform.localScale = Vector3.one * sdfScale;
            // sdfComposition reserved for SdfMax authoring; coil primitives carry runtime look.
            _ = sdfComposition;
        }

        // Smell emitter for stool odor intensity.
        var smell = GetComponent<Locomotion.Senses.SmellEmitter>();
        if (smell == null)
            smell = gameObject.AddComponent<Locomotion.Senses.SmellEmitter>();
        smell.signature = "poop";
        smell.intensity = smell01;
        smell.emissionMultiplier = smell01;
    }

    void ClearCoilChildren()
    {
        if (coilRoot == null) return;
        for (int i = coilRoot.childCount - 1; i >= 0; i--)
        {
            var c = coilRoot.GetChild(i);
            if (Application.isPlaying) Object.Destroy(c.gameObject);
            else Object.DestroyImmediate(c.gameObject);
        }
    }
}
