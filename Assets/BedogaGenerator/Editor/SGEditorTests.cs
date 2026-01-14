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
        
        // Create SpatialGenerator
        GameObject spatialGeneratorObj = new GameObject("SpatialGenerator");
        spatialGeneratorObj.transform.SetParent(testSceneRoot.transform);
        spatialGenerator = spatialGeneratorObj.AddComponent<SpatialGenerator>();
        spatialGenerator.mode = SpatialGenerator.GenerationMode.TwoDimensional;
        spatialGenerator.seed = 12345;
        spatialGenerator.generationSize = new Vector3(800, 600, 0);
        spatialGenerator.autoGenerateOnStart = false;
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
        
        // Generate UI
        spatialGenerator.Generate();
        
        // Find generated buttons
        Button[] buttons = Object.FindObjectsOfType<Button>();
        
        Assert.Greater(buttons.Length, 0, "Should generate at least one button");
        
        // Validate button clickability
        foreach (Button button in buttons)
        {
            Assert.IsNotNull(button, "Button should not be null");
            Assert.IsNotNull(button.GetComponent<RectTransform>(), "Button should have RectTransform");
            Assert.IsTrue(button.interactable, "Button should be interactable");
            
            // Check if button is part of the canvas
            Canvas buttonCanvas = button.GetComponentInParent<Canvas>();
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
        
        // Validate EventSystem
        Assert.IsNotNull(eventSystem, "EventSystem should exist");
        Assert.IsNotNull(eventSystem.currentInputModule, "EventSystem should have input module");
        
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
        Selectable[] selectables = Object.FindObjectsOfType<Selectable>();
        
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
        rootNode.alignPreference = SGBehaviorTreeNode.AlignmentPreference.Center;
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
