using UnityEngine;

/// <summary>Before-toilet-sit sequence runner.</summary>
public sealed class BeforeToiletSitNode : BehaviorTreeNode
{
    public ToiletStation toilet;
    int _i;

    public override void OnEnter(BehaviorTree tree)
    {
        _i = 0;
        if (toilet == null && tree?.currentGoal?.target != null)
            toilet = tree.currentGoal.target.GetComponent<ToiletStation>();
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (toilet == null) return BehaviorTreeStatus.Success;
        var list = toilet.beforeSitNodes;
        if (list == null || _i >= list.Count) return BehaviorTreeStatus.Success;
        var node = list[_i];
        if (node == null) { _i++; return BehaviorTreeStatus.Running; }
        var st = node.Execute(tree);
        if (st != BehaviorTreeStatus.Running)
            _i++;
        return BehaviorTreeStatus.Running;
    }
}

/// <summary>After-toilet-sit sequence (bidet or TP scrunch).</summary>
public sealed class AfterToiletSitNode : BehaviorTreeNode
{
    public ToiletStation toilet;
    public ScrunchToiletPaperNode scrunch;
    int _i;
    bool _hygieneDone;

    public override void OnEnter(BehaviorTree tree)
    {
        _i = 0;
        _hygieneDone = false;
        if (toilet == null && tree?.currentGoal?.target != null)
            toilet = tree.currentGoal.target.GetComponent<ToiletStation>();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (toilet == null) return BehaviorTreeStatus.Success;

        if (!_hygieneDone)
        {
            bool bidet = toilet.includesBidet && (toilet.options == null || toilet.options.preferBidetOverTp);
            if (!bidet && toilet.useToiletPaperBt)
            {
                if (scrunch == null)
                {
                    scrunch = new ScrunchToiletPaperNode { scroll = toilet.paperScroll };
                    scrunch.OnEnter(tree);
                }
                var st = scrunch.Execute(tree);
                if (st == BehaviorTreeStatus.Running) return BehaviorTreeStatus.Running;
            }
            _hygieneDone = true;
        }

        var list = toilet.afterSitNodes;
        if (list == null || _i >= list.Count) return BehaviorTreeStatus.Success;
        var node = list[_i];
        if (node == null) { _i++; return BehaviorTreeStatus.Running; }
        var s = node.Execute(tree);
        if (s != BehaviorTreeStatus.Running) _i++;
        return BehaviorTreeStatus.Running;
    }
}

/// <summary>Animate pee/poop on toilet (or free).</summary>
public sealed class ExcreteOnToiletNode : BehaviorTreeNode
{
    public ToiletStation toilet;
    public PeeStreamDirector pee;
    public PoopRuntime poopPrefab;
    public bool doPee = true;
    public bool doPoop = true;
    public float duration = 2f;
    float _t;
    bool _started;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        _started = false;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (!_started)
        {
            var actor = tree != null ? tree.gameObject : null;
            var bladder = actor != null ? BowelBladderRuntime.FindOrCreate(actor) : null;
            if (pee == null && actor != null)
                pee = actor.GetComponent<PeeStreamDirector>() ?? actor.AddComponent<PeeStreamDirector>();
            if (pee != null)
            {
                pee.bowelBladder = bladder;
                pee.groin = actor.GetComponent<GroinAnatomyRuntime>();
                if (doPee)
                {
                    int seed = actor.GetComponent<DeveloperRespectsSeed>()?.Seed ?? 0;
                    pee.BeginRelease(seed);
                }
            }
            if (doPoop && toilet != null && poopPrefab != null)
            {
                var go = Object.Instantiate(poopPrefab.gameObject);
                var pr = go.GetComponent<PoopRuntime>();
                int seed = actor != null && actor.GetComponent<DeveloperRespectsSeed>() != null
                    ? actor.GetComponent<DeveloperRespectsSeed>().Seed
                    : 1;
                pr.SpawnInBowl(toilet.bowlAnchor != null ? toilet.bowlAnchor : toilet.transform, seed);
                if (bladder != null)
                    bladder.bowelFill01 = 0f;
            }
            else if (doPoop && bladder != null)
                bladder.bowelFill01 = 0f;
            _started = true;
        }

        _t += Time.deltaTime;
        if (_t >= duration)
        {
            pee?.EndRelease();
            return BehaviorTreeStatus.Success;
        }
        return BehaviorTreeStatus.Running;
    }
}

/// <summary>Non-toilet pee/poop for ambulating actors / vehicles.</summary>
public sealed class FreeExcreteNode : BehaviorTreeNode
{
    public bool doPee = true;
    public bool doPoop = true;
    public float duration = 1.5f;
    float _t;
    PeeStreamDirector _pee;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        _pee = tree != null ? tree.GetComponent<PeeStreamDirector>() : null;
        if (_pee == null && tree != null)
            _pee = tree.gameObject.AddComponent<PeeStreamDirector>();
        if (doPee && _pee != null)
        {
            _pee.bowelBladder = BowelBladderRuntime.FindOrCreate(tree.gameObject);
            _pee.groin = tree.GetComponent<GroinAnatomyRuntime>();
            _pee.BeginRelease(tree.GetComponent<DeveloperRespectsSeed>()?.Seed ?? 0);
        }
        if (doPoop)
        {
            var b = BowelBladderRuntime.FindOrCreate(tree.gameObject);
            b.bowelFill01 = 0f;
        }
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        if (_t >= duration)
        {
            _pee?.EndRelease();
            return BehaviorTreeStatus.Success;
        }
        return BehaviorTreeStatus.Running;
    }
}
