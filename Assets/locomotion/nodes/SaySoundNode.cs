using UnityEngine;
using Locomotion.Musculature;

/// <summary>
/// Behavior tree node that plays sounds from the jaw's sound list, enabling speech in physics cards.
/// </summary>
public class SaySoundNode : BehaviorTreeNode
{
    [Header("Jaw Reference")]
    [Tooltip("Reference to jaw component (auto-found if null)")]
    public RagdollJaw jawComponent;

    [Header("Sound Selection")]
    [Tooltip("Index of sound to play (-1 = random, -2 = use soundClip)")]
    public int soundIndex = -1;

    [Tooltip("Direct sound clip reference (overrides soundIndex if set)")]
    public AudioClip soundClip;

    [Header("Playback Settings")]
    [Tooltip("Wait for sound to finish before succeeding")]
    public bool waitForCompletion = true;

    [Tooltip("Volume multiplier (0-1)")]
    [Range(0f, 1f)]
    public float volume = 1f;

    // Execution state
    private bool soundStarted = false;
    private float soundStartTime = 0f;
    private float soundDuration = 0f;

    private void Awake()
    {
        nodeType = NodeType.Action;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        soundStarted = false;
        soundStartTime = 0f;
        soundDuration = 0f;

        // Auto-find jaw component if not assigned
        if (jawComponent == null)
        {
            jawComponent = GetComponentInParent<RagdollJaw>();
            if (jawComponent == null)
            {
                var ragdollSystem = GetComponentInParent<RagdollSystem>();
                if (ragdollSystem != null && ragdollSystem.headComponent != null)
                {
                    jawComponent = ragdollSystem.headComponent.GetComponentInChildren<RagdollJaw>();
                }
            }
        }

        if (jawComponent == null)
        {
            Debug.LogWarning("SaySoundNode: No RagdollJaw component found");
            return;
        }

        // Determine which sound to play
        AudioClip clipToPlay = null;
        if (soundClip != null)
        {
            clipToPlay = soundClip;
        }
        else if (soundIndex >= 0 && jawComponent.soundList != null && soundIndex < jawComponent.soundList.Count)
        {
            clipToPlay = jawComponent.soundList[soundIndex];
        }
        else if (soundIndex == -1 && jawComponent.soundList != null && jawComponent.soundList.Count > 0)
        {
            // Random selection
            int randomIndex = Random.Range(0, jawComponent.soundList.Count);
            clipToPlay = jawComponent.soundList[randomIndex];
        }

        if (clipToPlay != null)
        {
            // Set volume if jaw has audio source
            var audioSource = jawComponent.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = volume;
            }

            // Play sound
            jawComponent.PlaySound(clipToPlay);
            soundStarted = true;
            soundStartTime = Time.time;
            soundDuration = clipToPlay.length;

            if (waitForCompletion)
            {
                // Set estimated duration based on clip length
                estimatedDuration = soundDuration;
            }
        }
        else
        {
            Debug.LogWarning("SaySoundNode: No valid sound to play");
        }
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (jawComponent == null)
        {
            return BehaviorTreeStatus.Failure;
        }

        if (!soundStarted)
        {
            // Sound failed to start
            return BehaviorTreeStatus.Failure;
        }

        if (waitForCompletion)
        {
            // Check if sound is still playing
            if (jawComponent.IsPlaying())
            {
                // Check for timeout (safety check)
                if (Time.time - soundStartTime > soundDuration + 1f)
                {
                    Debug.LogWarning("SaySoundNode: Sound playback timeout");
                    return BehaviorTreeStatus.Success; // Assume completed
                }
                return BehaviorTreeStatus.Running;
            }
            else
            {
                // Sound finished
                return BehaviorTreeStatus.Success;
            }
        }
        else
        {
            // Don't wait, succeed immediately after starting
            return BehaviorTreeStatus.Success;
        }
    }

    public override void OnExit(BehaviorTree tree)
    {
        // Optionally stop sound on exit (usually let it finish)
        // jawComponent?.Stop();
    }
}
