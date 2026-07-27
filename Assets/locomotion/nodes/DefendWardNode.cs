using UnityEngine;

/// <summary>BT leaf: activate defend wards from card or authored list.</summary>
public sealed class DefendWardNode : BehaviorTreeNode
{
    public CombatCard combatCard;
    public DefendWardRuntime wards;
    public float duration = 1.5f;
    float _t;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (wards == null && tree != null)
            wards = tree.GetComponent<DefendWardRuntime>()
                    ?? tree.gameObject.AddComponent<DefendWardRuntime>();
        if (combatCard == null && tree != null)
            combatCard = tree.GetComponent<CombatCardSelectionSession>()?.selectedCard;
        wards?.SetWardsFromCard(combatCard);
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        return _t >= duration ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }
}
