using UnityEngine;

/// <summary>Phenology clock. Advances the flower when water, sun, and ground sit inside their limits.</summary>
[AddComponentMenu("Locomotion/Civil/Flower Bio Rhythm")]
public sealed class FlowerBioRhythm : MonoBehaviour
{
    public FlowerTravelAgent agent;
    public float growthAgeSec;
    public float advancePerSecond = 0.15f;

    void Awake()
    {
        if (agent == null)
            agent = GetComponent<FlowerTravelAgent>();
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        if (agent == null || dt <= 0f) return;
        growthAgeSec += dt;
        var step = agent.SelectedStep;
        if (step == null || agent.aborted) return;
        if (!agent.RequirementsInsideLimits()) return;
        step.progress01 = Mathf.MoveTowards(step.progress01, 1f, dt * advancePerSecond);
        if (step.progress01 < 1f - 1e-4f) return;
        if (agent.TryAdvancePhenology())
            agent.PublishStepGrowth();
    }
}
