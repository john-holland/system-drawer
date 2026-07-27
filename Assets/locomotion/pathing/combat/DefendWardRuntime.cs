using UnityEngine;

/// <summary>Absorb combat damage using DefendWard list on the actor.</summary>
[AddComponentMenu("Locomotion/Combat/Defend Ward Runtime")]
public sealed class DefendWardRuntime : MonoBehaviour
{
    public System.Collections.Generic.List<DefendWard> activeWards = new System.Collections.Generic.List<DefendWard>();
    public Transform wardFacing;

    public float TryAbsorb(CombatDamageEvent evt, float remaining01)
    {
        if (evt == null || activeWards == null) return 0f;
        float absorbed = 0f;
        Vector3 face = wardFacing != null ? wardFacing.forward : transform.forward;
        for (int i = 0; i < activeWards.Count; i++)
        {
            var w = activeWards[i];
            if (w == null || !w.Absorbs(evt.type)) continue;
            float ang = Vector3.Angle(face, -evt.direction);
            if (ang > w.blockConeDeg * 0.5f) continue;
            float take = remaining01 * Mathf.Clamp01(w.absorb01);
            absorbed += take;
            remaining01 -= take;
            if (remaining01 <= 1e-4f) break;
        }
        return absorbed;
    }

    public void SetWardsFromCard(CombatCard card)
    {
        activeWards.Clear();
        if (card?.defendWards == null) return;
        for (int i = 0; i < card.defendWards.Count; i++)
            if (card.defendWards[i] != null)
                activeWards.Add(card.defendWards[i]);
    }
}
