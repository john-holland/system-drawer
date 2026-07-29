using UnityEngine;

/// <summary>
/// Builds a short Sequence of golden-ratio conic tuck frames for helmet prep.
/// Frames are procedural (no AnimationClip required); consumed by HairHelmetTuckController.
/// </summary>
public sealed class HairHelmetTuckBehaviorTree
{
    public struct TuckFrame
    {
        public int index;
        public float radiusFraction01;
        public float radiusMeters;
        public string nodeName;
    }

    public readonly TuckFrame[] frames;

    public HairHelmetTuckBehaviorTree(HairPlumeConfig config)
    {
        int n = config != null ? Mathf.Max(2, config.tuckFrameCount) : 8;
        float r0 = config != null ? config.tuckStartRadiusM : 0.22f;
        frames = new TuckFrame[n];
        for (int i = 0; i < n; i++)
        {
            float radius = r0 / Mathf.Pow(HairPlumeConfig.GoldenRatio, i);
            float frac = Mathf.Clamp01(radius / Mathf.Max(1e-4f, r0));
            frames[i] = new TuckFrame
            {
                index = i,
                radiusMeters = radius,
                radiusFraction01 = frac,
                nodeName = $"HairTuck_{i:D2}"
            };
        }
    }

    /// <summary>
    /// Create a GameObject hierarchy Sequence → frame leaves under parent (editor/runtime scaffold).
    /// </summary>
    public GameObject BuildHierarchy(Transform parent, string rootName = "HairHelmetTuckBT")
    {
        var root = new GameObject(rootName);
        if (parent != null)
            root.transform.SetParent(parent, false);

        var seq = new GameObject("Sequence_HairTuck");
        seq.transform.SetParent(root.transform, false);

        for (int i = 0; i < frames.Length; i++)
        {
            var leaf = new GameObject(frames[i].nodeName);
            leaf.transform.SetParent(seq.transform, false);
            var marker = leaf.AddComponent<HairHelmetTuckFrameMarker>();
            marker.frameIndex = frames[i].index;
            marker.radiusFraction01 = frames[i].radiusFraction01;
            marker.radiusMeters = frames[i].radiusMeters;
        }

        return root;
    }
}

/// <summary>Marker on each tuck BT leaf for controllers / open-close style playback.</summary>
public sealed class HairHelmetTuckFrameMarker : MonoBehaviour
{
    public int frameIndex;
    public float radiusFraction01 = 1f;
    public float radiusMeters = 0.22f;
}
