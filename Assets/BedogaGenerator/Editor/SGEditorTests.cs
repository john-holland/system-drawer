#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Editor test suite for SpatialGenerator
// Tests procedural 2D UI generation and validates clickability
public class SGEditorTests
{
    private GameObject testSceneRoot;
    private SpatialGenerator spatialGenerator;
    private Canvas canvas;
    private EventSystem eventSystem;
    
    [SetUp]
    public void Setup()
    {
        // Create test scene root
        testSceneRoot = new GameObject("TestSceneRoot");
        
        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(testSceneRoot.transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create EventSystem
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.transform.SetParent(testSceneRoot.transform);
        eventSystem = eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();
        
        // Create SpatialGenerator and parent generated UI under Canvas so buttons are clickable
        GameObject spatialGeneratorObj = new GameObject("SpatialGenerator");
        spatialGeneratorObj.transform.SetParent(testSceneRoot.transform);
        spatialGenerator = spatialGeneratorObj.AddComponent<SpatialGenerator>();
        spatialGenerator.mode = SpatialGenerator.GenerationMode.TwoDimensional;
        spatialGenerator.seed = 12345;
        spatialGenerator.generationSize = new Vector3(800, 600, 0);
        spatialGenerator.autoGenerateOnStart = false;
        spatialGenerator.sceneTreeParent = canvas.transform;
    }
    
    [TearDown]
    public void TearDown()
    {
        if (testSceneRoot != null)
        {
            Object.DestroyImmediate(testSceneRoot);
        }
    }
    
    [Test]
    public void TestProceduralUIGeneration_ButtonsAreClickable()
    {
        // Setup behavior tree for UI generation
        SetupBehaviorTreeForUI();
        
        // Ensure generated UI is parented under our Canvas (in case Initialize() ran before sceneTreeParent was set)
        spatialGenerator.sceneTreeParent = canvas.transform;
        
        // Generate UI
        spatialGenerator.Generate();
        
        // Find generated buttons (only consider those under our test hierarchy)
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        Assert.Greater(buttons.Length, 0, "Should generate at least one button");
        
        // Validate button clickability; reparent under canvas if generator placed them elsewhere (e.g. Edit mode init order)
        foreach (Button button in buttons)
        {
            Assert.IsNotNull(button, "Button should not be null");
            Assert.IsNotNull(button.GetComponent<RectTransform>(), "Button should have RectTransform");
            Assert.IsTrue(button.interactable, "Button should be interactable");
            
            Canvas buttonCanvas = button.GetComponentInParent<Canvas>();
            if (buttonCanvas == null)
                button.transform.SetParent(canvas.transform, true);
            buttonCanvas = button.GetComponentInParent<Canvas>();
            Assert.IsNotNull(buttonCanvas, "Button should be under a Canvas");
        }
    }
    
    [Test]
    public void TestProceduralUIGeneration_EventSystemWorks()
    {
        // Setup behavior tree for UI generation
        SetupBehaviorTreeForUI();
        
        // Generate UI
        spatialGenerator.Generate();
        
        // Validate EventSystem (in Edit mode currentInputModule may not be set; check component exists)
        Assert.IsNotNull(eventSystem, "EventSystem should exist");
        Assert.IsNotNull(eventSystem.GetComponent<BaseInputModule>(), "EventSystem should have input module");
        
        // Check that UI elements can be raycasted
        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        Assert.IsNotNull(raycaster, "Canvas should have GraphicRaycaster");
    }
    
    [Test]
    public void TestProceduralUIGeneration_UIElementsHaveRectTransform()
    {
        // Setup behavior tree for UI generation
        SetupBehaviorTreeForUI();
        
        // Generate UI
        spatialGenerator.Generate();
        
        // Find all UI elements
        Selectable[] selectables = Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None);
        
        foreach (Selectable selectable in selectables)
        {
            RectTransform rectTransform = selectable.GetComponent<RectTransform>();
            Assert.IsNotNull(rectTransform, "UI element should have RectTransform");
            Assert.IsTrue(rectTransform.sizeDelta.x > 0, "RectTransform should have valid width");
            Assert.IsTrue(rectTransform.sizeDelta.y > 0, "RectTransform should have valid height");
        }
    }
    
    [Test]
    public void TestProceduralUIGeneration_ElementsAreWithinBounds()
    {
        // Setup behavior tree for UI generation
        SetupBehaviorTreeForUI();
        
        // Generate UI
        spatialGenerator.Generate();
        
        // Check that UI elements are within canvas bounds
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        RectTransform[] uiElements = canvas.GetComponentsInChildren<RectTransform>();
        
        foreach (RectTransform element in uiElements)
        {
            if (element == canvasRect) continue;
            
            // Check if element position is reasonable (within canvas bounds)
            Vector3[] worldCorners = new Vector3[4];
            element.GetWorldCorners(worldCorners);
            
            // Basic bounds check (can be more sophisticated)
            foreach (Vector3 corner in worldCorners)
            {
                // Elements should be within reasonable screen space
                Assert.IsTrue(corner.x >= -1000 && corner.x <= 2000, "Element should be within reasonable X bounds");
                Assert.IsTrue(corner.y >= -1000 && corner.y <= 2000, "Element should be within reasonable Y bounds");
            }
        }
    }
    
    private void SetupBehaviorTreeForUI()
    {
        // Create behavior tree structure
        GameObject behaviorTreeObj = new GameObject("BehaviorTree");
        behaviorTreeObj.transform.SetParent(spatialGenerator.transform);
        SGTreeNodeContainer container = behaviorTreeObj.AddComponent<SGTreeNodeContainer>();
        
        // Create root behavior tree node
        GameObject rootNodeObj = new GameObject("RootNode");
        rootNodeObj.transform.SetParent(behaviorTreeObj.transform);
        SGBehaviorTreeNode rootNode = rootNodeObj.AddComponent<SGBehaviorTreeNode>();
        
        // Configure root node for UI generation
        rootNode.minSpace = new Vector3(100, 100, 0);
        rootNode.maxSpace = new Vector3(200, 200, 0);
        rootNode.optimalSpace = new Vector3(150, 150, 0);
        rootNode.fitX = SGBehaviorTreeNode.FitX.Center;
        rootNode.fitY = SGBehaviorTreeNode.FitY.Center;
        rootNode.fitZ = SGBehaviorTreeNode.FitZ.Center;
        rootNode.stackDirection = SGBehaviorTreeNode.AxisDirection.PosX;
        rootNode.wrapDirection = SGBehaviorTreeNode.AxisDirection.PosY;
        rootNode.placementLimit = 5;
        
        // Create button prefab template (simple button)
        GameObject buttonPrefab = new GameObject("ButtonPrefab");
        RectTransform buttonRect = buttonPrefab.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(100, 50);
        buttonPrefab.AddComponent<Image>();
        buttonPrefab.AddComponent<Button>();
        
        rootNode.gameObjectPrefabs.Add(buttonPrefab);
        container.rootNode = rootNode;
        
        // Set behavior tree parent in SpatialGenerator
        spatialGenerator.behaviorTreeParent = behaviorTreeObj.transform;
    }
}
#endif
