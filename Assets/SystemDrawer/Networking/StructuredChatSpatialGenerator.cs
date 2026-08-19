using UnityEngine;

/// <summary>2D spatial generator wrapper for structured chat UI placement.</summary>
[AddComponentMenu("System Drawer/Networking/Structured Chat Spatial Generator")]
[RequireComponent(typeof(SpatialGenerator))]
public sealed class StructuredChatSpatialGenerator : MonoBehaviour
{
    public SpatialGenerator generator;
    public Transform chatRoot;
    public bool removeOrphansWhenSyncing = true;
    public bool generateLayoutAfterUpdate = true;
    public Vector2 generationSize = new Vector2(800f, 400f);
    public ChatLexiconWord[] lexiconWords;

    SGTreeNodeContainer _container;

    void Awake()
    {
        if (chatRoot == null)
            chatRoot = transform;
        EnsureSpatialContext();
    }

    public void EnsureSpatialContext()
    {
        if (generator == null)
            generator = GetComponent<SpatialGenerator>();
        if (generator == null)
            generator = gameObject.AddComponent<SpatialGenerator>();
        if (chatRoot == null)
            chatRoot = transform;

        generator.mode = SpatialGenerator.GenerationMode.TwoDimensional;
        generator.behaviorTreeParent = chatRoot;
        generator.generationSize = new Vector3(generationSize.x, generationSize.y, 0f);

        _container = chatRoot.GetComponent<SGTreeNodeContainer>();
        if (_container == null)
            _container = chatRoot.gameObject.AddComponent<SGTreeNodeContainer>();

        if (generator.sceneTreeParent == null)
        {
            var sceneTree = transform.Find("SceneTree");
            if (sceneTree == null)
            {
                var go = new GameObject("SceneTree");
                go.transform.SetParent(transform, false);
                generator.sceneTreeParent = go.transform;
            }
            else
            {
                generator.sceneTreeParent = sceneTree;
            }
        }

        generator.Initialize();
        BindRoot();
    }

    void BindRoot()
    {
        if (_container == null || chatRoot == null)
            return;
        var nodes = chatRoot.GetComponentsInChildren<StructuredChatRagdollNode>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].eventName == "chat.root")
            {
                _container.rootNode = nodes[i];
                return;
            }
        }
    }

    public StructuredChatNetworkRequirementsSync.SyncResult UpdateForLexicon()
    {
        EnsureSpatialContext();
        var ragdoll = chatRoot != null ? chatRoot.GetComponent<StructuredChatRagdoll>() : GetComponent<StructuredChatRagdoll>();
        var words = lexiconWords;
        if ((words == null || words.Length == 0) && ragdoll != null)
            words = ragdoll.LexiconWords;
        var result = StructuredChatNetworkRequirementsSync.Apply(chatRoot, words, removeOrphansWhenSyncing);
        BindRoot();
        if (generateLayoutAfterUpdate)
            GenerateLayout();
        return result;
    }

    public void GenerateLayout()
    {
        EnsureSpatialContext();
        if (generator != null)
            generator.Generate();
    }
}
