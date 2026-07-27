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
        TryOpenLid();
    }

    void TryOpenLid()
    {
        if (toilet == null) return;
        WashHandsNode.TryBakeOpenClose(toilet.lidPlan, toilet.lidTopology);
        WashHandsNode.TryBeginOpen(toilet.seatAnchor != null ? toilet.seatAnchor : toilet.transform);
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

/// <summary>After-toilet-sit sequence (bidet wash or TP scrunch).</summary>
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
            if (bidet)
            {
                // Bidet wash: clear groin/stool smells instead of only skipping TP.
                if (tree != null)
                {
                    HygieneSmellClearService.ClearSignatures(tree.gameObject,
                        new[] { "poop", "pee", "groin", "urine", "fecal" });
                    tree.GetComponent<LifeSystemsSheet>()
                        ?.Adjust01(LifeSystemsChannelCatalog.Ablution, 0.12f);
                }
            }
            else if (toilet.useToiletPaperBt)
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
            VehicleOrganHost.FindOrCreate(actor);
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
            if (doPoop && actor != null)
            {
                var proc = FoodProcessorBioRhythmService.Instance
                           ?? actor.GetComponent<FoodProcessorBioRhythmService>();
                Transform bowl = toilet != null
                    ? (toilet.bowlAnchor != null ? toilet.bowlAnchor : toilet.transform)
                    : null;
                if (bladder?.pendingPoop != null && proc != null)
                    proc.SpawnPoopFromPayload(actor, bladder.pendingPoop, bowl);
                else if (proc != null)
                {
                    int seed = actor.GetComponent<DeveloperRespectsSeed>()?.Seed ?? 1;
                    var payload = proc.CreatePoopPayload(null, seed);
                    proc.SpawnPoopFromPayload(actor, payload, bowl);
                }
                else if (bladder != null)
                    bladder.bowelFill01 = 0f;
            }
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
        if (tree == null) return;
        VehicleOrganHost.FindOrCreate(tree.gameObject);
        _pee = tree.GetComponent<PeeStreamDirector>();
        if (_pee == null)
            _pee = tree.gameObject.AddComponent<PeeStreamDirector>();
        if (doPee && _pee != null)
        {
            _pee.bowelBladder = BowelBladderRuntime.FindOrCreate(tree.gameObject);
            _pee.groin = tree.GetComponent<GroinAnatomyRuntime>();
            _pee.BeginRelease(tree.GetComponent<DeveloperRespectsSeed>()?.Seed ?? 0);
        }
        if (doPoop)
        {
            var proc = FoodProcessorBioRhythmService.Instance
                       ?? tree.GetComponent<FoodProcessorBioRhythmService>();
            var bladder = BowelBladderRuntime.FindOrCreate(tree.gameObject);
            if (bladder.pendingPoop != null && proc != null)
                proc.SpawnPoopFromPayload(tree.gameObject, bladder.pendingPoop, null);
            else if (proc != null)
            {
                int seed = tree.GetComponent<DeveloperRespectsSeed>()?.Seed ?? 1;
                proc.SpawnPoopFromPayload(tree.gameObject, proc.CreatePoopPayload(null, seed), null);
            }
            else
                bladder.bowelFill01 = 0f;
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
