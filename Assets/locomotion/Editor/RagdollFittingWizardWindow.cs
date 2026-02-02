#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using Locomotion.EditorTools;
using Locomotion.Rig;
using Locomotion.Musculature;
using System.Collections.Generic;
using System.Linq;

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
        private Vector2 bodyPartsScroll;
        private ValidationReport validation = new ValidationReport();
        private RagdollAutoWire.Report lastReport;

        // Body parts section
        private Dictionary<string, bool> categoryFoldouts = new Dictionary<string, bool>();
        private Dictionary<string, List<BodyPartSlot>> bodyPartCategories = new Dictionary<string, List<BodyPartSlot>>();
        private Dictionary<string, bool> handFoldouts = new Dictionary<string, bool>();

        private class BodyPartSlot
        {
            public string label;
            public GameObject assignedObject;
            public System.Type componentType;
            public BodySide? side; // null for non-sided parts
            public bool isMergeable; // true for knee/shin, elbow/forearm, etc.
            public string mergeGroup; // "knee-shin", "elbow-forearm", "shoulder-collarbone"
            public string roleName; // "Knee", "Shin", "Elbow", etc. for FindOrAdd methods
        }

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

            InitializeBodyPartCategories();
        }

        private void InitializeBodyPartCategories()
        {
            bodyPartCategories.Clear();
            categoryFoldouts.Clear();

            // Core body parts — Head + Jaw as merged body part group (head-jaw)
            var core = new List<BodyPartSlot>
            {
                new BodyPartSlot { label = "Head", componentType = typeof(RagdollHead), side = null, isMergeable = true, mergeGroup = "head-jaw", roleName = "Head" },
                new BodyPartSlot { label = "Jaw", componentType = typeof(RagdollJaw), side = null, isMergeable = true, mergeGroup = "head-jaw", roleName = "Jaw" },
                new BodyPartSlot { label = "Neck", componentType = typeof(RagdollNeck), side = null, roleName = "Neck" },
                new BodyPartSlot { label = "Torso", componentType = typeof(RagdollTorso), side = null, roleName = "Torso" },
                new BodyPartSlot { label = "Pelvis", componentType = typeof(RagdollPelvis), side = null, roleName = "Pelvis" }
            };
            bodyPartCategories["Core"] = core;
            categoryFoldouts["Core"] = true;

            // Arms - with mergeable groups
            var arms = new List<BodyPartSlot>
            {
                new BodyPartSlot { label = "Collarbone (L)", componentType = typeof(RagdollCollarbone), side = BodySide.Left, isMergeable = true, mergeGroup = "shoulder-collarbone", roleName = "Collarbone" },
                new BodyPartSlot { label = "Collarbone (R)", componentType = typeof(RagdollCollarbone), side = BodySide.Right, isMergeable = true, mergeGroup = "shoulder-collarbone", roleName = "Collarbone" },
                new BodyPartSlot { label = "Shoulder (L)", componentType = typeof(RagdollShoulder), side = BodySide.Left, isMergeable = true, mergeGroup = "shoulder-collarbone", roleName = "Shoulder" },
                new BodyPartSlot { label = "Shoulder (R)", componentType = typeof(RagdollShoulder), side = BodySide.Right, isMergeable = true, mergeGroup = "shoulder-collarbone", roleName = "Shoulder" },
                new BodyPartSlot { label = "Upperarm (L)", componentType = typeof(RagdollUpperarm), side = BodySide.Left, roleName = "Upperarm" },
                new BodyPartSlot { label = "Upperarm (R)", componentType = typeof(RagdollUpperarm), side = BodySide.Right, roleName = "Upperarm" },
                new BodyPartSlot { label = "Elbow (L)", componentType = typeof(RagdollElbow), side = BodySide.Left, isMergeable = true, mergeGroup = "elbow-forearm", roleName = "Elbow" },
                new BodyPartSlot { label = "Elbow (R)", componentType = typeof(RagdollElbow), side = BodySide.Right, isMergeable = true, mergeGroup = "elbow-forearm", roleName = "Elbow" },
                new BodyPartSlot { label = "Forearm (L)", componentType = typeof(RagdollForearm), side = BodySide.Left, isMergeable = true, mergeGroup = "elbow-forearm", roleName = "Forearm" },
                new BodyPartSlot { label = "Forearm (R)", componentType = typeof(RagdollForearm), side = BodySide.Right, isMergeable = true, mergeGroup = "elbow-forearm", roleName = "Forearm" },
                new BodyPartSlot { label = "Hand (L)", componentType = typeof(RagdollHand), side = BodySide.Left, roleName = "Hand" },
                new BodyPartSlot { label = "Hand (R)", componentType = typeof(RagdollHand), side = BodySide.Right, roleName = "Hand" }
            };
            bodyPartCategories["Arms"] = arms;
            categoryFoldouts["Arms"] = true;

            // Legs - with mergeable groups
            var legs = new List<BodyPartSlot>
            {
                new BodyPartSlot { label = "Leg (L)", componentType = typeof(RagdollLeg), side = BodySide.Left, roleName = "Leg" },
                new BodyPartSlot { label = "Leg (R)", componentType = typeof(RagdollLeg), side = BodySide.Right, roleName = "Leg" },
                new BodyPartSlot { label = "Knee (L)", componentType = typeof(RagdollKnee), side = BodySide.Left, isMergeable = true, mergeGroup = "knee-shin", roleName = "Knee" },
                new BodyPartSlot { label = "Knee (R)", componentType = typeof(RagdollKnee), side = BodySide.Right, isMergeable = true, mergeGroup = "knee-shin", roleName = "Knee" },
                new BodyPartSlot { label = "Shin (L)", componentType = typeof(RagdollShin), side = BodySide.Left, isMergeable = true, mergeGroup = "knee-shin", roleName = "Shin" },
                new BodyPartSlot { label = "Shin (R)", componentType = typeof(RagdollShin), side = BodySide.Right, isMergeable = true, mergeGroup = "knee-shin", roleName = "Shin" },
                new BodyPartSlot { label = "Foot (L)", componentType = typeof(RagdollFoot), side = BodySide.Left, roleName = "Foot" },
                new BodyPartSlot { label = "Foot (R)", componentType = typeof(RagdollFoot), side = BodySide.Right, roleName = "Foot" }
            };
            bodyPartCategories["Legs"] = legs;
            categoryFoldouts["Legs"] = true;
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            EditorGUILayout.Space(8);

            DrawActorSection();
            EditorGUILayout.Space(8);

            DrawBodyPartsSection();
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

        private void DrawBodyPartsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Body Parts", EditorStyles.boldLabel);

            if (actorRoot == null)
            {
                EditorGUILayout.HelpBox("Select an Actor Root to configure body parts.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var ragdollSystem = actorRoot.GetComponent<RagdollSystem>();
            if (ragdollSystem == null)
            {
                EditorGUILayout.HelpBox("RagdollSystem component not found. Run Auto-Wire first or add it manually.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            bodyPartsScroll = EditorGUILayout.BeginScrollView(bodyPartsScroll, GUILayout.Height(400));

            // Draw each category
            foreach (var category in bodyPartCategories.Keys)
            {
                if (!categoryFoldouts.ContainsKey(category))
                    categoryFoldouts[category] = true;

                categoryFoldouts[category] = EditorGUILayout.Foldout(categoryFoldouts[category], category, true);

                if (categoryFoldouts[category])
                {
                    EditorGUI.indentLevel++;
                    DrawBodyPartCategory(category, bodyPartCategories[category], ragdollSystem);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndScrollView();

            // Auto-Create Missing button
            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Auto-Create cannot be used during play mode. Please stop play mode first.", MessageType.Warning);
                }
                
                if (GUILayout.Button("Auto-Create Missing Components", GUILayout.Height(25)))
                {
                    if (Application.isPlaying)
                    {
                        EditorUtility.DisplayDialog("Play Mode Active", "Auto-Create cannot be used during play mode. Please stop play mode first.", "OK");
                        return;
                    }
                    
                    try
                    {
                        AutoCreateMissingComponents(ragdollSystem);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[RagdollFittingWizardWindow] Error during auto-create: {e.Message}\n{e.StackTrace}");
                        EditorUtility.DisplayDialog("Auto-Create Error", $"An error occurred during auto-create:\n{e.Message}", "OK");
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBodyPartCategory(string category, List<BodyPartSlot> slots, RagdollSystem ragdollSystem)
        {
            // Group mergeable components together
            var mergeGroups = slots.Where(s => s.isMergeable).GroupBy(s => s.mergeGroup).ToList();
            var nonMergeable = slots.Where(s => !s.isMergeable).ToList();

            // Draw non-mergeable slots first
            foreach (var slot in nonMergeable)
            {
                DrawBodyPartSlot(slot, ragdollSystem);
            }

            // Draw mergeable groups
            foreach (var mergeGroup in mergeGroups)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(mergeGroup.Key.Replace("-", " / "), EditorStyles.miniLabel);

                var groupSlots = mergeGroup.ToList();
                foreach (var slot in groupSlots)
                {
                    EditorGUI.indentLevel++;
                    DrawBodyPartSlot(slot, ragdollSystem);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawBodyPartSlot(BodyPartSlot slot, RagdollSystem ragdollSystem)
        {
            EditorGUILayout.BeginHorizontal();

            // Status indicator
            bool hasComponent = slot.assignedObject != null && slot.assignedObject.GetComponent(slot.componentType) != null;
            EditorGUILayout.LabelField(hasComponent ? "✓" : "✗", GUILayout.Width(18));

            // Object field
            EditorGUI.BeginChangeCheck();
            GameObject newObject = (GameObject)EditorGUILayout.ObjectField(slot.label, slot.assignedObject, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                slot.assignedObject = newObject;
                if (newObject != null)
                {
                    AddComponentToObject(slot, ragdollSystem);
                }
            }

            // Auto-Detect button
            if (GUILayout.Button("Auto", GUILayout.Width(50)))
            {
                AutoDetectBodyPart(slot, ragdollSystem);
            }

            EditorGUILayout.EndHorizontal();

            // Show hand details if this is a hand
            if (slot.componentType == typeof(RagdollHand) && slot.assignedObject != null)
            {
                DrawHandDetails(slot, ragdollSystem);
            }
        }

        private void DrawHandDetails(BodyPartSlot handSlot, RagdollSystem ragdollSystem)
        {
            var hand = handSlot.assignedObject.GetComponent<RagdollHand>();
            if (hand == null) return;

            string handKey = $"{handSlot.side}_{handSlot.assignedObject.GetInstanceID()}";
            if (!handFoldouts.ContainsKey(handKey))
                handFoldouts[handKey] = false;

            EditorGUI.indentLevel++;
            handFoldouts[handKey] = EditorGUILayout.Foldout(handFoldouts[handKey], "Fingers", true);

            if (handFoldouts[handKey])
            {
                EditorGUI.indentLevel++;
                if (GUILayout.Button("Auto-Fill Fingers", GUILayout.Width(150)))
                {
                    AutoFillFingers(hand);
                }

                // Show finger list
                if (hand.fingers != null && hand.fingers.Count > 0)
                {
                    for (int i = 0; i < hand.fingers.Count; i++)
                    {
                        var finger = hand.fingers[i];
                        if (finger != null)
                        {
                            EditorGUILayout.ObjectField($"{finger.kind} ({finger.side})", finger, typeof(RagdollFinger), true);
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        private void AutoDetectBodyPart(BodyPartSlot slot, RagdollSystem ragdollSystem)
        {
            if (ragdollSystem == null) return;

            UnityEngine.Component component = null;

            // Use reflection to find the appropriate FindOrAdd method
            System.Reflection.MethodInfo method = null;
            if (slot.side.HasValue)
            {
                method = ragdollSystem.GetType().GetMethod($"FindOrAdd{slot.roleName}", 
                    new System.Type[] { typeof(BodySide) });
                if (method != null)
                {
                    component = method.Invoke(ragdollSystem, new object[] { slot.side.Value }) as UnityEngine.Component;
                }
            }
            else
            {
                method = ragdollSystem.GetType().GetMethod($"FindOrAdd{slot.roleName}", new System.Type[0]);
                if (method != null)
                {
                    component = method.Invoke(ragdollSystem, null) as UnityEngine.Component;
                }
            }

            if (component != null)
            {
                slot.assignedObject = component.gameObject;
                EditorUtility.SetDirty(actorRoot);
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
        }

        private void AddComponentToObject(BodyPartSlot slot, RagdollSystem ragdollSystem)
        {
            if (slot.assignedObject == null) return;

            // Check if component already exists
            var existingComponent = slot.assignedObject.GetComponent(slot.componentType);
            if (existingComponent != null) return;

            // Safety check: check for other body part components
            var otherBodyParts = slot.assignedObject.GetComponents<RagdollBodyPart>()
                .Where(c => c.GetType() != slot.componentType)
                .ToList();

            if (otherBodyParts.Count > 0)
            {
                string componentNames = string.Join(", ", otherBodyParts.Select(c => c.GetType().Name));
                int choice = EditorUtility.DisplayDialogComplex(
                    "Body Part Component Conflict",
                    $"This GameObject already has body part components: {componentNames}\n\nAdd {slot.componentType.Name} anyway?",
                    "Yes",
                    "Cancel",
                    "No"
                );

                if (choice != 0) // 0 = Yes, 1 = Cancel, 2 = No
                    return;
            }

            // Add component
            Undo.AddComponent(slot.assignedObject, slot.componentType);
            var newComponent = slot.assignedObject.GetComponent(slot.componentType);

            // Set side if applicable
            if (slot.side.HasValue && newComponent is RagdollSidedBodyPart sidedPart)
            {
                sidedPart.side = slot.side.Value;
            }

            EditorUtility.SetDirty(slot.assignedObject);
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        private void AutoCreateMissingComponents(RagdollSystem ragdollSystem)
        {
            if (ragdollSystem == null) return;
            
            if (Application.isPlaying)
            {
                Debug.LogWarning("[RagdollFittingWizardWindow] AutoCreateMissingComponents cannot be used during play mode.");
                return;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Auto-Create Missing Body Parts");

            var created = new List<string>();

            // Group mergeable components by merge group and side
            var mergeableGroups = bodyPartCategories.Values
                .SelectMany(slots => slots)
                .Where(s => s.isMergeable)
                .GroupBy(s => new { s.mergeGroup, s.side })
                .ToList();

            foreach (var mergeGroup in mergeableGroups)
            {
                var slots = mergeGroup.ToList();
                if (slots.Count != 2) continue; // Should have exactly 2 components in a mergeable pair

                var slot1 = slots[0];
                var slot2 = slots[1];

                // Smart detection strategy
                // First, try to find existing bones/components
                AutoDetectBodyPart(slot1, ragdollSystem);
                AutoDetectBodyPart(slot2, ragdollSystem);

                bool has1 = slot1.assignedObject != null && slot1.assignedObject.GetComponent(slot1.componentType) != null;
                bool has2 = slot2.assignedObject != null && slot2.assignedObject.GetComponent(slot2.componentType) != null;

                if (has1 && !has2)
                {
                    // Component 1 exists, try to auto-create component 2
                    var comp1 = slot1.assignedObject.GetComponent(slot1.componentType);
                    TryAutoCreateFromComponent(comp1, slot2, created);
                }
                else if (!has1 && has2)
                {
                    // Component 2 exists, try to auto-create component 1
                    var comp2 = slot2.assignedObject.GetComponent(slot2.componentType);
                    TryAutoCreateFromComponent(comp2, slot1, created);
                }
                else if (!has1 && !has2)
                {
                    // Neither exists, create both in logical order
                    // Determine logical order based on component types
                    if (slot1.roleName == "Knee" || slot1.roleName == "Elbow" || slot1.roleName == "Shoulder")
                    {
                        // Create slot1 first (knee/elbow/shoulder), then slot2 (shin/forearm/collarbone)
                        CreateComponentGameObject(slot1, ragdollSystem, created);
                        TryAutoCreateFromComponent(slot1.assignedObject?.GetComponent(slot1.componentType), slot2, created);
                    }
                    else
                    {
                        // Create slot2 first, then slot1
                        CreateComponentGameObject(slot2, ragdollSystem, created);
                        TryAutoCreateFromComponent(slot2.assignedObject?.GetComponent(slot2.componentType), slot1, created);
                    }
                }
            }

            // Auto-link all components (defer to avoid OnGUI restrictions)
            // Use EditorApplication.update to ensure we're outside OnGUI context
            UnityEditor.EditorApplication.CallbackFunction validateAction = null;
            validateAction = () =>
            {
                EditorApplication.update -= validateAction;
                try
                {
                    if (ragdollSystem != null)
                    {
                        // Use reflection to call the private method directly instead of SendMessage
                        var method = ragdollSystem.GetType().GetMethod("ValidateBoneComponents", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (method != null)
                        {
                            method.Invoke(ragdollSystem, null);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[RagdollFittingWizardWindow] Error during validation callback: {e.Message}\n{e.StackTrace}");
                }
            };
            EditorApplication.update += validateAction;

            Undo.CollapseUndoOperations(group);

            if (created.Count > 0)
            {
                EditorUtility.DisplayDialog("Auto-Create Complete", $"Created {created.Count} components:\n" + string.Join("\n", created), "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Auto-Create Complete", "No missing components found. All components are present.", "OK");
            }

            EditorUtility.SetDirty(actorRoot);
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        private void TryAutoCreateFromComponent(UnityEngine.Component sourceComponent, BodyPartSlot targetSlot, List<string> created)
        {
            if (sourceComponent == null) return;

            // Use reflection to find and call auto-create method
            var autoCreateMethod = sourceComponent.GetType().GetMethod($"AutoCreate{targetSlot.roleName}", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (autoCreateMethod != null)
            {
                autoCreateMethod.Invoke(sourceComponent, null);
                var targetComponent = sourceComponent.gameObject.GetComponentsInChildren(targetSlot.componentType)
                    .FirstOrDefault(c => c != sourceComponent);
                if (targetComponent != null)
                {
                    targetSlot.assignedObject = targetComponent.gameObject;
                    created.Add($"{targetSlot.label} (auto-created from {sourceComponent.GetType().Name})");
                }
            }
        }

        private void CreateComponentGameObject(BodyPartSlot slot, RagdollSystem ragdollSystem, List<string> created)
        {
            // Try to find bone first
            AutoDetectBodyPart(slot, ragdollSystem);
            
            if (slot.assignedObject == null)
            {
                // Create new GameObject
                GameObject newObj = new GameObject($"{slot.side?.ToString() ?? ""}_{slot.roleName}");
                newObj.transform.SetParent(ragdollSystem.transform, worldPositionStays: false);
                slot.assignedObject = newObj;
                AddComponentToObject(slot, ragdollSystem);
                created.Add($"{slot.label} (new GameObject created)");
            }
        }

        private void AutoFillFingers(RagdollHand hand)
        {
            if (hand == null) return;

            Undo.RecordObject(hand, "Auto-Fill Fingers");

            // Find all RagdollFinger components as direct children
            var fingers = hand.GetComponentsInChildren<RagdollFinger>()
                .Where(f => f.transform.parent == hand.transform)
                .ToList();

            hand.fingers = fingers;

            // Auto-fill digits for each finger
            foreach (var finger in fingers)
            {
                if (finger == null) continue;

                Undo.RecordObject(finger, "Auto-Fill Digits");
                
                // Get or create digits for all direct child transforms
                var digits = new List<RagdollDigit>();
                
                // Process all direct children of the finger
                for (int i = 0; i < finger.transform.childCount; i++)
                {
                    Transform childTransform = finger.transform.GetChild(i);
                    
                    // Check if child already has a RagdollDigit component
                    RagdollDigit digit = childTransform.GetComponent<RagdollDigit>();
                    
                    if (digit == null)
                    {
                        // Create RagdollDigit component if it doesn't exist
                        Undo.AddComponent<RagdollDigit>(childTransform.gameObject);
                        digit = childTransform.GetComponent<RagdollDigit>();
                    }
                    
                    if (digit != null)
                    {
                        // Auto-assign digit number based on sibling index
                        digit.indexInFinger = i;
                        digits.Add(digit);
                    }
                }

                // Sort digits by sibling index to ensure correct order
                digits.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
                
                // Re-assign indexInFinger based on final order
                for (int i = 0; i < digits.Count; i++)
                {
                    if (digits[i] != null)
                    {
                        digits[i].indexInFinger = i;
                        Undo.RecordObject(digits[i], "Set Digit Index");
                    }
                }

                finger.digits = digits;

                // Mark last digit as caboose and add nailbed if applicable
                if (digits.Count > 0)
                {
                    var lastDigit = digits[digits.Count - 1];
                    if (lastDigit != null)
                    {
                        Undo.RecordObject(lastDigit, "Set Caboose Digit");
                        lastDigit.isCabooseDigit = true;
                        
                        // Add nailbed component if it doesn't exist
                        if (lastDigit.nailbed == null)
                        {
                            RagdollNailbed nailbed = lastDigit.GetComponent<RagdollNailbed>();
                            if (nailbed == null)
                            {
                                Undo.AddComponent<RagdollNailbed>(lastDigit.gameObject);
                                nailbed = lastDigit.GetComponent<RagdollNailbed>();
                            }
                            
                            if (nailbed != null)
                            {
                                lastDigit.nailbed = nailbed;
                                Undo.RecordObject(lastDigit, "Assign Nailbed");
                            }
                        }
                    }
                }
            }

            EditorUtility.SetDirty(hand);
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
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

            using (new EditorGUI.DisabledScope(actorRoot == null || Application.isPlaying))
            {
                if (Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Auto-Wire cannot be used during play mode. Please stop play mode first.", MessageType.Warning);
                }
                
                if (GUILayout.Button("Auto-Wire / Auto-Create", GUILayout.Height(30)))
                {
                    if (Application.isPlaying)
                    {
                        EditorUtility.DisplayDialog("Play Mode Active", "Auto-Wire cannot be used during play mode. Please stop play mode first.", "OK");
                        return;
                    }
                    
                    try
                    {
                        RunAutoWire();
                        Validate();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[RagdollFittingWizardWindow] Error during auto-wire: {e.Message}\n{e.StackTrace}");
                        EditorUtility.DisplayDialog("Auto-Wire Error", $"An error occurred during auto-wire:\n{e.Message}", "OK");
                    }
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

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Body Parts", EditorStyles.boldLabel);
            DrawCheck("Core body parts (Head/Neck/Torso/Pelvis)", validation.hasCoreBodyParts);
            DrawCheck("Arm body parts (at least one hand)", validation.hasArmBodyParts);
            DrawCheck("Leg body parts (at least one foot)", validation.hasLegBodyParts);

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
            if (Application.isPlaying)
            {
                Debug.LogWarning("[RagdollFittingWizardWindow] RunAutoWire cannot be used during play mode.");
                return;
            }
            
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
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
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

            // Validate body parts
            var ragdollSystem = actorRoot.GetComponent<RagdollSystem>();
            if (ragdollSystem != null)
            {
                validation.hasCoreBodyParts = ragdollSystem.headComponent != null && ragdollSystem.neckComponent != null 
                    && ragdollSystem.torsoComponent != null && ragdollSystem.pelvisComponent != null;
                validation.hasArmBodyParts = (ragdollSystem.leftHandComponent != null || ragdollSystem.rightHandComponent != null);
                validation.hasLegBodyParts = (ragdollSystem.leftFootComponent != null || ragdollSystem.rightFootComponent != null);
            }
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
            public bool hasCoreBodyParts;
            public bool hasArmBodyParts;
            public bool hasLegBodyParts;

            public bool IsReadyToSave()
            {
                // Minimal required set for "saveable wiring"
                return hasBoneMap && hasRagdollSystem && hasNervousSystem && hasBrain && hasWorldInteraction && hasAnySensor && hasAnyEar;
            }
        }
    }
}
#endif

