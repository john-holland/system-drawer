using UnityEngine;
using SdfMax;

/// <summary>
/// Pull 3–5 TP sheets and Mandelbrot-fold into a bun.
/// </summary>
public sealed class ScrunchToiletPaperNode : BehaviorTreeNode
{
    public PaperScrollSystem scroll;
    public int sheetsMin = 3;
    public int sheetsMax = 5;
    public float foldSeconds = 0.8f;
    public Transform bunVisual;
    [Range(0f, 1f)] public float wetness01;
    [Range(0f, 1f)] public float smell01;
    public float resultantPooTextureScale = 1f;

    float _t;
    int _pulled;
    bool _started;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        _started = false;
        _pulled = 0;
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (scroll == null)
            scroll = tree != null ? tree.GetComponentInChildren<PaperScrollSystem>() : null;
        if (scroll == null && tree?.currentGoal?.target != null)
            scroll = tree.currentGoal.target.GetComponentInChildren<PaperScrollSystem>();
        if (scroll == null)
            return BehaviorTreeStatus.Failure;

        if (!_started)
        {
            var seed = tree != null ? tree.GetComponent<DeveloperRespectsSeed>() : null;
            if (seed != null)
            {
                var rng = new System.Random(seed.Seed ^ 0x71C0);
                _pulled = sheetsMin + rng.Next(Mathf.Max(0, sheetsMax - sheetsMin + 1));
            }
            else
                _pulled = Random.Range(sheetsMin, sheetsMax + 1);
            scroll.PullSheets(_pulled);
            _started = true;
        }

        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / Mathf.Max(1e-3f, foldSeconds));
        float m = SdfMaxNoiseUtility.SampleMandelbrot(new Vector2(u * 2f - 1f, wetness01), new SdfMaxNode
        {
            mandelbrotIterations = 24,
            mandelbrotEscape = 4f,
            noiseFrequency = 1f,
            radius = 50f
        });
        float shrink = Mathf.Lerp(1f, 0.25f * resultantPooTextureScale, Mathf.Clamp01(m));
        if (bunVisual != null)
            bunVisual.localScale = Vector3.one * shrink;

        return u >= 1f ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }
}
