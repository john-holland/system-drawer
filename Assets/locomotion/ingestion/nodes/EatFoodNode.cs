using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pruneable/mergeable eat sequence: bake → bite → chew → swallow.
/// Front bite = vertical jaw; molars = 3D roll; sections drive progressive chew count.
/// </summary>
public sealed class EatFoodNode : BehaviorTreeNode
{
    public FoodItem food;
    public MouthInteriorRuntime mouth;
    public bool allowOpenClosePeel = true;
    public float phaseSeconds = 0.35f;

    List<ChewPhase> _phases;
    ChewConvexTreeBakeService.BakeResult _bake;
    int _phaseIndex;
    float _elapsed;
    int _sectionIndex;
    int _sectionChewsRemaining;
    bool _started;

    public override void OnEnter(BehaviorTree tree)
    {
        _started = false;
        _phaseIndex = 0;
        _elapsed = 0f;
        _sectionIndex = 0;
        _sectionChewsRemaining = 0;
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (food == null && tree?.currentGoal?.target != null)
            food = tree.currentGoal.target.GetComponent<FoodItem>();
        if (mouth == null && tree != null)
            mouth = tree.GetComponent<MouthInteriorRuntime>()
                    ?? tree.GetComponentInChildren<MouthInteriorRuntime>();
        if (food == null || mouth == null)
            return BehaviorTreeStatus.Failure;

        if (!_started)
        {
            _bake = ChewConvexTreeBakeService.Bake(food, mouth);
            _phases = ChewStrategy.PhasesFor(food.kind);
            if (!allowOpenClosePeel)
                _phases.RemoveAll(p => p == ChewPhase.OpenClosePeel);
            _started = true;
            mouth.SetFoodInMouth(food.biteFitRadius, food.mouthfeelLongevitySeconds);
            PrimeSectionChews();
        }

        if (_phases == null || _phases.Count == 0)
            return BehaviorTreeStatus.Success;

        _elapsed += Time.deltaTime;
        var phase = _phases[Mathf.Clamp(_phaseIndex, 0, _phases.Count - 1)];
        TickPhase(phase);

        float dur = phaseSeconds;
        if (phase == ChewPhase.ChewMolarsProgressive || phase == ChewPhase.TongueParabola)
        {
            dur = Mathf.Max(phaseSeconds, mouth.mouthfeelLongevityRemaining > 0f
                ? Mathf.Min(2.5f, mouth.mouthfeelLongevityRemaining * 0.25f)
                : phaseSeconds);
            // Extra time for remaining chewable sections.
            if (_sectionChewsRemaining > 0)
                dur += _sectionChewsRemaining * 0.08f;
        }

        if (_elapsed >= dur)
        {
            _elapsed = 0f;
            if (phase == ChewPhase.ChewMolarsProgressive && _sectionChewsRemaining > 0)
            {
                _sectionChewsRemaining--;
                AdvanceSection();
                return BehaviorTreeStatus.Running;
            }
            _phaseIndex++;
            if (_phaseIndex >= _phases.Count)
            {
                mouth.ClearFoodInMouth();
                var processor = FoodProcessorBioRhythmService.Instance
                                ?? tree.GetComponent<FoodProcessorBioRhythmService>();
                processor?.OnSwallow(tree.gameObject, food);
                return BehaviorTreeStatus.Success;
            }
            if (_phases[_phaseIndex] == ChewPhase.ChewMolarsProgressive)
                PrimeSectionChews();
        }

        return BehaviorTreeStatus.Running;
    }

    void PrimeSectionChews()
    {
        _sectionChewsRemaining = 0;
        if (_bake?.sections == null) return;
        for (int i = 0; i < _bake.sections.Count; i++)
        {
            if (_bake.sections[i] != null && !_bake.sections[i].inedible)
                _sectionChewsRemaining++;
        }
        _sectionChewsRemaining = Mathf.Max(1, _sectionChewsRemaining);
        AdvancePastInedible();
    }

    void AdvanceSection()
    {
        _sectionIndex++;
        AdvancePastInedible();
        if (_bake?.sections != null && _sectionIndex < _bake.sections.Count)
        {
            float r = _bake.sections[_sectionIndex].fitRadius;
            mouth.foodInMouthRadius = Mathf.Max(0.002f, r);
        }
    }

    void TickPhase(ChewPhase phase)
    {
        float t = Mathf.Clamp01(_elapsed / Mathf.Max(1e-3f, phaseSeconds));
        bool preferRight = mouth.PreferRightChewSide;
        switch (phase)
        {
            case ChewPhase.FrontCut:
            case ChewPhase.BiteToFit:
            case ChewPhase.ChewFront:
                mouth.DriveFrontBite(Mathf.PingPong(t * 2f, 1f));
                break;
            case ChewPhase.ChewMolarsProgressive:
                mouth.DriveMolarRoll(t, preferRight);
                if (mouth.tongue != null)
                    mouth.tongue.SetFoodPocketLocal(ChewStrategy.TongueOffsetForMeat(t, preferRight));
                break;
            case ChewPhase.TongueMove:
                if (mouth.tongue != null)
                    mouth.tongue.SetFoodPocketLocal(ChewStrategy.TongueOffsetForMeat(t, preferRight));
                break;
            case ChewPhase.TongueParabola:
                mouth.tongue?.SetPocketParabola(t);
                mouth.DriveMolarRoll(t, preferRight);
                break;
            case ChewPhase.DiscardInedible:
                AdvancePastInedible();
                // Duck-typed put-back / open for inedible peels.
                TryPutBackInedible(t);
                break;
            case ChewPhase.OpenClosePeel:
                var drivers = food.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < drivers.Length; i++)
                {
                    var d = drivers[i];
                    if (d == null) continue;
                    var m = d.GetType().GetMethod("SetOpen01", new[] { typeof(float) });
                    m?.Invoke(d, new object[] { Mathf.Clamp01(t) });
                }
                break;
            case ChewPhase.Swallow:
                mouth.DriveFrontBite(Mathf.Lerp(0.4f, 0.1f, t));
                mouth.foodInMouthRadius = Mathf.Lerp(mouth.foodInMouthRadius, 0f, t);
                break;
        }
    }

    void TryPutBackInedible(float t)
    {
        if (_bake?.sections == null || food == null) return;
        if (_sectionIndex >= _bake.sections.Count) return;
        if (!_bake.sections[_sectionIndex].inedible) return;
        var drivers = food.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < drivers.Length; i++)
        {
            var d = drivers[i];
            if (d == null) continue;
            var m = d.GetType().GetMethod("SetOpen01", new[] { typeof(float) });
            m?.Invoke(d, new object[] { Mathf.Clamp01(1f - t) });
        }
    }

    void AdvancePastInedible()
    {
        if (_bake?.sections == null) return;
        while (_sectionIndex < _bake.sections.Count && _bake.sections[_sectionIndex].inedible)
            _sectionIndex++;
    }
}

/// <summary>Thin bite-only node.</summary>
public sealed class BiteNode : BehaviorTreeNode
{
    public MouthInteriorRuntime mouth;
    public float duration = 0.25f;
    float _t;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (mouth == null && tree != null)
            mouth = tree.GetComponent<MouthInteriorRuntime>();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (mouth == null) return BehaviorTreeStatus.Failure;
        _t += Time.deltaTime;
        mouth.DriveFrontBite(Mathf.PingPong(_t * 4f, 1f));
        return _t >= duration ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }
}

/// <summary>Thin chew-only node (molar roll).</summary>
public sealed class ChewNode : BehaviorTreeNode
{
    public MouthInteriorRuntime mouth;
    public float duration = 0.6f;
    float _t;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (mouth == null && tree != null)
            mouth = tree.GetComponent<MouthInteriorRuntime>();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (mouth == null) return BehaviorTreeStatus.Failure;
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / duration);
        mouth.DriveMolarRoll(u, mouth.PreferRightChewSide);
        mouth.tongue?.SetFoodPocketLocal(ChewStrategy.TongueOffsetForMeat(u, mouth.PreferRightChewSide));
        return _t >= duration ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }
}

/// <summary>Thin swallow node.</summary>
public sealed class SwallowNode : BehaviorTreeNode
{
    public MouthInteriorRuntime mouth;
    public FoodItem food;
    public float duration = 0.3f;
    float _t;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (mouth == null && tree != null)
            mouth = tree.GetComponent<MouthInteriorRuntime>();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (mouth == null) return BehaviorTreeStatus.Failure;
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / duration);
        mouth.DriveFrontBite(Mathf.Lerp(0.4f, 0.1f, u));
        if (_t >= duration)
        {
            mouth.ClearFoodInMouth();
            var processor = FoodProcessorBioRhythmService.Instance
                            ?? tree.GetComponent<FoodProcessorBioRhythmService>();
            if (food == null && tree?.currentGoal?.target != null)
                food = tree.currentGoal.target.GetComponent<FoodItem>();
            if (food != null)
                processor?.OnSwallow(tree.gameObject, food);
            return BehaviorTreeStatus.Success;
        }
        return BehaviorTreeStatus.Running;
    }
}
