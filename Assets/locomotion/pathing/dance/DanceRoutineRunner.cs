using System.Collections;
using Locomotion.Narrative.Music;
using UnityEngine;

/// <summary>Plays dance routine move indices through RagdollAnimationSetManager (same index space as IK training).</summary>
[AddComponentMenu("Locomotion/Animation/Dance Routine Runner")]
public sealed class DanceRoutineRunner : MonoBehaviour
{
    public DanceRoutineBehaviorTreeAsset routine;
    public RagdollIKAnimationManager ikManager;
    public RagdollAnimationSetManager setManager;
    public CausalityMusicBridge musicBridge;
    public BeatQuantizedActionBinder beatBinder;
    public int currentStep;

    Coroutine _delayedPlay;

    public void PlayStep(int step)
    {
        if (routine == null || routine.moveAnimationIndices == null)
            return;
        if (step < 0 || step >= routine.moveAnimationIndices.Count)
            return;
        currentStep = step;
        int idx = routine.moveAnimationIndices[step];
        float delay = QuantizeDelaySec();
        if (delay > 0.001f)
        {
            if (_delayedPlay != null)
                StopCoroutine(_delayedPlay);
            _delayedPlay = StartCoroutine(PlayAfterDelay(idx, delay));
            return;
        }
        PlayIndex(idx);
    }

    public void PlayNext()
    {
        if (routine == null || routine.moveAnimationIndices == null || routine.moveAnimationIndices.Count == 0)
            return;
        PlayStep((currentStep + 1) % routine.moveAnimationIndices.Count);
    }

    public float QuantizeDelaySec()
    {
        if (routine == null || !routine.containsSong)
            return 0f;
        if (beatBinder != null)
            return beatBinder.QuantizeDelaySec();
        float bpm = musicBridge != null && musicBridge.dialogueBpm > 0f
            ? musicBridge.dialogueBpm
            : routine.bpm;
        float q = musicBridge != null ? musicBridge.playerInteractionQuantize01 : routine.quantize01;
        if (q <= 0f || bpm <= 0f)
            return 0f;
        int sub = Mathf.Max(1, routine.subdivision);
        float beatSec = 60f / bpm;
        float grid = beatSec / sub;
        float into = Time.time % grid;
        return into <= 1e-4f ? 0f : grid - into;
    }

    IEnumerator PlayAfterDelay(int idx, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayIndex(idx);
        _delayedPlay = null;
    }

    void PlayIndex(int idx)
    {
        if (setManager == null)
            setManager = GetComponent<RagdollAnimationSetManager>()
                         ?? GetComponentInParent<RagdollAnimationSetManager>();
        if (setManager != null)
            setManager.Play(idx);
    }
}
