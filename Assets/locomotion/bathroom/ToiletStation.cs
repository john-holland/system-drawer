using System.Collections.Generic;
using UnityEngine;

/// <summary>Toilet with bidet default, TP scroll, lid open/close, before/after sit BTs.</summary>
[AddComponentMenu("Locomotion/Bathroom/Toilet Station")]
public sealed class ToiletStation : MonoBehaviour
{
    public bool includesBidet = true;
    public bool useToiletPaperBt = true;
    public PaperScrollSystem paperScroll;
    [Tooltip("Optional OpenCloseTopologyAsset (Locomotion.Open).")]
    public ScriptableObject lidTopology;
    [Tooltip("Optional ObjectOpenCloseTopologyPlanNode host.")]
    public MonoBehaviour lidPlan;
    public Transform seatAnchor;
    public Transform bowlAnchor;
    public List<BehaviorTreeNode> beforeSitNodes = new List<BehaviorTreeNode>();
    public List<BehaviorTreeNode> afterSitNodes = new List<BehaviorTreeNode>();
    public BathroomToiletOptions options;

    void Awake()
    {
        if (options == null)
            options = ScriptableObject.CreateInstance<BathroomToiletOptions>();
        if (paperScroll == null)
            paperScroll = GetComponentInChildren<PaperScrollSystem>();
    }
}

[CreateAssetMenu(fileName = "BathroomToiletOptions", menuName = "Locomotion/Bathroom/Toilet Options")]
public sealed class BathroomToiletOptions : ScriptableObject
{
    public bool includesBidet = true;
    public bool allowFreeExcreteFallback = true;
    public float sitBlendSeconds = 0.4f;
    public bool autoFlush = true;
    public bool preferBidetOverTp = true;
}
