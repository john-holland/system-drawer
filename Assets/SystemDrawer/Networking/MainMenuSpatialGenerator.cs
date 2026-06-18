using UnityEngine;

/// <summary>2D spatial generator wrapper for main menu placement.</summary>
[AddComponentMenu("System Drawer/Networking/Main Menu Spatial Generator")]
[RequireComponent(typeof(SpatialGenerator))]
public sealed class MainMenuSpatialGenerator : MonoBehaviour
{
    public SpatialGenerator generator;

    [Header("Menu Root")]
    public Transform menuRoot;

    [Header("Network Requirements Sync")]
    [Tooltip("When on, Update applies canonical networking menu tree and locks managed nodes in the inspector.")]
    public bool syncNetworkRequirements = true;
    [Tooltip("When sync is on, removes managed nodes not present in the canonical spec.")]
    public bool removeOrphansWhenSyncing;

    [Header("2D Layout")]
    public Vector2 menuGenerationSize = new Vector2(800f, 600f);
    [Tooltip("Regenerate spatial layout after applying network requirements.")]
    public bool generateLayoutAfterUpdate = true;

    SGTreeNodeContainer _container;

    void Awake()
    {
        if (menuRoot == null)
            menuRoot = transform;
        ConfigureForMainMenu(menuRoot);
    }

    public void ConfigureForMainMenu(Transform behaviorTreeParent)
    {
        if (behaviorTreeParent != null)
            menuRoot = behaviorTreeParent;
        EnsureSpatialContext();
    }

    public void EnsureSpatialContext()
    {
        if (generator == null)
            generator = GetComponent<SpatialGenerator>();
        if (generator == null)
            generator = gameObject.AddComponent<SpatialGenerator>();
        if (menuRoot == null)
            menuRoot = transform;

        generator.mode = SpatialGenerator.GenerationMode.TwoDimensional;
        generator.behaviorTreeParent = menuRoot;
        generator.generationSize = new Vector3(menuGenerationSize.x, menuGenerationSize.y, 0f);

        _container = menuRoot.GetComponent<SGTreeNodeContainer>();
        if (_container == null)
            _container = menuRoot.gameObject.AddComponent<SGTreeNodeContainer>();

        if (generator.sceneTreeParent == null)
        {
            var sceneTree = transform.Find("SceneTree");
            if (sceneTree == null)
            {
                var sceneTreeGo = new GameObject("SceneTree");
                sceneTreeGo.transform.SetParent(transform, false);
                generator.sceneTreeParent = sceneTreeGo.transform;
            }
            else
            {
                generator.sceneTreeParent = sceneTree;
            }
        }

        generator.Initialize();
        BindContainerRootNode();
    }

    void BindContainerRootNode()
    {
        if (_container == null || menuRoot == null)
            return;

        MenuRagdollNode root = FindMenuRootNode();
        if (root != null)
            _container.rootNode = root;
    }

    MenuRagdollNode FindMenuRootNode()
    {
        var nodes = menuRoot.GetComponentsInChildren<MenuRagdollNode>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].eventName == "menu.root")
                return nodes[i];
        }
        for (int i = 0; i < menuRoot.childCount; i++)
        {
            var node = menuRoot.GetChild(i).GetComponent<MenuRagdollNode>();
            if (node != null)
                return node;
        }
        return menuRoot.GetComponentInChildren<MenuRagdollNode>();
    }

    public MainMenuNetworkRequirementsSync.SyncResult UpdateMainMenuForNetworkRequirements()
    {
        EnsureSpatialContext();
        var menu = menuRoot != null ? menuRoot.GetComponent<MenuRagdoll>() : GetComponent<MenuRagdoll>();
        if (menu == null)
            menu = GetComponentInChildren<MenuRagdoll>();

        var result = MainMenuNetworkRequirementsSync.Apply(
            menuRoot,
            menu,
            syncNetworkRequirements,
            removeOrphansWhenSyncing);

        BindContainerRootNode();
        if (generateLayoutAfterUpdate && syncNetworkRequirements)
            GenerateMenuLayout();
        return result;
    }

    public void GenerateMenuLayout()
    {
        if (generator == null)
            return;
        EnsureSpatialContext();
        generator.Generate();
    }
}
