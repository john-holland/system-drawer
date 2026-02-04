#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor window for IK animation training: sweep power/weights, run scenarios,
/// Overwrite/Append to PhysicsIKTrainingRunAsset, Abort Run.
/// Includes interactive preview scene (embedded hierarchy) with option to use active actor or load actor into preview.
/// </summary>
public class PhysicsIKTrainingWindow : EditorWindow
{
    private AnimationBehaviorTree animationTree;
    private PhysicsCardSolver solver;
    private PhysicsIKTrainingRunAsset runAsset;
    private PhysicsIKTrainingCategory testCategory = PhysicsIKTrainingCategory.Locomotion;

    private Rigidbody ragdollRigidbody;
    private bool includeFrozenAxisRuns = true;

    /// <summary>When true, actor is loaded into preview scene (light + plane) and used for baking; when false, use active actor from main scene.</summary>
    private bool usePreviewSceneActor;
    /// <summary>When solver not set, optional prefab/root to instantiate in preview.</summary>
    private GameObject actorPrefabOrRoot;

    private const string PreviewSceneName = "IKTrainingPreview_Scene";
    private const int PreviewSize = 300;
    private Scene previewScene;
    private Camera previewCamera;
    private GameObject previewContainer;
    private RenderTexture previewRenderTexture;
    private float cameraOrbitYaw = 20f;
    private float cameraOrbitPitch = 15f;
    private float cameraDistance = 4f;
    private Vector3 previewPivot = Vector3.zero;
    private bool previewDragActive;
    private GameObject previewInstance;
    private PhysicsCardSolver previewInstanceSolver;
    private Rigidbody previewInstanceRagdollRigidbody;

    /// <summary>Number of power steps (0.5..2). Higher = more runs and finer coefficient granularity.</summary>
    private int powerStepCount = 4;
    private const int PowerStepCountMin = 2;
    private const int PowerStepCountMax = 32;

    [Tooltip("When on, apply each run to solver/ragdoll and wait Preview duration so you can watch the animation (requires Play Mode).")]
    private bool playAnimationDuringTraining;
    [Tooltip("Seconds to show each run when Play animation during training is on.")]
    private float previewDurationSeconds = 2f;
    [Tooltip("When on, set all ragdoll rigidbodies to non-kinematic at run start so joints can move (physics/IK); restore at end.")]
    private bool ensureRagdollNonKinematicDuringTraining = true;

    private bool abortRequested;
    private bool running;
    private bool previewing;
    private double previewEndTime;
    private PhysicsIKTrainedSet currentPreviewSet;
    private int sweepIndex;
    private int totalRuns;
    private float[] powerSteps;

    /// <summary>Captured when Start Training is clicked; restored when run ends or aborts.</summary>
    private bool hasStoredRagdollState;
    private Vector3 storedRagdollPosition;
    private Quaternion storedRagdollRotation;
    private Vector3 storedRagdollVelocity;
    private Vector3 storedRagdollAngularVelocity;
    private RigidbodyConstraints storedRagdollConstraints;
    /// <summary>Captured when Start Training enables non-kinematic; restored when run ends.</summary>
    private List<Rigidbody> storedRagdollRigidbodies;
    private List<bool> storedRagdollKinematic;
    private List<PhysicsIKTrainedSet> sweepResults = new List<PhysicsIKTrainedSet>();
    private Vector2 scroll;
    private int topCount = 10;
    private float compositeThreshold = 0f;

    [MenuItem("Window/Locomotion/IK Animation Training")]
    public static void ShowWindow()
    {
        var w = GetWindow<PhysicsIKTrainingWindow>("IK Animation Training");
        w.minSize = new Vector2(420, 520);
        w.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update -= OnTrainingUpdate;
        if (running)
            EditorApplication.update += OnTrainingUpdate;
        EnsurePreviewScene();
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnTrainingUpdate;
        if (running)
            ResetRagdollStateAfterRun();
        running = false;
        CleanupPreviewScene();
    }

    private void OnDestroy()
    {
        CleanupPreviewScene();
    }

    /// <summary>Solver used for training: preview instance when Load actor into preview, else main scene.</summary>
    private PhysicsCardSolver GetEffectiveSolver()
    {
        if (usePreviewSceneActor && previewInstanceSolver != null) return previewInstanceSolver;
        return solver;
    }

    /// <summary>Ragdoll rigidbody used for training: preview instance when Load actor into preview, else main scene.</summary>
    private Rigidbody GetEffectiveRagdollRigidbody()
    {
        if (usePreviewSceneActor && previewInstanceRagdollRigidbody != null) return previewInstanceRagdollRigidbody;
        return ragdollRigidbody;
    }

    private void EnsurePreviewScene()
    {
        if (previewScene.IsValid() && previewScene.isLoaded) return;
        previewScene = SceneManager.CreateScene(PreviewSceneName);
        var camGo = new GameObject("PreviewCamera");
        camGo.AddComponent<Camera>();
        previewCamera = camGo.GetComponent<Camera>();
        previewCamera.orthographic = false;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.22f, 0.22f, 0.24f, 1f);
        cameraDistance = 4f;
        cameraOrbitYaw = 20f;
        cameraOrbitPitch = 15f;
        UpdatePreviewCameraTransform();
        SceneManager.MoveGameObjectToScene(camGo, previewScene);
        previewContainer = new GameObject("PreviewContainer");
        SceneManager.MoveGameObjectToScene(previewContainer, previewScene);
        var lightGo = new GameObject("PreviewDirectionalLight");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        SceneManager.MoveGameObjectToScene(lightGo, previewScene);
        if (previewRenderTexture == null || !previewRenderTexture.IsCreated())
        {
            previewRenderTexture = new RenderTexture(PreviewSize, PreviewSize, 24);
            previewRenderTexture.Create();
        }
        previewCamera.targetTexture = previewRenderTexture;
        previewCamera.enabled = true;
    }

    private void UpdatePreviewCameraTransform()
    {
        if (previewCamera == null) return;
        float yawRad = cameraOrbitYaw * Mathf.Deg2Rad;
        float pitchRad = cameraOrbitPitch * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            Mathf.Sin(pitchRad),
            -Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
        ) * cameraDistance;
        previewCamera.transform.position = previewPivot + offset;
        previewCamera.transform.LookAt(previewPivot);
    }

    private void CleanupPreviewScene()
    {
        DestroyPreviewInstance();
        if (previewRenderTexture != null && previewRenderTexture.IsCreated())
            previewRenderTexture.Release();
        previewRenderTexture = null;
        if (previewScene.IsValid() && previewScene.isLoaded)
            SceneManager.UnloadSceneAsync(previewScene);
        previewScene = default;
        previewCamera = null;
        previewContainer = null;
    }

    private void DestroyPreviewInstance()
    {
        if (previewInstance != null)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(previewInstance);
            else
                UnityEngine.Object.DestroyImmediate(previewInstance);
            previewInstance = null;
        }
        previewInstanceSolver = null;
        previewInstanceRagdollRigidbody = null;
    }

    private GameObject GetActorRootForPreview()
    {
        if (solver != null) return solver.gameObject;
        return actorPrefabOrRoot;
    }

    private static Bounds GetHierarchyBounds(GameObject root)
    {
        Bounds b = new Bounds(root.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        foreach (var c in root.GetComponentsInChildren<Collider>(true))
        {
            if (first) { b = c.bounds; first = false; }
            else b.Encapsulate(c.bounds);
        }
        return b;
    }

    private static Rigidbody FindRagdollCapsuleRigidbody(PhysicsCardSolver s)
    {
        if (s == null) return null;
        var rs = s.GetComponent<RagdollSystem>();
        if (rs == null || rs.ragdollRoot == null) return null;
        return rs.ragdollRoot.GetComponent<Rigidbody>();
    }

    private void EnsurePreviewInstance()
    {
        if (!usePreviewSceneActor)
        {
            DestroyPreviewInstance();
            return;
        }
        GameObject root = GetActorRootForPreview();
        if (root == null) { DestroyPreviewInstance(); return; }
        if (previewInstance != null && (previewInstanceSolver == null || previewInstanceSolver.gameObject == null))
            DestroyPreviewInstance();
        if (previewInstance != null) return;

        EnsurePreviewScene();
        if (!previewScene.IsValid() || previewContainer == null) return;

        GameObject instance = Instantiate(root);
        instance.name = root.name + "(Preview)";
        SceneManager.MoveGameObjectToScene(instance, previewScene);
        previewInstance = instance;
        previewInstanceSolver = instance.GetComponent<PhysicsCardSolver>();
        previewInstanceRagdollRigidbody = FindRagdollCapsuleRigidbody(previewInstanceSolver);

        AddPreviewScenePlane();
        PositionActorBoundsAbovePlane(instance, 0f);
        previewPivot = GetHierarchyBounds(instance).center;
        UpdatePreviewCameraTransform();
    }

    private void AddPreviewScenePlane()
    {
        if (!previewScene.IsValid()) return;
        foreach (var go in previewScene.GetRootGameObjects())
            if (go.name == "PreviewFloor") return;
        var planeGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        planeGo.name = "PreviewFloor";
        planeGo.transform.position = Vector3.zero;
        planeGo.transform.rotation = Quaternion.identity;
        planeGo.transform.localScale = Vector3.one * 2f;
        SceneManager.MoveGameObjectToScene(planeGo, previewScene);
    }

    private void PositionActorBoundsAbovePlane(GameObject instance, float planeY, float epsilon = 0.01f)
    {
        Bounds b = GetHierarchyBounds(instance);
        float needY = planeY + epsilon - b.min.y;
        instance.transform.position += Vector3.up * needY;
    }

    /// <summary>When Use active actor: optional display-only clone in preview so preview rect shows something.</summary>
    private void EnsureDisplayCloneForActiveActor()
    {
        if (usePreviewSceneActor) return;
        if (GetActorRootForPreview() == null) { DestroyPreviewInstance(); return; }
        if (previewInstance != null) return;
        GameObject root = GetActorRootForPreview();
        EnsurePreviewScene();
        if (!previewScene.IsValid() || previewContainer == null) return;
        GameObject instance = Instantiate(root);
        instance.name = root.name + "(Display)";
        SceneManager.MoveGameObjectToScene(instance, previewScene);
        previewInstance = instance;
        previewInstanceSolver = null;
        previewInstanceRagdollRigidbody = null;
        previewPivot = GetHierarchyBounds(instance).center;
        UpdatePreviewCameraTransform();
    }

    private void DrawPreviewArea()
    {
        const float height = 300f;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(height + 8));
        GUILayout.Space(4);
        Rect previewRect = GUILayoutUtility.GetRect(PreviewSize, height);
        if (previewRect.width > 0 && previewRect.height > 0)
        {
            EnsurePreviewScene();
            if (usePreviewSceneActor)
                EnsurePreviewInstance();
            else
                EnsureDisplayCloneForActiveActor();

            if (previewRenderTexture != null && previewRenderTexture.IsCreated() && previewCamera != null)
            {
                UpdatePreviewCameraTransform();
                if (Event.current.type == EventType.Repaint)
                    previewCamera.Render();
                EditorGUI.DrawPreviewTexture(previewRect, previewRenderTexture, null, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.22f));
                GUI.Label(previewRect, "No preview (assign Physics Card Solver or Actor prefab)", EditorStyles.centeredGreyMiniLabel);
            }

            if (Event.current.rawType == EventType.MouseUp && Event.current.button == 0)
                previewDragActive = false;
            if (previewRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                    previewDragActive = true;
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                    previewDragActive = false;
                if (previewDragActive && Event.current.type == EventType.MouseDrag)
                {
                    cameraOrbitYaw += Event.current.delta.x * 0.5f;
                    cameraOrbitPitch -= Event.current.delta.y * 0.5f;
                    cameraOrbitPitch = Mathf.Clamp(cameraOrbitPitch, -89f, 89f);
                    Event.current.Use();
                    Repaint();
                }
                if (Event.current.type == EventType.ScrollWheel)
                {
                    cameraDistance = Mathf.Clamp(cameraDistance + Event.current.delta.y * 0.2f, 1f, 20f);
                    Event.current.Use();
                    Repaint();
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>Restore ragdoll rigidbody transform, velocity, constraints, and kinematic state captured at run start.</summary>
    private void ResetRagdollStateAfterRun()
    {
        RestoreRagdollKinematicState();
        var rb = GetEffectiveRagdollRigidbody();
        if (!hasStoredRagdollState || rb == null) return;
        if (!Application.isPlaying)
        {
            Undo.RecordObject(rb.transform, "Reset ragdoll after training");
            Undo.RecordObject(rb, "Reset ragdoll after training");
        }
        rb.transform.position = storedRagdollPosition;
        rb.transform.rotation = storedRagdollRotation;
        rb.linearVelocity = storedRagdollVelocity;
        rb.angularVelocity = storedRagdollAngularVelocity;
        rb.constraints = storedRagdollConstraints;
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(rb.transform);
            EditorUtility.SetDirty(rb);
        }
        hasStoredRagdollState = false;
    }

    /// <summary>Set all ragdoll rigidbodies to non-kinematic so physics/IK can move joints; store previous state.</summary>
    private void SetRagdollNonKinematicForTraining()
    {
        var effSolver = GetEffectiveSolver();
        if (!ensureRagdollNonKinematicDuringTraining || effSolver == null) return;
        var ragdollSystem = effSolver.GetComponent<RagdollSystem>();
        if (ragdollSystem == null || ragdollSystem.ragdollRoot == null) return;
        Rigidbody[] rbs = ragdollSystem.ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
        if (rbs == null || rbs.Length == 0) return;
        storedRagdollRigidbodies = new List<Rigidbody>(rbs.Length);
        storedRagdollKinematic = new List<bool>(rbs.Length);
        for (int i = 0; i < rbs.Length; i++)
        {
            Rigidbody rb = rbs[i];
            if (rb == null) continue;
            storedRagdollRigidbodies.Add(rb);
            storedRagdollKinematic.Add(rb.isKinematic);
            rb.isKinematic = false;
        }
    }

    /// <summary>Restore kinematic state of all ragdoll rigidbodies saved at run start.</summary>
    private void RestoreRagdollKinematicState()
    {
        if (storedRagdollRigidbodies == null || storedRagdollKinematic == null) return;
        int n = Mathf.Min(storedRagdollRigidbodies.Count, storedRagdollKinematic.Count);
        for (int i = 0; i < n; i++)
        {
            Rigidbody rb = storedRagdollRigidbodies[i];
            if (rb != null)
            {
                if (!Application.isPlaying)
                    Undo.RecordObject(rb, "Reset ragdoll kinematic after training");
                rb.isKinematic = storedRagdollKinematic[i];
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(rb);
            }
        }
        storedRagdollRigidbodies = null;
        storedRagdollKinematic = null;
    }

    private void OnTrainingUpdate()
    {
        if (!running || sweepResults == null || powerSteps == null)
        {
            EditorApplication.update -= OnTrainingUpdate;
            running = false;
            previewing = false;
            ResetRagdollStateAfterRun();
            Repaint();
            return;
        }

        if (abortRequested)
        {
            EditorApplication.update -= OnTrainingUpdate;
            running = false;
            previewing = false;
            abortRequested = false;
            ResetRagdollStateAfterRun();
            Repaint();
            return;
        }

        // Finish preview: wait duration elapsed -> record metrics and advance
        if (previewing)
        {
            if (EditorApplication.timeSinceStartup >= previewEndTime)
            {
                PhysicsIKTrainedSet withMetrics = PhysicsIKTrainingRunner.RunOne(GetEffectiveSolver(), currentPreviewSet, testCategory, currentPreviewSet.seed, GetEffectiveRagdollRigidbody(), runAsset);
                sweepResults.Add(withMetrics);
                sweepIndex++;
                previewing = false;
            }
            Repaint();
            return;
        }

        if (sweepIndex >= totalRuns)
        {
            EditorApplication.update -= OnTrainingUpdate;
            running = false;
            ResetRagdollStateAfterRun();
            Repaint();
            return;
        }

        var effSolver = GetEffectiveSolver();
        var effRagdoll = GetEffectiveRagdollRigidbody();
        int axisCount = (testCategory == PhysicsIKTrainingCategory.ToolUse && includeFrozenAxisRuns && effRagdoll != null)
            ? PhysicsIKTrainingRunner.DefaultFrozenAxisOptions.Length
            : 1;
        int powerIndex = sweepIndex / axisCount;
        int axisIndex = sweepIndex % axisCount;
        float power = powerSteps[powerIndex];
        RigidbodyConstraints constraint = (testCategory == PhysicsIKTrainingCategory.ToolUse && includeFrozenAxisRuns && effRagdoll != null)
            ? PhysicsIKTrainingRunner.DefaultFrozenAxisOptions[axisIndex]
            : RigidbodyConstraints.None;

        PhysicsIKTrainedSet set = effSolver != null
            ? PhysicsIKTrainedSet.FromSolver(effSolver, power)
            : PhysicsIKTrainedSet.Default();
        set.powerScale = power;
        set.rigidbodyConstraints = (int)constraint;
        set.seed = (int)(DateTime.UtcNow.Ticks % 1000000) + sweepIndex;
        set.tag = axisCount > 1 ? $"{testCategory}_p{powerIndex}_axis{axisIndex}" : $"{testCategory}_{sweepIndex}";

        // Apply set so scene (solver + ragdoll) shows this run
        set.ApplyTo(effSolver);
        if (effRagdoll != null)
            set.ApplyConstraintsTo(effRagdoll);

        if (playAnimationDuringTraining && Application.isPlaying)
        {
            previewing = true;
            currentPreviewSet = set;
            previewEndTime = EditorApplication.timeSinceStartup + (double)Mathf.Max(0.1f, previewDurationSeconds);
        }
        else
        {
            PhysicsIKTrainedSet withMetrics = PhysicsIKTrainingRunner.RunOne(solver, set, testCategory, set.seed, ragdollRigidbody, runAsset);
            sweepResults.Add(withMetrics);
            sweepIndex++;
        }
        Repaint();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("IK Animation Training", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        DrawPreviewArea();
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
        bool prevMode = usePreviewSceneActor;
        usePreviewSceneActor = EditorGUILayout.Toggle("Load actor into preview scene", usePreviewSceneActor);
        if (prevMode != usePreviewSceneActor)
        {
            if (usePreviewSceneActor)
                EnsurePreviewInstance();
            else
            {
                DestroyPreviewInstance();
                EnsureDisplayCloneForActiveActor();
            }
        }
        if (usePreviewSceneActor && GetActorRootForPreview() != null && (previewInstance == null || previewInstanceSolver == null))
            EnsurePreviewInstance();
        if (!usePreviewSceneActor && GetActorRootForPreview() != null && previewInstance == null)
            EnsureDisplayCloneForActiveActor();
        if (!usePreviewSceneActor && GetActorRootForPreview() == null)
            DestroyPreviewInstance();

        animationTree = (AnimationBehaviorTree)EditorGUILayout.ObjectField("Animation Tree", animationTree, typeof(AnimationBehaviorTree), true);
        EditorGUI.BeginChangeCheck();
        solver = (PhysicsCardSolver)EditorGUILayout.ObjectField("Physics Card Solver", solver, typeof(PhysicsCardSolver), true);
        if (EditorGUI.EndChangeCheck() && usePreviewSceneActor)
            EnsurePreviewInstance();
        runAsset = (PhysicsIKTrainingRunAsset)EditorGUILayout.ObjectField("Run Asset (save target)", runAsset, typeof(PhysicsIKTrainingRunAsset), false);
        testCategory = (PhysicsIKTrainingCategory)EditorGUILayout.EnumPopup("Test Category", testCategory);
        ragdollRigidbody = (Rigidbody)EditorGUILayout.ObjectField("Ragdoll Capsule Rigidbody", ragdollRigidbody, typeof(Rigidbody), true);
        actorPrefabOrRoot = (GameObject)EditorGUILayout.ObjectField("Actor prefab/root (if no solver)", actorPrefabOrRoot, typeof(GameObject), true);
        GUI.enabled = GetEffectiveRagdollRigidbody() != null;
        if (GUILayout.Button("Reset IK constraints"))
        {
            var rb = GetEffectiveRagdollRigidbody();
            if (rb != null)
            {
                if (!Application.isPlaying)
                    Undo.RecordObject(rb, "Reset IK constraints");
                rb.constraints = RigidbodyConstraints.None;
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(rb);
            }
        }
        GUI.enabled = true;
        if (testCategory == PhysicsIKTrainingCategory.ToolUse)
            includeFrozenAxisRuns = EditorGUILayout.Toggle("Include frozen-axis runs (tool)", includeFrozenAxisRuns);
        bool isClimbSwingPickRoll = testCategory == PhysicsIKTrainingCategory.Climb || testCategory == PhysicsIKTrainingCategory.Swing
            || testCategory == PhysicsIKTrainingCategory.Pick || testCategory == PhysicsIKTrainingCategory.Roll;
        if (isClimbSwingPickRoll && runAsset != null)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Card and Tool", EditorStyles.miniLabel);
            using (var runSo = new SerializedObject(runAsset))
            {
                var cardProp = runSo.FindProperty("cardSlot");
                var toolProp = runSo.FindProperty("toolSlot");
                if (cardProp != null)
                    EditorGUILayout.PropertyField(cardProp, new GUIContent("Card"), true);
                if (toolProp != null)
                    EditorGUILayout.PropertyField(toolProp, new GUIContent("Tool"), true);
                runSo.ApplyModifiedPropertiesWithoutUndo();
            }
            if (GUI.changed)
                EditorUtility.SetDirty(runAsset);
        }

        bool isThrow = testCategory == PhysicsIKTrainingCategory.Throw;
        if ((isThrow || (runAsset != null && runAsset.needsToBeThrown)) && runAsset != null)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Throw", EditorStyles.miniLabel);
            runAsset.needsToBeThrown = EditorGUILayout.Toggle("Needs to be thrown", runAsset.needsToBeThrown);
            runAsset.thrownObject = EditorGUILayout.ObjectField("Thrown Object (GameObject/Transform/bone)", runAsset.thrownObject, typeof(UnityEngine.Object), true);
            runAsset.throwHandMode = (ThrowHandMode)EditorGUILayout.EnumPopup("Hand mode", runAsset.throwHandMode);
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Throw goal (target)", EditorStyles.miniLabel);
            runAsset.throwTargetPosition = EditorGUILayout.Vector3Field("Throw target position", runAsset.throwTargetPosition);
            runAsset.throwGoalTarget = (GameObject)EditorGUILayout.ObjectField("Throw goal target", runAsset.throwGoalTarget, typeof(GameObject), true);
            if (runAsset.throwGoalTarget != null)
                EditorGUILayout.HelpBox("Throw target will use this object's position at runtime.", MessageType.None);
            if (isThrow && runAsset.throwAnimationTrees != null)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Throw animation trees", EditorStyles.miniLabel);
                SerializedObject so = new SerializedObject(runAsset);
                SerializedProperty listProp = so.FindProperty("throwAnimationTrees");
                if (listProp != null)
                    EditorGUILayout.PropertyField(listProp, true);
                EditorGUILayout.LabelField("Throw animation range (per-slot, meters)", EditorStyles.miniLabel);
                SerializedProperty rangeMinProp = so.FindProperty("throwAnimationRangeMin");
                SerializedProperty rangeMaxProp = so.FindProperty("throwAnimationRangeMax");
                if (rangeMinProp != null)
                    EditorGUILayout.PropertyField(rangeMinProp, true);
                if (rangeMaxProp != null)
                    EditorGUILayout.PropertyField(rangeMaxProp, true);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (GUI.changed) EditorUtility.SetDirty(runAsset);
        }

        if (testCategory == PhysicsIKTrainingCategory.Carry && runAsset != null)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Carry", EditorStyles.miniLabel);
            runAsset.carriedObject = (GameObject)EditorGUILayout.ObjectField("Carried object", runAsset.carriedObject, typeof(GameObject), true);
            runAsset.pleaseHold = EditorGUILayout.Toggle("Please hold (re-grasp if put down)", runAsset.pleaseHold);
            SerializedObject soCarry = new SerializedObject(runAsset);
            SerializedProperty carryTrees = soCarry.FindProperty("carryAnimationTrees");
            if (carryTrees != null)
                EditorGUILayout.PropertyField(carryTrees, new GUIContent("Carry animation trees"), true);
            soCarry.ApplyModifiedPropertiesWithoutUndo();
            if (GUI.changed) EditorUtility.SetDirty(runAsset);
        }

        if (testCategory == PhysicsIKTrainingCategory.Isometric && runAsset != null)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Isometric", EditorStyles.miniLabel);
            SerializedObject soIsometric = new SerializedObject(runAsset);
            SerializedProperty isometricCardProp = soIsometric.FindProperty("isometricCard");
            SerializedProperty isometricHoldProp = soIsometric.FindProperty("isometricHoldDuration");
            if (isometricCardProp != null)
                EditorGUILayout.PropertyField(isometricCardProp, new GUIContent("Isometric pose/card"), true);
            if (isometricHoldProp != null)
                EditorGUILayout.PropertyField(isometricHoldProp, new GUIContent("Hold duration (s)"));
            soIsometric.ApplyModifiedPropertiesWithoutUndo();
            if (GUI.changed) EditorUtility.SetDirty(runAsset);
        }

        if (testCategory == PhysicsIKTrainingCategory.Place && runAsset != null)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Place", EditorStyles.miniLabel);
            runAsset.placeObject = (GameObject)EditorGUILayout.ObjectField("Object to place", runAsset.placeObject, typeof(GameObject), true);
            runAsset.placeTargetPosition = EditorGUILayout.Vector3Field("Target position", runAsset.placeTargetPosition);
            runAsset.placeTargetRotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Target rotation (euler)", runAsset.placeTargetRotation.eulerAngles));
            SerializedObject soPlace = new SerializedObject(runAsset);
            SerializedProperty placeTrees = soPlace.FindProperty("placeAnimationTrees");
            if (placeTrees != null)
                EditorGUILayout.PropertyField(placeTrees, new GUIContent("Place animation trees"), true);
            soPlace.ApplyModifiedPropertiesWithoutUndo();
            if (GUI.changed) EditorUtility.SetDirty(runAsset);
        }

        if (testCategory == PhysicsIKTrainingCategory.Hit && runAsset != null)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Hit", EditorStyles.miniLabel);
            runAsset.hitTarget = (GameObject)EditorGUILayout.ObjectField("Hit target", runAsset.hitTarget, typeof(GameObject), true);
            SerializedObject soHit = new SerializedObject(runAsset);
            SerializedProperty hitLimbNames = soHit.FindProperty("hitLimbNames");
            if (hitLimbNames != null)
                EditorGUILayout.PropertyField(hitLimbNames, new GUIContent("Hit limb names"), true);
            soHit.ApplyModifiedPropertiesWithoutUndo();
            runAsset.hitUseTool = EditorGUILayout.Toggle("Use tool", runAsset.hitUseTool);
            if (runAsset.hitUseTool)
                runAsset.hitTool = (GameObject)EditorGUILayout.ObjectField("Hit tool", runAsset.hitTool, typeof(GameObject), true);
            SerializedObject soHit2 = new SerializedObject(runAsset);
            SerializedProperty hitTrees = soHit2.FindProperty("hitAnimationTrees");
            if (hitTrees != null)
                EditorGUILayout.PropertyField(hitTrees, new GUIContent("Hit animation trees"), true);
            soHit2.ApplyModifiedPropertiesWithoutUndo();
            if (GUI.changed) EditorUtility.SetDirty(runAsset);
        }

        if (testCategory == PhysicsIKTrainingCategory.Weightlift && runAsset != null)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Weightlift", EditorStyles.miniLabel);
            runAsset.weightliftTool = (GameObject)EditorGUILayout.ObjectField("Weight/tool", runAsset.weightliftTool, typeof(GameObject), true);
            runAsset.weightliftMuscleGroup = EditorGUILayout.TextField("Muscle group", runAsset.weightliftMuscleGroup);
            SerializedObject soWl = new SerializedObject(runAsset);
            SerializedProperty wlTrees = soWl.FindProperty("weightliftAnimationTrees");
            if (wlTrees != null)
                EditorGUILayout.PropertyField(wlTrees, new GUIContent("Weightlift animation trees"), true);
            soWl.ApplyModifiedPropertiesWithoutUndo();
            if (GUI.changed) EditorUtility.SetDirty(runAsset);
        }

        if (testCategory == PhysicsIKTrainingCategory.Catch && runAsset != null)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Catch", EditorStyles.miniLabel);
            runAsset.catchObject = (GameObject)EditorGUILayout.ObjectField("Object to catch", runAsset.catchObject, typeof(GameObject), true);
            SerializedObject soCatch = new SerializedObject(runAsset);
            SerializedProperty catchLimbNames = soCatch.FindProperty("catchLimbNames");
            if (catchLimbNames != null)
                EditorGUILayout.PropertyField(catchLimbNames, new GUIContent("Catch limb names"), true);
            SerializedProperty catchTrees = soCatch.FindProperty("catchAnimationTrees");
            if (catchTrees != null)
                EditorGUILayout.PropertyField(catchTrees, new GUIContent("Catch animation trees"), true);
            soCatch.ApplyModifiedPropertiesWithoutUndo();
            if (GUI.changed) EditorUtility.SetDirty(runAsset);
        }

        if (testCategory == PhysicsIKTrainingCategory.Shoot && runAsset != null)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Shoot", EditorStyles.miniLabel);
            runAsset.shootTarget = (GameObject)EditorGUILayout.ObjectField("Shoot target", runAsset.shootTarget, typeof(GameObject), true);
            runAsset.shootTargetPosition = EditorGUILayout.Vector3Field("Shoot target position", runAsset.shootTargetPosition);
            if (runAsset.shootTarget != null)
                EditorGUILayout.HelpBox("Shoot target will use this object's position at runtime when set.", MessageType.None);
            runAsset.shootLaunchedObject = EditorGUILayout.ObjectField("Launched object", runAsset.shootLaunchedObject, typeof(UnityEngine.Object), true);
            runAsset.shootHandMode = (ThrowHandMode)EditorGUILayout.EnumPopup("Hand mode", runAsset.shootHandMode);
            SerializedObject soShoot = new SerializedObject(runAsset);
            SerializedProperty shootTrees = soShoot.FindProperty("shootAnimationTrees");
            if (shootTrees != null)
                EditorGUILayout.PropertyField(shootTrees, new GUIContent("Shoot animation trees"), true);
            soShoot.ApplyModifiedPropertiesWithoutUndo();
            if (GUI.changed) EditorUtility.SetDirty(runAsset);
        }

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Sweep", EditorStyles.boldLabel);
        int newCount = EditorGUILayout.IntSlider("Run count / granularity (power steps)", powerStepCount, PowerStepCountMin, PowerStepCountMax);
        if (newCount != powerStepCount)
        {
            powerStepCount = Mathf.Clamp(newCount, PowerStepCountMin, PowerStepCountMax);
            powerSteps = null;
        }
        int axisCountForLabel = (testCategory == PhysicsIKTrainingCategory.ToolUse && includeFrozenAxisRuns && GetEffectiveRagdollRigidbody() != null)
            ? PhysicsIKTrainingRunner.DefaultFrozenAxisOptions.Length
            : 1;
        int totalPreview = powerStepCount * axisCountForLabel;
        EditorGUILayout.HelpBox($"Total runs this sweep: {powerStepCount} power × {axisCountForLabel} axis = {totalPreview}", MessageType.None);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        playAnimationDuringTraining = EditorGUILayout.Toggle("Play animation during training", playAnimationDuringTraining);
        if (playAnimationDuringTraining)
        {
            previewDurationSeconds = EditorGUILayout.Slider("Preview duration (s)", previewDurationSeconds, 0.5f, 10f);
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode and click Start Training to see the ragdoll animate each run.", MessageType.Info);
        }
        ensureRagdollNonKinematicDuringTraining = EditorGUILayout.Toggle("Ensure ragdoll non-kinematic during training", ensureRagdollNonKinematicDuringTraining);
        if (ensureRagdollNonKinematicDuringTraining && GetEffectiveSolver() != null)
        {
            var rs = GetEffectiveSolver().GetComponent<RagdollSystem>();
            if (rs == null || rs.ragdollRoot == null)
                EditorGUILayout.HelpBox("Solver has no RagdollSystem or ragdollRoot; kinematic state will not be changed.", MessageType.None);
        }
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Run", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !running;
        if (GUILayout.Button("Start Training", GUILayout.Height(24)))
        {
            RestoreRagdollKinematicState();
            var rb = GetEffectiveRagdollRigidbody();
            if (rb != null)
            {
                hasStoredRagdollState = true;
                storedRagdollPosition = rb.transform.position;
                storedRagdollRotation = rb.transform.rotation;
                storedRagdollVelocity = rb.linearVelocity;
                storedRagdollAngularVelocity = rb.angularVelocity;
                storedRagdollConstraints = rb.constraints;
            }
            else
                hasStoredRagdollState = false;
            SetRagdollNonKinematicForTraining();
            sweepResults = new List<PhysicsIKTrainedSet>();
            sweepIndex = 0;
            int steps = Mathf.Clamp(powerStepCount, PowerStepCountMin, PowerStepCountMax);
            powerSteps = new float[steps];
            float t0 = 0.5f;
            float t1 = 2f;
            for (int i = 0; i < steps; i++)
                powerSteps[i] = Mathf.Lerp(t0, t1, steps > 1 ? (float)i / (steps - 1) : 0.5f);
            int axisCount = (testCategory == PhysicsIKTrainingCategory.ToolUse && includeFrozenAxisRuns && GetEffectiveRagdollRigidbody() != null)
                ? PhysicsIKTrainingRunner.DefaultFrozenAxisOptions.Length
                : 1;
            totalRuns = powerSteps.Length * axisCount;
            abortRequested = false;
            running = true;
            EditorApplication.update += OnTrainingUpdate;
        }
        GUI.enabled = true;
        GUI.enabled = running;
        if (GUILayout.Button("Abort Run", GUILayout.Height(24)))
            abortRequested = true;
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (running)
        {
            string msg = previewing
                ? $"Previewing run {sweepIndex + 1} / {totalRuns} — watch the ragdoll (Abort to stop)"
                : $"Running... {sweepResults?.Count ?? 0} / {totalRuns} (click Abort Run to stop)";
            EditorGUILayout.HelpBox(msg, MessageType.Info);
        }
        else if (sweepResults != null && sweepResults.Count > 0)
            EditorGUILayout.HelpBox($"Completed {sweepResults.Count} runs.", MessageType.None);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = runAsset != null && sweepResults != null && sweepResults.Count > 0;
        if (GUILayout.Button("Overwrite", GUILayout.Height(22)))
        {
            runAsset.OverwriteWith(sweepResults.ToArray());
            if (runAsset.trainedSets != null && runAsset.trainedSets.Length > 0 &&
                PhysicsIKTrainingAggregator.SelectSuccessful(runAsset.trainedSets, topCount, compositeThreshold, out var agg))
            {
                runAsset.rangeDiamondMin = agg.rangeDiamondMin;
                runAsset.rangeDiamondMax = agg.rangeDiamondMax;
            }
            runAsset.displayName = runAsset.displayName ?? "IK Training Run";
            runAsset.creationTime = DateTime.UtcNow.ToString("o");
            runAsset.testCategory = testCategory;
            runAsset.animationTree = animationTree;
            runAsset.solver = solver;
            EditorUtility.SetDirty(runAsset);
            AssetDatabase.SaveAssets();
        }
        if (GUILayout.Button("Append", GUILayout.Height(22)))
        {
            runAsset.Append(sweepResults.ToArray());
            if (runAsset.trainedSets != null && runAsset.trainedSets.Length > 0 &&
                PhysicsIKTrainingAggregator.SelectSuccessful(runAsset.trainedSets, topCount, compositeThreshold, out var agg))
            {
                runAsset.rangeDiamondMin = agg.rangeDiamondMin;
                runAsset.rangeDiamondMax = agg.rangeDiamondMax;
            }
            EditorUtility.SetDirty(runAsset);
            AssetDatabase.SaveAssets();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        topCount = EditorGUILayout.IntField("Top count (for range diamond)", Mathf.Max(1, topCount));
        compositeThreshold = EditorGUILayout.FloatField("Composite threshold", compositeThreshold);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Last run results", EditorStyles.boldLabel);
        if (sweepResults != null && sweepResults.Count > 0)
        {
            for (int i = 0; i < Mathf.Min(sweepResults.Count, 20); i++)
            {
                var s = sweepResults[i];
                string axisStr = s.rigidbodyConstraints != 0 ? $" constraints={s.rigidbodyConstraints}" : "";
                EditorGUILayout.LabelField($"  [{i}] power={s.powerScale:F1}{axisStr} time={s.completionTime:F2} acc={s.accuracyScore:F2} powerUsed={s.powerUsed:F2}");
            }
            if (sweepResults.Count > 20)
                EditorGUILayout.LabelField($"  ... and {sweepResults.Count - 20} more");
        }
        else
            EditorGUILayout.LabelField("  (no results yet)");

        EditorGUILayout.EndScrollView();
    }
}
#endif
