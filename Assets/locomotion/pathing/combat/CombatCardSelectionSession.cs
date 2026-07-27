using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Locomotion/Combat/Card Selection Session")]
public sealed class CombatCardSelectionSession : MonoBehaviour
{
    public SlowTimeController slowTime;
    public CombatObjectNode combatNode;
    public CombatPlannerService planner;
    public SafetyLockWardenPlannerService safetyLock;
    public CombatSession combatSession;

    public readonly List<CombatCard> candidates = new List<CombatCard>();
    public CombatCard hoveredCard;
    public CombatCard selectedCard;
    public CombatMode mode = CombatMode.Melee;
    public GameObject target;
    public bool slowTimeActive;
    public bool requirePlayerConfirm = true;
    [Range(0f, 1f)] public float defaultTimeScale = 0.28f;

    public void Begin(IList<CombatCard> pool, float timeScaleCoefficient)
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
                if (safetyLock != null && !safetyLock.GateFire(c))
                    continue;
                candidates.Add(c);
            }
        }
        if (slowTime != null)
        {
            slowTime.Enter(timeScaleCoefficient > 0f ? timeScaleCoefficient : defaultTimeScale);
            slowTimeActive = true;
        }
    }

    public void SetHovered(CombatCard card) => hoveredCard = card;

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
        if (combatNode != null)
            combatNode.combatCard = selectedCard;
        if (combatSession != null)
            combatSession.activeCard = selectedCard;
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
