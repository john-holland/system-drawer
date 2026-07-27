using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime state for slow-time LoveCard selection.</summary>
[AddComponentMenu("Locomotion/Love Making/Card Selection Session")]
public sealed class LoveMakingCardSelectionSession : MonoBehaviour
{
    public SlowTimeController slowTime;
    public LoveMakeObjectNode loveMakeNode;
    public LoveMakingPlannerService planner;
    public ConsentWardenPlannerService consentWarden;
    public LoveMakingSession loveSession;

    public readonly List<LoveCard> candidates = new List<LoveCard>();
    public LoveCard hoveredCard;
    public LoveCard selectedCard;
    public LoveMakingMode mode = LoveMakingMode.Tender;
    public GameObject partner;
    public bool slowTimeActive;
    public bool requirePlayerConfirm = true;
    [Range(0f, 1f)] public float defaultTimeScale = 0.32f;

    public void Begin(IList<LoveCard> pool, float timeScaleCoefficient)
    {
        candidates.Clear();
        hoveredCard = null;
        selectedCard = null;
        if (pool != null)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c == null) continue;
                if (consentWarden != null)
                    c = consentWarden.SoftGate(c);
                candidates.Add(c);
            }
        }
        if (slowTime != null)
        {
            slowTime.Enter(timeScaleCoefficient > 0f ? timeScaleCoefficient : defaultTimeScale);
            slowTimeActive = true;
        }
    }

    public void SetHovered(LoveCard card) => hoveredCard = card;

    public bool TryConfirmHovered()
    {
        if (hoveredCard == null) return false;
        selectedCard = hoveredCard;
        return true;
    }

    public void Commit()
    {
        if (selectedCard == null && hoveredCard != null)
            selectedCard = hoveredCard;
        if (loveMakeNode != null)
            loveMakeNode.loveCard = selectedCard;
        if (loveSession != null)
            loveSession.activeCard = selectedCard;
        EndSlowTime();
    }

    public void Cancel()
    {
        selectedCard = null;
        EndSlowTime();
    }

    public void EndSlowTime()
    {
        if (slowTime != null && slowTimeActive)
            slowTime.Exit();
        slowTimeActive = false;
    }
}
