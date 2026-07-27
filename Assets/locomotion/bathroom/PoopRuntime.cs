using UnityEngine;

/// <summary>Parameterized stool: wetness, smell, texture; optional SDF/rope coil in bowl.</summary>
[AddComponentMenu("Locomotion/Bathroom/Poop Runtime")]
public sealed class PoopRuntime : MonoBehaviour
{
    [Range(0f, 1f)] public float wetness01 = 0.4f;
    [Range(0f, 1f)] public float smell01 = 0.5f;
    public float textureScale = 1f;
    public bool useSdfMaxRandomization = true;
    public bool useRopeCoilPhysics;
    public UnityEngine.Object sdfComposition;
    public Transform coilRoot;
    public int coilSeed;

    public void SpawnInBowl(Transform bowl, int seed)
    {
        // todo: use rope physics with toilet bowl volume
        coilSeed = seed;
        if (bowl == null) return;
        transform.SetParent(bowl, false);
        transform.localPosition = Vector3.up * 0.05f;
        if (useRopeCoilPhysics && coilRoot != null)
        {
            // Lightweight coil: stacked offset rings.
            var rng = new System.Random(seed);
            for (int i = 0; i < 4; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = $"dung_coil_{i}";
                go.transform.SetParent(coilRoot != null ? coilRoot : transform, false);
                go.transform.localScale = Vector3.one * (0.03f * textureScale);
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                go.transform.localPosition = new Vector3(Mathf.Cos(a), i * 0.02f, Mathf.Sin(a)) * 0.04f;
                Object.Destroy(go.GetComponent<Collider>());
            }
        }
        else if (useSdfMaxRandomization)
        {
            transform.localScale = Vector3.one * (0.05f * textureScale * (0.8f + (seed % 100) * 0.004f));
        }
    }
}
