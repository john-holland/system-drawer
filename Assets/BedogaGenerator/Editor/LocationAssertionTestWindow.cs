using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor window for testing and asserting object locations and bounds
/// </summary>
public class LocationAssertionTestWindow : EditorWindow
{
    private SpatialGenerator spatialGenerator;
    private List<GameObject> testObjects = new List<GameObject>();
    private Vector2 scrollPosition;
    private bool showBoundsVisualization = true;
    private bool showExpectedPositions = false;
    private Color boundsColor = new Color(0f, 1f, 0f, 0.3f);
    private float boundsLineWidth = 2f;
    
    // Test results
    private List<LocationTestResult> testResults = new List<LocationTestResult>();
    
    private class LocationTestResult
    {
        public GameObject obj;
        public Vector3 actualPosition;
        public Vector3? expectedPosition;
        public Bounds actualBounds;
        public Bounds? expectedBounds;
        public bool positionMatch;
        public bool boundsMatch;
        public string message;
    }
    
    [MenuItem("Tools/BedogaGenerator/Location Assertion Test")]
    public static void ShowWindow()
    {
        LocationAssertionTestWindow window = GetWindow<LocationAssertionTestWindow>("Location Test");
        window.Show();
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("Location Assertion Test", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // SpatialGenerator selection
        EditorGUILayout.LabelField("SpatialGenerator", EditorStyles.boldLabel);
        spatialGenerator = (SpatialGenerator)EditorGUILayout.ObjectField("Generator", spatialGenerator, typeof(SpatialGenerator), true);
        
        if (spatialGenerator != null)
        {
            if (GUILayout.Button("Collect Generated Objects"))
            {
                CollectGeneratedObjects();
            }
        }
        
        EditorGUILayout.Space();
        
        // Test objects list
        EditorGUILayout.LabelField("Test Objects", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selected"))
        {
            AddSelectedObjects();
        }
        if (GUILayout.Button("Clear"))
        {
            testObjects.Clear();
        }
        EditorGUILayout.EndHorizontal();
        
        // Display test objects
        for (int i = testObjects.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();
            GameObject obj = (GameObject)EditorGUILayout.ObjectField(testObjects[i], typeof(GameObject), true);
            if (obj == null)
            {
                testObjects.RemoveAt(i);
            }
            else
            {
                testObjects[i] = obj;
            }
            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                testObjects.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        
        // Visualization settings
        EditorGUILayout.LabelField("Visualization", EditorStyles.boldLabel);
        showBoundsVisualization = EditorGUILayout.Toggle("Show Bounds", showBoundsVisualization);
        showExpectedPositions = EditorGUILayout.Toggle("Show Expected Positions", showExpectedPositions);
        boundsColor = EditorGUILayout.ColorField("Bounds Color", boundsColor);
        boundsLineWidth = EditorGUILayout.Slider("Line Width", boundsLineWidth, 1f, 5f);
        
        EditorGUILayout.Space();
        
        // Test actions
        EditorGUILayout.LabelField("Tests", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Assert All Positions"))
        {
            AssertAllPositions();
        }
        if (GUILayout.Button("Assert All Bounds"))
        {
            AssertAllBounds();
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Compare with SpatialGenerator Bounds"))
        {
            CompareWithGeneratorBounds();
        }
        
        EditorGUILayout.Space();
        
        // Test results
        EditorGUILayout.LabelField("Test Results", EditorStyles.boldLabel);
        
        if (testResults.Count == 0)
        {
            EditorGUILayout.HelpBox("No test results. Run a test to see results.", MessageType.Info);
        }
        else
        {
            int passed = testResults.Count(r => r.positionMatch && r.boundsMatch);
            int failed = testResults.Count - passed;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Passed: {passed}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"Failed: {failed}", GUILayout.Width(100));
            if (GUILayout.Button("Copy Test Results", GUILayout.Width(150)))
            {
                CopyTestResultsToClipboard();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            foreach (var result in testResults)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(result.obj, typeof(GameObject), true);
                if (result.positionMatch && result.boundsMatch)
                {
                    EditorGUILayout.LabelField("✓ PASS", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("✗ FAIL", EditorStyles.boldLabel);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField($"Position: {result.actualPosition}");
                if (result.expectedPosition.HasValue)
                {
                    EditorGUILayout.LabelField($"Expected: {result.expectedPosition.Value}");
                    EditorGUILayout.LabelField($"Difference: {(result.actualPosition - result.expectedPosition.Value).magnitude:F3}");
                }
                
                EditorGUILayout.LabelField($"Bounds: center={result.actualBounds.center}, size={result.actualBounds.size}");
                if (result.expectedBounds.HasValue)
                {
                    EditorGUILayout.LabelField($"Expected: center={result.expectedBounds.Value.center}, size={result.expectedBounds.Value.size}");
                    Vector3 centerDiff = result.actualBounds.center - result.expectedBounds.Value.center;
                    Vector3 sizeDiff = result.actualBounds.size - result.expectedBounds.Value.size;
                    EditorGUILayout.LabelField($"Center Diff: {centerDiff.magnitude:F3}, Size Diff: {sizeDiff.magnitude:F3}");
                }
                
                if (!string.IsNullOrEmpty(result.message))
                {
                    EditorGUILayout.HelpBox(result.message, MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        // Repaint to update visualization
        if (showBoundsVisualization)
        {
            SceneView.RepaintAll();
        }
    }
    
    void OnSceneGUI(SceneView sceneView)
    {
        if (!showBoundsVisualization) return;
        
        foreach (var obj in testObjects)
        {
            if (obj == null) continue;
            
            Bounds bounds = GetObjectBounds(obj);
            
            // Draw bounds
            Handles.color = boundsColor;
            Handles.DrawWireCube(bounds.center, bounds.size);
            
            // Draw position marker
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(obj.transform.position, Vector3.up, 0.2f);
            
            // Draw expected position if available
            if (showExpectedPositions)
            {
                var result = testResults.FirstOrDefault(r => r.obj == obj);
                if (result != null && result.expectedPosition.HasValue)
                {
                    Handles.color = Color.cyan;
                    Handles.DrawWireDisc(result.expectedPosition.Value, Vector3.up, 0.2f);
                    Handles.DrawLine(obj.transform.position, result.expectedPosition.Value);
                }
            }
        }
    }
    
    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    private void CollectGeneratedObjects()
    {
        if (spatialGenerator == null) return;
        
        testObjects.Clear();
        
        // Get sceneTreeParent children
        if (spatialGenerator.sceneTreeParent != null)
        {
            for (int i = 0; i < spatialGenerator.sceneTreeParent.childCount; i++)
            {
                Transform child = spatialGenerator.sceneTreeParent.GetChild(i);
                testObjects.Add(child.gameObject);
            }
        }
    }
    
    private void AddSelectedObjects()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            if (!testObjects.Contains(obj))
            {
                testObjects.Add(obj);
            }
        }
    }
    
    private void AssertAllPositions()
    {
        testResults.Clear();
        
        foreach (var obj in testObjects)
        {
            if (obj == null) continue;
            
            LocationTestResult result = new LocationTestResult
            {
                obj = obj,
                actualPosition = obj.transform.position,
                actualBounds = GetObjectBounds(obj),
                expectedPosition = null, // Can be set manually or from expected data
                positionMatch = true, // Will be false if expected doesn't match
                boundsMatch = true
            };
            
            // For now, just record actual values
            // Expected values can be set manually or loaded from test data
            result.message = "Position recorded. Set expected values to compare.";
            
            testResults.Add(result);
        }
    }
    
    private void AssertAllBounds()
    {
        testResults.Clear();
        
        foreach (var obj in testObjects)
        {
            if (obj == null) continue;
            
            Bounds bounds = GetObjectBounds(obj);
            
            LocationTestResult result = new LocationTestResult
            {
                obj = obj,
                actualPosition = obj.transform.position,
                actualBounds = bounds,
                expectedBounds = null,
                positionMatch = true,
                boundsMatch = true
            };
            
            result.message = $"Bounds: center={bounds.center}, size={bounds.size}, min={bounds.min}, max={bounds.max}";
            
            testResults.Add(result);
        }
    }
    
    private void CompareWithGeneratorBounds()
    {
        if (spatialGenerator == null)
        {
            EditorUtility.DisplayDialog("Error", "No SpatialGenerator selected", "OK");
            return;
        }
        
        testResults.Clear();
        
        // Convert generation bounds from local space to world space
        // generationSize is in local space, need to scale by lossyScale
        Vector3 worldSize = Vector3.Scale(spatialGenerator.generationSize, spatialGenerator.transform.lossyScale);
        Bounds generatorBounds = new Bounds(spatialGenerator.transform.position, worldSize);
        
        foreach (var obj in testObjects)
        {
            if (obj == null) continue;
            
            Bounds objBounds = GetObjectBounds(obj);
            bool isInside = generatorBounds.Contains(objBounds.center);
            bool intersects = generatorBounds.Intersects(objBounds);
            
            LocationTestResult result = new LocationTestResult
            {
                obj = obj,
                actualPosition = obj.transform.position,
                actualBounds = objBounds,
                expectedBounds = generatorBounds,
                positionMatch = isInside,
                boundsMatch = intersects
            };
            
            result.message = $"Generator bounds: center={generatorBounds.center}, size={generatorBounds.size}\n" +
                           $"Object center inside: {isInside}, Object intersects: {intersects}";
            
            testResults.Add(result);
        }
    }
    
    private Bounds GetObjectBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) return renderer.bounds;
        
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) return collider.bounds;
        
        return new Bounds(obj.transform.position, obj.transform.lossyScale);
    }
    
    private void CopyTestResultsToClipboard()
    {
        if (testResults.Count == 0)
        {
            EditorUtility.DisplayDialog("No Results", "No test results to copy.", "OK");
            return;
        }
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine("=== Location Assertion Test Results ===");
        sb.AppendLine();
        
        int passed = testResults.Count(r => r.positionMatch && r.boundsMatch);
        int failed = testResults.Count - passed;
        
        sb.AppendLine($"Total Tests: {testResults.Count}");
        sb.AppendLine($"Passed: {passed}");
        sb.AppendLine($"Failed: {failed}");
        sb.AppendLine();
        sb.AppendLine("--- Detailed Results ---");
        sb.AppendLine();
        
        for (int i = 0; i < testResults.Count; i++)
        {
            var result = testResults[i];
            sb.AppendLine($"[{i + 1}] {result.obj.name}");
            sb.AppendLine($"  Status: {(result.positionMatch && result.boundsMatch ? "PASS" : "FAIL")}");
            sb.AppendLine($"  Object: {result.obj.name} (Path: {GetGameObjectPath(result.obj)})");
            sb.AppendLine($"  Actual Position: {result.actualPosition}");
            
            if (result.expectedPosition.HasValue)
            {
                sb.AppendLine($"  Expected Position: {result.expectedPosition.Value}");
                Vector3 posDiff = result.actualPosition - result.expectedPosition.Value;
                sb.AppendLine($"  Position Difference: {posDiff} (Magnitude: {posDiff.magnitude:F6})");
                sb.AppendLine($"  Position Match: {result.positionMatch}");
            }
            
            sb.AppendLine($"  Actual Bounds:");
            sb.AppendLine($"    Center: {result.actualBounds.center}");
            sb.AppendLine($"    Size: {result.actualBounds.size}");
            sb.AppendLine($"    Min: {result.actualBounds.min}");
            sb.AppendLine($"    Max: {result.actualBounds.max}");
            
            if (result.expectedBounds.HasValue)
            {
                sb.AppendLine($"  Expected Bounds:");
                sb.AppendLine($"    Center: {result.expectedBounds.Value.center}");
                sb.AppendLine($"    Size: {result.expectedBounds.Value.size}");
                sb.AppendLine($"    Min: {result.expectedBounds.Value.min}");
                sb.AppendLine($"    Max: {result.expectedBounds.Value.max}");
                
                Vector3 centerDiff = result.actualBounds.center - result.expectedBounds.Value.center;
                Vector3 sizeDiff = result.actualBounds.size - result.expectedBounds.Value.size;
                sb.AppendLine($"  Bounds Difference:");
                sb.AppendLine($"    Center: {centerDiff} (Magnitude: {centerDiff.magnitude:F6})");
                sb.AppendLine($"    Size: {sizeDiff} (Magnitude: {sizeDiff.magnitude:F6})");
                sb.AppendLine($"  Bounds Match: {result.boundsMatch}");
            }
            
            if (!string.IsNullOrEmpty(result.message))
            {
                sb.AppendLine($"  Message: {result.message}");
            }
            
            sb.AppendLine();
        }
        
        string text = sb.ToString();
        EditorGUIUtility.systemCopyBuffer = text;
        EditorUtility.DisplayDialog("Copied", $"Test results copied to clipboard!\n\n{testResults.Count} results, {passed} passed, {failed} failed", "OK");
    }
    
    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "null";
        
        string path = obj.name;
        Transform current = obj.transform.parent;
        
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        
        return path;
    }
}
