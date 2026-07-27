using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime state for slow-time WrestlingCard selection (gambit clone).</summary>
[AddComponentMenu("Locomotion/Wrestling/Card Selection Session")]
public sealed class WrestlingCardSelectionSession : MonoBehaviour
{
    public SlowTimeController slowTime;
    public AngularWrestlingCardSelectMode selectMode;
    public WrestlingCardHighlightRenderer highlight;
    public WrestlingMoveInputBindings moveBindings;
    public GambitInputTriggerBuffer inputBuffer;
    public WrestleObjectNode wrestleNode;
    public WrestlingPlannerService planner;

    public readonly List<WrestlingCard> candidates = new List<WrestlingCard>();
    public WrestlingCard hoveredCard;
    public WrestlingCard selectedCard;
    public WrestlingMode mode = WrestlingMode.Play;
    public GameObject opponent;
    public bool slowTimeActive;
    public bool requirePlayerConfirm = true;
    [Range(0f, 1f)] public float defaultTimeScale = 0.28f;

    public void Begin(IList<WrestlingCard> pool, float timeScaleCoefficient)
    {
        candidates.Clear();
        hoveredCard = null;
        selectedCard = null;
        if (pool != null)
        {
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null)
                    candidates.Add(pool[i]);
        }
        if (selectMode != null)
            selectMode.SetCandidates(candidates);
        if (highlight != null)
            highlight.Clear();
        if (slowTime != null)
        {
            slowTime.Enter(timeScaleCoefficient > 0f ? timeScaleCoefficient : defaultTimeScale);
            slowTimeActive = true;
        }
        if (inputBuffer != null)
            inputBuffer.Clear();
    }

    public void SetHovered(WrestlingCard card)
    {
        hoveredCard = card;
        if (selectMode != null)
            selectMode.SetHovered(card);
        if (highlight != null)
            highlight.SetHovered(card, opponent);
    }

    public bool TryConfirmHovered()
    {
        if (hoveredCard == null) return false;
        selectedCard = hoveredCard;
        if (selectMode != null)
            selectMode.SetSelected(selectedCard);
        if (highlight != null)
            highlight.SetSelected(selectedCard, opponent);
        return true;
    }

    public bool TrySelectMoveKind(WrestlingMoveKind kind)
    {
        WrestlingCard best = null;
        int bestOptional = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            if (c == null || c.moveKind != kind) continue;
            int opt = c.CountOptionalLimbsPresent(GetComponent<RagdollSystem>());
            if (opt > bestOptional)
            {
                bestOptional = opt;
                best = c;
            }
        }
        if (best == null) return false;
        SetHovered(best);
        return TryConfirmHovered();
    }

    public void Cancel()
    {
        selectedCard = null;
        hoveredCard = null;
        if (selectMode != null)
            selectMode.ClearSelection();
        if (highlight != null)
            highlight.Clear();
        EndSlowTime();
    }

    public bool Commit()
    {
        if (selectedCard == null)
            return false;
        if (planner != null)
            selectedCard = planner.ExpandBranches(selectedCard);
        if (wrestleNode != null)
            wrestleNode.wrestlingCard = selectedCard;
        EndSlowTime();
        return true;
    }

    public void EndSlowTime()
    {
        if (slowTime != null && slowTimeActive)
            slowTime.Exit();
        slowTimeActive = false;
    }
}
