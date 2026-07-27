using System.Collections.Generic;
using UnityEngine;
using Weather;

/// <summary>Tooth face for brushing (not world axes).</summary>
public enum ToothBrushFace
{
    Buccal = 0,
    Lingual = 1,
    Occlusal = 2
}

/// <summary>Brush each present tooth on buccal / lingual / occlusal faces.</summary>
public sealed class BrushTeethNode : BehaviorTreeNode
{
    public MouthInteriorRuntime mouth;
    public LipEdgeWrapDriver lipWrap;
    public Transform brushTip;
    public float secondsPerSide = 0.08f;

    List<(ToothSlot tooth, ToothBrushFace face)> _plan;
    int _i;
    float _t;

    public override void OnEnter(BehaviorTree tree)
    {
        _i = 0;
        _t = 0f;
        if (mouth == null && tree != null)
            mouth = tree.GetComponent<MouthInteriorRuntime>() ?? tree.GetComponentInChildren<MouthInteriorRuntime>();
        if (lipWrap == null && mouth != null)
            lipWrap = mouth.lipWrap;
        _plan = new List<(ToothSlot, ToothBrushFace)>();
        if (mouth != null)
        {
            foreach (var tooth in mouth.EnumeratePresent())
            {
                _plan.Add((tooth, ToothBrushFace.Buccal));
                _plan.Add((tooth, ToothBrushFace.Lingual));
                _plan.Add((tooth, ToothBrushFace.Occlusal));
            }
        }
        if (brushTip != null && lipWrap != null)
            lipWrap.UpsertTrack(brushTip, 0.008f, 0.05f);
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (_plan == null || _plan.Count == 0) return BehaviorTreeStatus.Success;
        _t += Time.deltaTime;
        var (tooth, face) = _plan[_i];
        if (mouth != null && brushTip != null)
        {
            Vector3 p = mouth.ResolveToothWorld(tooth);
            mouth.ResolveToothFaceNormals(tooth, out var buccal, out var lingual, out var occlusal);
            Vector3 n = face == ToothBrushFace.Buccal ? buccal
                : (face == ToothBrushFace.Lingual ? lingual : occlusal);
            brushTip.position = p + n * 0.005f;
            brushTip.rotation = Quaternion.LookRotation(n, mouth.transform.up);
        }
        if (_t >= secondsPerSide)
        {
            _t = 0f;
            _i++;
            if (_i >= _plan.Count) return BehaviorTreeStatus.Success;
        }
        return BehaviorTreeStatus.Running;
    }
}

/// <summary>Brush tongue mesh/SDF.</summary>
public sealed class BrushTongueNode : BehaviorTreeNode
{
    public TongueRuntime tongue;
    public float duration = 1f;
    float _t;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (tongue == null && tree != null)
            tongue = tree.GetComponentInChildren<TongueRuntime>();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (tongue == null) return BehaviorTreeStatus.Failure;
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / duration);
        tongue.curl01 = Mathf.PingPong(u * 2f, 1f);
        tongue.SetFoodPocketLocal(new Vector3(0f, 0f, Mathf.Lerp(0.01f, 0.04f, u)));
        return _t >= duration ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }
}

/// <summary>Floss between adjacent tooth pairs.</summary>
public sealed class FlossTeethNode : BehaviorTreeNode
{
    public MouthInteriorRuntime mouth;
    public float secondsPerPair = 0.12f;
    List<(ToothSlot a, ToothSlot b)> _pairs;
    int _i;
    float _t;

    public override void OnEnter(BehaviorTree tree)
    {
        _i = 0;
        _t = 0f;
        if (mouth == null && tree != null)
            mouth = tree.GetComponent<MouthInteriorRuntime>();
        _pairs = new List<(ToothSlot, ToothSlot)>();
        if (mouth == null) return;
        var list = new List<ToothSlot>(mouth.EnumeratePresent());
        list.Sort((x, y) => x.stop01.CompareTo(y.stop01));
        for (int i = 0; i + 1 < list.Count; i++)
        {
            if (list[i].arch == list[i + 1].arch && list[i].side == list[i + 1].side)
                _pairs.Add((list[i], list[i + 1]));
        }
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (_pairs == null || _pairs.Count == 0) return BehaviorTreeStatus.Success;
        _t += Time.deltaTime;
        if (_t >= secondsPerPair)
        {
            _t = 0f;
            _i++;
            if (_i >= _pairs.Count) return BehaviorTreeStatus.Success;
        }
        return BehaviorTreeStatus.Running;
    }
}

/// <summary>Wash hands: open/close sink + clear hand smells + manifold whitelist.</summary>
public sealed class WashHandsNode : BehaviorTreeNode
{
    public ScriptableObject sinkTopology;
    public MonoBehaviour sinkPlan;
    public Transform sinkCenter;
    public WeatherPhysicsManifold manifold;
    public List<string> manifoldWhitelist = new List<string>
    {
        HygieneManifoldClearService.ChannelWater,
        HygieneManifoldClearService.ChannelOdor
    };
    public List<string> manifoldBlacklist = new List<string> { HygieneManifoldClearService.ChannelSkin };
    public float duration = 1.2f;
    float _t;
    bool _opened;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        _opened = false;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (!_opened)
        {
            TryBakeOpenClose(sinkPlan, sinkTopology);
            TryBeginOpen(sinkCenter);
            _opened = true;
        }

        _t += Time.deltaTime;
        if (tree != null)
        {
            HygieneSmellClearService.ClearHands(tree.gameObject);
            var sheet = tree.GetComponent<LifeSystemsSheet>();
            sheet?.Adjust01(LifeSystemsChannelCatalog.Ablution, 0.05f * Time.deltaTime);
        }
        if (manifold != null && sinkCenter != null)
            HygieneManifoldClearService.ClearSphere(manifold, sinkCenter.position, 0.35f, manifoldWhitelist, manifoldBlacklist);

        return _t >= duration ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }

    public static void TryBakeOpenClose(MonoBehaviour plan, ScriptableObject topology)
    {
        if (plan == null || topology == null) return;
        var topoField = plan.GetType().GetField("topology");
        topoField?.SetValue(plan, topology);
        plan.GetType().GetMethod("BakeFromTopology")?.Invoke(plan, null);
    }

    public static void TryBeginOpen(Transform root)
    {
        if (root == null) return;
        var mbs = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < mbs.Length; i++)
        {
            var m = mbs[i]?.GetType().GetMethod("BeginOpen");
            if (m != null && m.GetParameters().Length == 0)
            {
                m.Invoke(mbs[i], null);
                return;
            }
        }
    }
}

/// <summary>Shower: whole-body smell clear + manifold lists.</summary>
public sealed class ShowerNode : BehaviorTreeNode
{
    public ScriptableObject showerTopology;
    public MonoBehaviour showerPlan;
    public Transform showerHead;
    public WeatherPhysicsManifold manifold;
    public List<string> manifoldWhitelist = new List<string>
    {
        HygieneManifoldClearService.ChannelWater,
        HygieneManifoldClearService.ChannelHumidity,
        HygieneManifoldClearService.ChannelOdor
    };
    public List<string> manifoldBlacklist = new List<string> { HygieneManifoldClearService.ChannelSkin };
    public float duration = 3f;
    float _t;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        WashHandsNode.TryBakeOpenClose(showerPlan, showerTopology);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        if (tree != null)
        {
            HygieneSmellClearService.ClearAllOn(tree.gameObject);
            tree.GetComponent<LifeSystemsSheet>()?.Adjust01(LifeSystemsChannelCatalog.Ablution, 0.08f * Time.deltaTime);
        }
        if (manifold != null && showerHead != null)
            HygieneManifoldClearService.ClearSphere(manifold, showerHead.position, 1.2f, manifoldWhitelist, manifoldBlacklist);
        return _t >= duration ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }
}
