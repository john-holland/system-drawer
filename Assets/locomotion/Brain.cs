using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Brain component that processes impulses, interprets physics cards, executes behavior trees,
/// and communicates with other brains. Supports dual LSTM systems for symmetric body parts.
/// </summary>
public class Brain : MonoBehaviour
{
    [Header("Brain Properties")]
    [Tooltip("Execution priority (higher = more important)")]
    public int priority = 0;

    [Tooltip("Body part this brain is attached to")]
    public GameObject attachedBodyPart;

    [Tooltip("Main behavior tree")]
    public BehaviorTree behaviorTree;

    [Header("Connected Brains")]
    [Tooltip("Other brains to communicate with")]
    public List<Brain> connectedBrains = new List<Brain>();

    [Header("Impulse Filters")]
    [Tooltip("Filters for processing impulses")]
    public List<ImpulseFilter> impulseFilters = new List<ImpulseFilter>();

    [Header("Dual LSTM System (for symmetric body parts)")]
    [Tooltip("Enable dual LSTM for symmetric body parts")]
    public bool enableDualLSTM = false;

    [Tooltip("Mirror dimension for dual LSTM (x, y, or z)")]
    public MirrorDimension mirrorDimension = MirrorDimension.X;

    [Tooltip("Left LSTM predictor (for symmetric body parts)")]
    public LSTMPredictor leftLSTM;

    [Tooltip("Right LSTM predictor (for symmetric body parts)")]
    public LSTMPredictor rightLSTM;

    [Header("Lie / trust")]
    [Tooltip("When false, incoming Decisions merge without running ILiePolicy.")]
    public bool enableLieDetection = false;

    [Tooltip("Optional; used when enableLieDetection is true. If null, merge deterministically.")]
    public CurveLiePolicy liePolicy;

    [Tooltip("Optional LSTM for lie evaluation when dual LSTM disabled.")]
    public LSTMPredictor lieDetectionLstm;

    [Header("Dream memory")]
    [Tooltip("When true, LSTM outputs are dream-memory only (non-authoritative for physics).")]
    public bool dreamMemoryMode;

    [Tooltip("Optional dream-memory LSTM wrapper.")]
    public Locomotion.DreamCycle.DreamMemoryLSTM dreamMemoryLstm;

    [Header("Thought history (debug / lie policy)")]
    [SerializeField] private int maxThoughtHistory = 16;

    [Header("Incoming thought types")]
    [Tooltip("When false, incoming Decision thoughts are ignored (not logged or merged).")]
    public bool acceptThoughtDecision = true;
    [Tooltip("When false, incoming Query thoughts are ignored (no Response is sent).")]
    public bool acceptThoughtQuery = true;
    [Tooltip("When false, incoming Response thoughts are ignored.")]
    public bool acceptThoughtResponse = true;
    [Tooltip("When false, incoming Alert thoughts are ignored.")]
    public bool acceptThoughtAlert = true;
    [Tooltip("When false, incoming BehaviorTree merge hints are ignored.")]
    public bool acceptThoughtBehaviorTree = true;
    [Tooltip("When false, incoming RequestPrune thoughts are ignored.")]
    public bool acceptThoughtRequestPrune = true;

    // Internal state
    private Queue<ImpulseData> impulseQueue = new Queue<ImpulseData>();
    private Queue<ThoughtData> thoughtQueue = new Queue<ThoughtData>();
    private readonly Queue<ThoughtData> thoughtHistory = new Queue<ThoughtData>();
    private RagdollAnimationSetManager animationSetManager;

    private void Awake()
    {
        animationSetManager = GetComponentInParent<RagdollAnimationSetManager>();
    }

    private void Update()
    {
        // Process impulses
        ProcessImpulses();

        // Execute behavior tree (skip when RagdollAnimationSetManager has playback paused or stopped)
        if (behaviorTree != null)
        {
            if (animationSetManager != null && (animationSetManager.IsPaused || animationSetManager.IsStopped))
                return;
            var playbackGate = GetComponentInParent<IBehaviorTreePlaybackGate>();
            if (playbackGate == null || !playbackGate.ManagesBehaviorTree(behaviorTree))
                behaviorTree.Execute();
        }

        // Process thoughts from other brains
        ProcessThoughts();

        // Update dual LSTM system if enabled
        if (enableDualLSTM)
        {
            UpdateDualLSTM();
        }
    }

    /// <summary>
    /// Process an incoming impulse.
    /// </summary>
    public void ProcessImpulse(ImpulseData impulse)
    {
        if (impulse == null)
            return;

        // Apply filters
        if (!ShouldAllowImpulse(impulse))
            return;

        impulseQueue.Enqueue(impulse);
    }

    /// <summary>
    /// Send thought to another brain.
    /// </summary>
    public void SendThought(Brain target, ThoughtData thought)
    {
        if (target == null || thought == null)
            return;
        thought.sender = this;
        thought.receiver = target;
        target.ReceiveThought(this, thought);
    }

    /// <summary>
    /// Receive thought from another brain.
    /// </summary>
    public void ReceiveThought(Brain sender, ThoughtData thought)
    {
        if (thought == null)
            return;
        if (thought.sender == null)
            thought.sender = sender;
        if (thought.receiver == null)
            thought.receiver = this;
        thoughtQueue.Enqueue(thought);
    }

    /// <summary>Recent processed thoughts (oldest may be evicted).</summary>
    public IReadOnlyList<ThoughtData> GetThoughtHistorySnapshot()
    {
        return thoughtHistory.ToArray();
    }

    /// <summary>
    /// Execute behavior tree.
    /// </summary>
    public void ExecuteBehaviorTree()
    {
        if (behaviorTree != null)
        {
            behaviorTree.Execute();
        }
    }

    /// <summary>
    /// Interpret physics card for behavior tree.
    /// </summary>
    public void InterpretPhysicsCard(GoodSection card)
    {
        if (card == null || behaviorTree == null)
            return;

        // Add card to behavior tree's available cards
        if (!behaviorTree.availableCards.Contains(card))
        {
            behaviorTree.availableCards.Add(card);
        }
    }

    private void ProcessImpulses()
    {
        while (impulseQueue.Count > 0)
        {
            ImpulseData impulse = impulseQueue.Dequeue();
            HandleImpulse(impulse);
        }
    }

    private void ProcessThoughts()
    {
        while (thoughtQueue.Count > 0)
        {
            ThoughtData thought = thoughtQueue.Dequeue();
            HandleThought(thought);
        }
    }

    private void HandleImpulse(ImpulseData impulse)
    {
        if (impulse == null)
            return;

        if (impulse.impulseType == ImpulseType.Motor)
        {
            // Route motor impulse to muscle system
            MotorData motorData = impulse.GetData<MotorData>();
            if (motorData != null)
            {
                RagdollSystem ragdollSystem = GetComponentInParent<RagdollSystem>();
                if (ragdollSystem != null)
                {
                    ragdollSystem.ActivateMuscleGroup(motorData.muscleGroup, motorData.activation);
                }
            }
        }
        else if (impulse.impulseType == ImpulseType.Sensory)
        {
            SensoryData sensoryData = impulse.GetData<SensoryData>();
            if (sensoryData != null && behaviorTree != null)
                behaviorTree.ApplySensoryInput(sensoryData);
        }
    }

    private void PushThoughtHistory(ThoughtData t)
    {
        if (t == null) return;
        thoughtHistory.Enqueue(t);
        while (thoughtHistory.Count > maxThoughtHistory)
            thoughtHistory.Dequeue();
    }

    private LSTMPredictor GetLieDetectionLstm()
    {
        if (lieDetectionLstm != null)
            return lieDetectionLstm;
        if (enableDualLSTM && leftLSTM != null)
            return leftLSTM;
        if (rightLSTM != null)
            return rightLSTM;
        return GetComponent<LSTMPredictor>();
    }

    private bool AcceptsThoughtType(ThoughtType type)
    {
        return type switch
        {
            ThoughtType.Decision => acceptThoughtDecision,
            ThoughtType.Query => acceptThoughtQuery,
            ThoughtType.Response => acceptThoughtResponse,
            ThoughtType.Alert => acceptThoughtAlert,
            ThoughtType.BehaviorTree => acceptThoughtBehaviorTree,
            ThoughtType.RequestPrune => acceptThoughtRequestPrune,
            _ => true
        };
    }

    private void HandleThought(ThoughtData thought)
    {
        if (thought == null)
            return;
        if (!AcceptsThoughtType(thought.messageType))
            return;

        switch (thought.messageType)
        {
            case ThoughtType.Decision:
                PushThoughtHistory(thought);
                HandleDecisionThought(thought);
                break;
            case ThoughtType.Query:
                PushThoughtHistory(thought);
                HandleQueryThought(thought);
                break;
            case ThoughtType.Response:
                PushThoughtHistory(thought);
                if (thought.data is ResponseThoughtPayload rp && !string.IsNullOrEmpty(rp.answerText))
                    Debug.Log($"[Brain:{name}] Response q={rp.queryId} {rp.answerText}");
                break;
            case ThoughtType.Alert:
                PushThoughtHistory(thought);
                if (thought.data is AlertThoughtPayload ap && behaviorTree != null && behaviorTree.currentGoal != null && ap.severity > 0.6f)
                    behaviorTree.currentGoal.priority = Mathf.Min(99, behaviorTree.currentGoal.priority + Mathf.RoundToInt(ap.severity * 3f));
                break;
            case ThoughtType.BehaviorTree:
                PushThoughtHistory(thought);
                if (thought.data is BehaviorTreeThoughtPayload btp && btp.suggestMirrorSenderTree && thought.sender != null && thought.sender.behaviorTree != null && behaviorTree != null)
                    behaviorTree.availableCards = new List<GoodSection>(thought.sender.behaviorTree.availableCards);
                break;
            case ThoughtType.RequestPrune:
                PushThoughtHistory(thought);
                if (behaviorTree == null)
                    break;
                var cards = behaviorTree.GetRequiredCards();
                var pp = thought.data as RequestPruneThoughtPayload;
                if (cards != null && cards.Count > 0)
                    behaviorTree.PruneForCards(cards);
                else if (pp != null && pp.fullTree && behaviorTree.availableCards != null)
                    behaviorTree.PruneForCards(behaviorTree.availableCards);
                break;
        }
    }

    private void HandleDecisionThought(ThoughtData thought)
    {
        float effectiveConviction = Mathf.Clamp01(thought.conviction);
        if (thought.data is DecisionThoughtPayload dp)
            effectiveConviction *= Mathf.Clamp01(dp.conviction);

        if (enableLieDetection && liePolicy != null)
        {
            var lstm = GetLieDetectionLstm();
            LieEvaluation lie = liePolicy.Evaluate(this, thought, lstm);
            if (lie.shouldMisrepresent)
                effectiveConviction *= Mathf.Clamp01(1f - lie.confidence);
            if (effectiveConviction < 0.05f)
                return;
        }

        if (behaviorTree == null || thought.data is not DecisionThoughtPayload dpx || string.IsNullOrEmpty(dpx.proposedGoalName))
            return;

        var goal = new BehaviorTreeGoal
        {
            goalName = dpx.proposedGoalName,
            targetPosition = dpx.optionalTargetPosition,
            priority = Mathf.Clamp(Mathf.RoundToInt(5 + effectiveConviction * 5f), 0, 99)
        };

        if (behaviorTree.currentGoal == null || effectiveConviction >= 0.5f)
            behaviorTree.SetGoal(goal);
    }

    private void HandleQueryThought(ThoughtData thought)
    {
        if (thought.sender == null)
            return;
        var qp = thought.data as QueryThoughtPayload;
        if (qp == null || string.IsNullOrEmpty(qp.queryId))
            qp = new QueryThoughtPayload { queryId = System.Guid.NewGuid().ToString("N"), channels = QueryChannel.All };

        var respPayload = BuildQueryResponse(qp);
        var responseThought = new ThoughtData(this, thought.sender, ThoughtType.Response, respPayload);
        SendThought(thought.sender, responseThought);
    }

    private ResponseThoughtPayload BuildQueryResponse(QueryThoughtPayload qp)
    {
        var sb = new StringBuilder();
        if ((qp.channels & QueryChannel.Goals) != 0 && behaviorTree != null && behaviorTree.currentGoal != null)
        {
            var g = behaviorTree.currentGoal;
            sb.Append("goal:").Append(g.goalName).Append("; ");
        }
        if ((qp.channels & QueryChannel.Filters) != 0 && impulseFilters != null)
            sb.Append("filters:").Append(impulseFilters.Count).Append("; ");
        if ((qp.channels & QueryChannel.BehaviorTreeSummary) != 0 && behaviorTree != null)
            sb.Append("bt:").Append(behaviorTree.gameObject.name).Append("; ");

        return new ResponseThoughtPayload { queryId = qp.queryId, answerText = sb.ToString(), structuredTags = new string[0] };
    }

    private bool ShouldAllowImpulse(ImpulseData impulse)
    {
        if (impulseFilters == null || impulseFilters.Count == 0)
            return true;

        foreach (var filter in impulseFilters)
        {
            if (filter != null && !filter.ShouldAllow(impulse))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Update dual LSTM system for symmetric body parts.
    /// Swizzles data between left and right LSTM predictors.
    /// </summary>
    private void UpdateDualLSTM()
    {
        if (dreamMemoryMode)
        {
            if (dreamMemoryLstm != null)
                dreamMemoryLstm.EncodeDreamMemory();
            return;
        }
        if (leftLSTM == null || rightLSTM == null)
            return;

        // Swizzle data between LSTM predictors based on mirror dimension
        // Left LSTM gets right's data mirrored, and vice versa
        RagdollState leftState = GetStateForLSTM(leftLSTM);
        RagdollState rightState = GetStateForLSTM(rightLSTM);

        // Mirror right state for left LSTM
        RagdollState mirroredRight = MirrorState(rightState, mirrorDimension);
        
        // Mirror left state for right LSTM
        RagdollState mirroredLeft = MirrorState(leftState, mirrorDimension);

        // Update LSTM predictors with mirrored data
        leftLSTM.UpdateWithState(mirroredRight);
        rightLSTM.UpdateWithState(mirroredLeft);
    }

    private RagdollState GetStateForLSTM(LSTMPredictor predictor)
    {
        // Get state relevant to this LSTM predictor
        RagdollSystem ragdollSystem = GetComponentInParent<RagdollSystem>();
        if (ragdollSystem != null)
        {
            return ragdollSystem.GetCurrentState();
        }
        return new RagdollState();
    }

    private RagdollState MirrorState(RagdollState state, MirrorDimension dimension)
    {
        RagdollState mirrored = state.CopyState();

        // Mirror position based on dimension
        switch (dimension)
        {
            case MirrorDimension.X:
                mirrored.rootPosition = new Vector3(-mirrored.rootPosition.x, mirrored.rootPosition.y, mirrored.rootPosition.z);
                break;
            case MirrorDimension.Y:
                mirrored.rootPosition = new Vector3(mirrored.rootPosition.x, -mirrored.rootPosition.y, mirrored.rootPosition.z);
                break;
            case MirrorDimension.Z:
                mirrored.rootPosition = new Vector3(mirrored.rootPosition.x, mirrored.rootPosition.y, -mirrored.rootPosition.z);
                break;
        }

        return mirrored;
    }
}

/// <summary>
/// Mirror dimension for dual LSTM system.
/// </summary>
public enum MirrorDimension
{
    X,
    Y,
    Z
}
