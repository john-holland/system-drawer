using UnityEngine;

/// <summary>
/// Wires <see cref="Brain.behaviorTree"/> from a first- or third-person <see cref="BehaviorTree"/> template (prefab or scene prototype).
/// Keeps Continuum free of Brain references; vocabulary data lives in <see cref="VocabularyBuiltInRegistry"/>.
/// </summary>
[DisallowMultipleComponent]
public class PlayerVocabBuiltIn : MonoBehaviour
{
    public Brain targetBrain;
    public BehaviorTree firstPersonTreePrefab;
    public BehaviorTree thirdPersonTreePrefab;
    public RagdollPlayerPerspective defaultPerspective = RagdollPlayerPerspective.FirstPerson;
    [Tooltip("Parent for instantiated tree (defaults to brain transform).")]
    public Transform behaviorTreeParent;

    BehaviorTree instantiated;
    Brain wiredBrain;

    void Awake()
    {
        TryWireBehaviorTree();
    }

    void OnEnable()
    {
        TryWireBehaviorTree();
    }

    /// <summary>Call after assigning template references at runtime (e.g. before <see cref="OnEnable"/> if you use <c>AddComponent</c>).</summary>
    public void RefreshWiring() => TryWireBehaviorTree();

    void OnDestroy()
    {
        if (wiredBrain != null && wiredBrain.behaviorTree == instantiated)
            wiredBrain.behaviorTree = null;
        if (instantiated != null)
        {
            Destroy(instantiated.gameObject);
            instantiated = null;
        }
        wiredBrain = null;
    }

    /// <summary>Re-instantiate from <paramref name="mode"/> when you need to swap at runtime.</summary>
    public void ApplyPerspective(RagdollPlayerPerspective mode)
    {
        defaultPerspective = mode;
        if (wiredBrain != null && wiredBrain.behaviorTree == instantiated)
            wiredBrain.behaviorTree = null;
        if (instantiated != null)
        {
            Destroy(instantiated.gameObject);
            instantiated = null;
        }
        wiredBrain = null;
        TryWireBehaviorTree();
    }

    void TryWireBehaviorTree()
    {
        Brain brain = targetBrain != null ? targetBrain : GetComponentInParent<Brain>();
        if (brain == null)
            return;

        BehaviorTree template = SelectTemplate();
        if (template == null)
            return;

        if (brain.behaviorTree != null && brain.behaviorTree != instantiated)
            return;

        if (instantiated != null)
            return;

        Transform parent = behaviorTreeParent != null ? behaviorTreeParent : brain.transform;
        var go = Instantiate(template.gameObject, parent);
        go.name = template.gameObject.name + "(Instance)";
        instantiated = go.GetComponent<BehaviorTree>();
        brain.behaviorTree = instantiated;
        wiredBrain = brain;
    }

    BehaviorTree SelectTemplate()
    {
        return defaultPerspective == RagdollPlayerPerspective.FirstPerson ? firstPersonTreePrefab : thirdPersonTreePrefab;
    }
}
