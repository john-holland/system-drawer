#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using Locomotion.EditorTools;
using Locomotion.Rig;

namespace Locomotion.EditorTools
{
    /// <summary>
    /// Wizard for fitting/wiring ragdoll actors: bones + physics + systems (brains/sensors/cards).
    /// Humanoid-first (HumanBodyBones) but stores mapping as trait ids so generic rigs can extend.
    /// </summary>
    public class RagdollFittingWizardWindow : EditorWindow
    {
        private GameObject actorRoot;
        private Animator animator;
        private BoneMap boneMap;

        // Options
        private bool ensureGlobalSolvers = true;
        private bool ensureLocomotionSystems = true;
        private bool ensureSensors = true;
        private bool ensureHybridRagdollPhysics = true;

        private Vector2 scroll;
        private ValidationReport validation = new ValidationReport();
        private RagdollAutoWire.Report lastReport;

        [MenuItem("Window/Locomotion/Ragdoll Fitting Wizard")]
        public static void ShowWindow()
        {
            var w = GetWindow<RagdollFittingWizardWindow>("Ragdoll Fitting Wizard");
            w.minSize = new Vector2(560, 650);
            w.Show();
        }

        private void OnEnable()
        {
            // Try prefill from selection
            if (actorRoot == null && Selection.activeGameObject != null)
            {
                actorRoot = Selection.activeGameObject;
                animator = RagdollAutoWire.FindAnimator(actorRoot);
            }
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            EditorGUILayout.Space(8);

            DrawActorSection();
            EditorGUILayout.Space(8);

            DrawOptions();
            EditorGUILayout.Space(8);

            DrawActions();
            EditorGUILayout.Space(8);

            DrawValidation();
            EditorGUILayout.Space(8);

            DrawSaveToPrefab();
            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                Validate();
            }
        }

        private void DrawHeader()
        {
            var title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
            EditorGUILayout.LabelField("Ragdoll Fitting Wizard", title);
            EditorGUILayout.HelpBox(
                "Fits an actor by auto-mapping bones (Humanoid-first), creating missing ragdoll physics (hybrid), " +
                "and wiring systems (RagdollSystem/NervousSystem/Brains/Sensors).",
                MessageType.Info
            );
        }

        private void DrawActorSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Actor", EditorStyles.boldLabel);

            actorRoot = (GameObject)EditorGUILayout.ObjectField("Actor Root", actorRoot, typeof(GameObject), true);
            if (actorRoot != null)
            {
                animator = (Animator)EditorGUILayout.ObjectField("Animator", animator != null ? animator : RagdollAutoWire.FindAnimator(actorRoot), typeof(Animator), true);
                boneMap = (BoneMap)EditorGUILayout.ObjectField("BoneMap", boneMap != null ? boneMap : actorRoot.GetComponent<BoneMap>(), typeof(BoneMap), true);

                EditorGUILayout.LabelField("Rig Type", RagdollAutoWire.IsHumanoid(animator) ? "Humanoid" : "Generic/Unknown");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Auto-Wire Options", EditorStyles.boldLabel);

            ensureGlobalSolvers = EditorGUILayout.ToggleLeft("Ensure global solvers (AudioPathingSolver, HierarchicalPathingSolver)", ensureGlobalSolvers);
            ensureLocomotionSystems = EditorGUILayout.ToggleLeft("Ensure locomotion core (RagdollSystem, NervousSystem, Brain, WorldInteraction, PhysicsCardSolver)", ensureLocomotionSystems);
            ensureSensors = EditorGUILayout.ToggleLeft("Ensure sensors (Eyes/Smell/Ears)", ensureSensors);
            ensureHybridRagdollPhysics = EditorGUILayout.ToggleLeft("Hybrid ragdoll build (create missing Rigidbody/Joints/Muscles on humanoid bones)", ensureHybridRagdollPhysics);

            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(actorRoot == null))
            {
                if (GUILayout.Button("Auto-Wire / Auto-Create", GUILayout.Height(30)))
                {
                    RunAutoWire();
                    Validate();
                }

                if (GUILayout.Button("Select Actor Root"))
                {
                    Selection.activeGameObject = actorRoot;
                }
            }

            if (lastReport != null)
            {
                if (lastReport.errors.Count > 0)
                    EditorGUILayout.HelpBox(string.Join("\n", lastReport.errors), MessageType.Error);
                if (lastReport.warnings.Count > 0)
                    EditorGUILayout.HelpBox(string.Join("\n", lastReport.warnings), MessageType.Warning);
                if (lastReport.info.Count > 0)
                    EditorGUILayout.HelpBox(string.Join("\n", lastReport.info), MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawValidation()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            if (actorRoot == null)
            {
                EditorGUILayout.HelpBox("Select an Actor Root to validate.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            DrawCheck("BoneMap present", validation.hasBoneMap);
            DrawCheck("Humanoid rig (Animator)", validation.isHumanoid);
            DrawCheck("RagdollSystem present", validation.hasRagdollSystem);
            DrawCheck("NervousSystem present", validation.hasNervousSystem);
            DrawCheck("Brain present", validation.hasBrain);
            DrawCheck("WorldInteraction present", validation.hasWorldInteraction);
            DrawCheck("At least one Sensor present", validation.hasAnySensor);
            DrawCheck("At least one Ear present", validation.hasAnyEar);

            EditorGUILayout.EndVertical();
        }

        private void DrawSaveToPrefab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Save", EditorStyles.boldLabel);

            bool canSave = validation.IsReadyToSave();
            using (new EditorGUI.DisabledScope(actorRoot == null || !canSave))
            {
                if (GUILayout.Button("Save to Prefab", GUILayout.Height(32)))
                {
                    SaveToPrefabFlow();
                }
            }

            if (!canSave)
            {
                EditorGUILayout.HelpBox("Save to Prefab becomes available once required wiring validates.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void RunAutoWire()
        {
            lastReport = new RagdollAutoWire.Report();

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Ragdoll Auto-Wire");

            if (ensureGlobalSolvers)
                RagdollAutoWire.EnsureGlobalSolvers(lastReport);

            if (boneMap == null)
                boneMap = RagdollAutoWire.EnsureBoneMap(actorRoot);

            if (animator == null)
                animator = RagdollAutoWire.FindAnimator(actorRoot);

            if (RagdollAutoWire.IsHumanoid(animator))
                RagdollAutoWire.AutoFillHumanBoneMap(boneMap, animator);

            if (ensureLocomotionSystems)
                RagdollAutoWire.EnsureLocomotionCore(actorRoot, lastReport);

            if (ensureSensors)
                RagdollAutoWire.EnsureSensors(actorRoot, boneMap, animator, lastReport);

            if (ensureHybridRagdollPhysics)
                RagdollAutoWire.EnsureRagdollPhysicsHybrid(actorRoot, animator, boneMap, lastReport);

            Undo.CollapseUndoOperations(group);

            EditorUtility.SetDirty(actorRoot);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private void Validate()
        {
            validation = new ValidationReport();
            if (actorRoot == null)
                return;

            animator = animator != null ? animator : RagdollAutoWire.FindAnimator(actorRoot);
            boneMap = boneMap != null ? boneMap : actorRoot.GetComponent<BoneMap>();

            validation.hasBoneMap = boneMap != null;
            validation.isHumanoid = RagdollAutoWire.IsHumanoid(animator);
            validation.hasRagdollSystem = actorRoot.GetComponent<RagdollSystem>() != null;
            validation.hasNervousSystem = actorRoot.GetComponent<NervousSystem>() != null;
            validation.hasBrain = actorRoot.GetComponentInChildren<Brain>() != null;
            validation.hasWorldInteraction = actorRoot.GetComponent<WorldInteraction>() != null;
            validation.hasAnySensor = actorRoot.GetComponentInChildren<Sensor>() != null;
            validation.hasAnyEar = actorRoot.GetComponentInChildren<Locomotion.Audio.Ears>() != null;
        }

        private static void DrawCheck(string label, bool ok)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(ok ? "✓" : "✗", GUILayout.Width(18));
            EditorGUILayout.LabelField(label);
            EditorGUILayout.EndHorizontal();
        }

        private void SaveToPrefabFlow()
        {
            // Prefab context detection
            bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(actorRoot);
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            bool inPrefabStage = stage != null && stage.prefabContentsRoot != null && actorRoot.transform.IsChildOf(stage.prefabContentsRoot.transform);

            if (inPrefabStage || isPrefabInstance)
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "Save to Prefab",
                    "Choose how to persist these changes:\n\n- Update Prefab: apply to the prefab you’re editing\n- Save New: save a new prefab asset/variant\n",
                    "Update Prefab",
                    "Cancel",
                    "Save New"
                );

                // 0=Update, 1=Cancel, 2=Save New
                if (choice == 1)
                    return;

                if (choice == 0)
                {
                    if (inPrefabStage)
                    {
                        PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
                        EditorUtility.DisplayDialog("Saved", "Prefab updated.", "OK");
                    }
                    else
                    {
                        PrefabUtility.ApplyPrefabInstance(actorRoot, InteractionMode.UserAction);
                        EditorUtility.DisplayDialog("Saved", "Prefab instance applied.", "OK");
                    }
                }
                else if (choice == 2)
                {
                    SaveNewPrefab();
                }
            }
            else
            {
                // Scene object default: offer Save New (non-destructive)
                SaveNewPrefab();
            }
        }

        private void SaveNewPrefab()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save New Prefab",
                actorRoot != null ? actorRoot.name : "RagdollActor",
                "prefab",
                "Choose location for the new prefab."
            );

            if (string.IsNullOrEmpty(path))
                return;

            PrefabUtility.SaveAsPrefabAssetAndConnect(actorRoot, path, InteractionMode.UserAction);
            EditorUtility.DisplayDialog("Saved", $"Saved new prefab:\n{path}", "OK");
        }

        private class ValidationReport
        {
            public bool hasBoneMap;
            public bool isHumanoid;
            public bool hasRagdollSystem;
            public bool hasNervousSystem;
            public bool hasBrain;
            public bool hasWorldInteraction;
            public bool hasAnySensor;
            public bool hasAnyEar;

            public bool IsReadyToSave()
            {
                // Minimal required set for "saveable wiring"
                return hasBoneMap && hasRagdollSystem && hasNervousSystem && hasBrain && hasWorldInteraction && hasAnySensor && hasAnyEar;
            }
        }
    }
}
#endif

