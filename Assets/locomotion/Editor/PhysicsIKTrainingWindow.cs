#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for IK animation training: sweep power/weights, run scenarios,
/// Overwrite/Append to PhysicsIKTrainingRunAsset, Abort Run.
/// </summary>
public class PhysicsIKTrainingWindow : EditorWindow
{
    private AnimationBehaviorTree animationTree;
    private PhysicsCardSolver solver;
    private PhysicsIKTrainingRunAsset runAsset;
    private PhysicsIKTrainingCategory testCategory = PhysicsIKTrainingCategory.Locomotion;

    private Rigidbody ragdollRigidbody;
    private bool includeFrozenAxisRuns = true;

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
        w.minSize = new Vector2(420, 480);
        w.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update -= OnTrainingUpdate;
        if (running)
            EditorApplication.update += OnTrainingUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnTrainingUpdate;
        if (running)
            ResetRagdollStateAfterRun();
        running = false;
    }

    /// <summary>Restore ragdoll rigidbody transform, velocity, constraints, and kinematic state captured at run start.</summary>
    private void ResetRagdollStateAfterRun()
    {
        RestoreRagdollKinematicState();
        if (!hasStoredRagdollState || ragdollRigidbody == null) return;
        if (!Application.isPlaying)
        {
            Undo.RecordObject(ragdollRigidbody.transform, "Reset ragdoll after training");
            Undo.RecordObject(ragdollRigidbody, "Reset ragdoll after training");
        }
        ragdollRigidbody.transform.position = storedRagdollPosition;
        ragdollRigidbody.transform.rotation = storedRagdollRotation;
        ragdollRigidbody.linearVelocity = storedRagdollVelocity;
        ragdollRigidbody.angularVelocity = storedRagdollAngularVelocity;
        ragdollRigidbody.constraints = storedRagdollConstraints;
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(ragdollRigidbody.transform);
            EditorUtility.SetDirty(ragdollRigidbody);
        }
        hasStoredRagdollState = false;
    }

    /// <summary>Set all ragdoll rigidbodies to non-kinematic so physics/IK can move joints; store previous state.</summary>
    private void SetRagdollNonKinematicForTraining()
    {
        if (!ensureRagdollNonKinematicDuringTraining || solver == null) return;
        var ragdollSystem = solver.GetComponent<RagdollSystem>();
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
                PhysicsIKTrainedSet withMetrics = PhysicsIKTrainingRunner.RunOne(solver, currentPreviewSet, testCategory, currentPreviewSet.seed, ragdollRigidbody, runAsset);
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

        int axisCount = (testCategory == PhysicsIKTrainingCategory.ToolUse && includeFrozenAxisRuns && ragdollRigidbody != null)
            ? PhysicsIKTrainingRunner.DefaultFrozenAxisOptions.Length
            : 1;
        int powerIndex = sweepIndex / axisCount;
        int axisIndex = sweepIndex % axisCount;
        float power = powerSteps[powerIndex];
        RigidbodyConstraints constraint = (testCategory == PhysicsIKTrainingCategory.ToolUse && includeFrozenAxisRuns && ragdollRigidbody != null)
            ? PhysicsIKTrainingRunner.DefaultFrozenAxisOptions[axisIndex]
            : RigidbodyConstraints.None;

        PhysicsIKTrainedSet set = solver != null
            ? PhysicsIKTrainedSet.FromSolver(solver, power)
            : PhysicsIKTrainedSet.Default();
        set.powerScale = power;
        set.rigidbodyConstraints = (int)constraint;
        set.seed = (int)(DateTime.UtcNow.Ticks % 1000000) + sweepIndex;
        set.tag = axisCount > 1 ? $"{testCategory}_p{powerIndex}_axis{axisIndex}" : $"{testCategory}_{sweepIndex}";

        // Apply set so scene (solver + ragdoll) shows this run
        set.ApplyTo(solver);
        if (ragdollRigidbody != null)
            set.ApplyConstraintsTo(ragdollRigidbody);

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

        EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
        animationTree = (AnimationBehaviorTree)EditorGUILayout.ObjectField("Animation Tree", animationTree, typeof(AnimationBehaviorTree), true);
        solver = (PhysicsCardSolver)EditorGUILayout.ObjectField("Physics Card Solver", solver, typeof(PhysicsCardSolver), true);
        runAsset = (PhysicsIKTrainingRunAsset)EditorGUILayout.ObjectField("Run Asset (save target)", runAsset, typeof(PhysicsIKTrainingRunAsset), false);
        testCategory = (PhysicsIKTrainingCategory)EditorGUILayout.EnumPopup("Test Category", testCategory);
        ragdollRigidbody = (Rigidbody)EditorGUILayout.ObjectField("Ragdoll Capsule Rigidbody", ragdollRigidbody, typeof(Rigidbody), true);
        GUI.enabled = ragdollRigidbody != null;
        if (GUILayout.Button("Reset IK constraints"))
        {
            if (ragdollRigidbody != null)
            {
                if (!Application.isPlaying)
                    Undo.RecordObject(ragdollRigidbody, "Reset IK constraints");
                ragdollRigidbody.constraints = RigidbodyConstraints.None;
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(ragdollRigidbody);
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
        int axisCountForLabel = (testCategory == PhysicsIKTrainingCategory.ToolUse && includeFrozenAxisRuns && ragdollRigidbody != null)
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
        if (ensureRagdollNonKinematicDuringTraining && solver != null)
        {
            var rs = solver.GetComponent<RagdollSystem>();
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
            if (ragdollRigidbody != null)
            {
                hasStoredRagdollState = true;
                storedRagdollPosition = ragdollRigidbody.transform.position;
                storedRagdollRotation = ragdollRigidbody.transform.rotation;
                storedRagdollVelocity = ragdollRigidbody.linearVelocity;
                storedRagdollAngularVelocity = ragdollRigidbody.angularVelocity;
                storedRagdollConstraints = ragdollRigidbody.constraints;
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
            int axisCount = (testCategory == PhysicsIKTrainingCategory.ToolUse && includeFrozenAxisRuns && ragdollRigidbody != null)
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
