using System.Collections.Generic;
using System.Linq;
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

    // Internal state
    private Queue<ImpulseData> impulseQueue = new Queue<ImpulseData>();
    private Queue<ThoughtData> thoughtQueue = new Queue<ThoughtData>();

    private void Update()
    {
        // Process impulses
        ProcessImpulses();

        // Execute behavior tree
        if (behaviorTree != null)
        {
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
        if (target != null && thought != null)
        {
            target.ReceiveThought(this, thought);
        }
    }

    /// <summary>
    /// Receive thought from another brain.
    /// </summary>
    public void ReceiveThought(Brain sender, ThoughtData thought)
    {
        if (thought != null)
        {
            thoughtQueue.Enqueue(thought);
        }
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
            // Route sensory impulse to behavior tree
            SensoryData sensoryData = impulse.GetData<SensoryData>();
            if (sensoryData != null && behaviorTree != null)
            {
                // Update behavior tree with sensory input
                // This would trigger goal changes or decision-making
            }
        }
    }

    private void HandleThought(ThoughtData thought)
    {
        if (thought == null)
            return;

        // Handle different thought types
        switch (thought.messageType)
        {
            case ThoughtType.Decision:
                // Apply decision from another brain
                break;
            case ThoughtType.Query:
                // Respond to query
                break;
            case ThoughtType.BehaviorTree:
                // Receive behavior tree from another brain
                break;
            case ThoughtType.RequestPrune:
                // Request pruning of behavior tree
                break;
        }
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
