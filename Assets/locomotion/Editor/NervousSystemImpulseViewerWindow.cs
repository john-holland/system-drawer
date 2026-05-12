#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Realtime paper-doll viewer for NervousSystem: slots at body positions,
/// gray lines (sensory up / motor down) that light up when impulses are processed.
/// Step through in Play mode to see impulses up to brain and activations down to limbs.
/// Includes Brain summary and behavior-tree / related component discovery under the ragdoll root.
/// </summary>
public class NervousSystemImpulseViewerWindow : EditorWindow
{
    private GameObject targetObject;
    private NervousSystem nervousSystem;
    private RagdollSystem ragdollSystem;
    private Vector2 scroll;
    private Vector2 scrollBrainRefs;
    private bool showEditPanel = true;
    private bool showBrainRefsPanel = true;
    private string selectedChannelForClear = "";
    private string injectChannel = "Spinal";
    private ImpulseType injectType = ImpulseType.Sensory;
    private string injectSource = "WorldInteraction";
    private string injectTarget = "Brain";
    private string injectMuscleGroup = "Torso";
    private float injectActivation = 0.5f;

    private Brain[] cachedBrains;
    private BehaviorTree[] cachedBehaviorTrees;
    private AnimationBehaviorTree[] cachedAnimationBehaviorTrees;
    private PhysicsCardSolver[] cachedPhysicsCardSolvers;
    private RagdollAnimationSetManager[] cachedRagdollAnimationSetManagers;
    private WorldInteraction[] cachedWorldInteractions;

    private const float DollPadding = 20f;
    private const float SlotRadius = 12f;
    private static readonly Color GrayLine = new Color(0.4f, 0.4f, 0.45f);
    private static readonly Color SensoryHighlight = new Color(0.2f, 0.7f, 1f);
    private static readonly Color MotorHighlight = new Color(1f, 0.6f, 0.2f);

    private enum BodySlot
    {
        Brain,
        Head,
        Neck,
        Torso,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg
    }

    private static readonly Vector2[] SlotPositionsNormalized = new Vector2[]
    {
        new Vector2(0.5f, 0.22f),  // Brain
        new Vector2(0.5f, 0.15f),  // Head
        new Vector2(0.5f, 0.32f),  // Neck
        new Vector2(0.5f, 0.5f),   // Torso
        new Vector2(0.18f, 0.42f), // LeftArm
        new Vector2(0.82f, 0.42f), // RightArm
        new Vector2(0.35f, 0.82f), // LeftLeg
        new Vector2(0.65f, 0.82f)  // RightLeg
    };

    [MenuItem("Window/System Drawer/Physics/Nervous System Impulse Viewer", false, 400)]
    public static void ShowWindow()
    {
        var w = GetWindow<NervousSystemImpulseViewerWindow>("Impulse Viewer");
        w.minSize = new Vector2(420, 560);
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Repaint();
    }

    private void OnEditorUpdate()
    {
        if (Application.isPlaying && nervousSystem != null)
            Repaint();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target", GUILayout.Width(40));
        var newTarget = (GameObject)EditorGUILayout.ObjectField(targetObject, typeof(GameObject), true);
        if (newTarget != targetObject)
        {
            targetObject = newTarget;
            RefreshTarget();
        }
        if (GUILayout.Button("Refresh from scene", GUILayout.Width(120)))
            RefreshFromScene();
        EditorGUILayout.EndHorizontal();

        if (nervousSystem == null)
        {
            EditorGUILayout.HelpBox("Assign a GameObject with a NervousSystem component.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play mode to view live impulses. Gray lines will light up when stepping through.", MessageType.Warning);
        }

        EditorGUILayout.Space(8);

        float dollAreaHeight = 320f;
        float dollAreaWidth = position.width - DollPadding * 2f;
        if (dollAreaWidth > 280f)
            dollAreaWidth = 280f;
        var dollRect = GUILayoutUtility.GetRect(dollAreaWidth, dollAreaHeight);

        DrawPaperDoll(dollRect);

        EditorGUILayout.Space(8);

        showBrainRefsPanel = EditorGUILayout.Foldout(showBrainRefsPanel, "Brain & behavior references", true);
        if (showBrainRefsPanel)
            DrawBrainAndReferencesPanel();

        EditorGUILayout.Space(6);

        showEditPanel = EditorGUILayout.Foldout(showEditPanel, "Edit / Debug", true);
        if (showEditPanel)
        {
            DrawClearQueue();
            DrawInjectImpulse();
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshTarget()
    {
        nervousSystem = targetObject != null ? targetObject.GetComponent<NervousSystem>() : null;
        ragdollSystem = targetObject != null ? targetObject.GetComponent<RagdollSystem>() : null;
        if (ragdollSystem == null && nervousSystem != null)
            ragdollSystem = nervousSystem.GetComponentInParent<RagdollSystem>() ?? nervousSystem.GetComponentInChildren<RagdollSystem>(true);
        if (nervousSystem != null && selectedChannelForClear == "" && nervousSystem.impulseChannels != null && nervousSystem.impulseChannels.Count > 0)
            selectedChannelForClear = nervousSystem.impulseChannels[0].channelName;
        RefreshRelatedComponentCaches();
    }

    void RefreshRelatedComponentCaches()
    {
        cachedBrains = null;
        cachedBehaviorTrees = null;
        cachedAnimationBehaviorTrees = null;
        cachedPhysicsCardSolvers = null;
        cachedRagdollAnimationSetManagers = null;
        cachedWorldInteractions = null;
        if (nervousSystem == null)
            return;

        Transform root = nervousSystem.transform.root;
        cachedBrains = root.GetComponentsInChildren<Brain>(true);
        cachedBehaviorTrees = root.GetComponentsInChildren<BehaviorTree>(true);
        cachedAnimationBehaviorTrees = root.GetComponentsInChildren<AnimationBehaviorTree>(true);
        cachedPhysicsCardSolvers = root.GetComponentsInChildren<PhysicsCardSolver>(true);
        cachedRagdollAnimationSetManagers = root.GetComponentsInChildren<RagdollAnimationSetManager>(true);
        cachedWorldInteractions = root.GetComponentsInChildren<WorldInteraction>(true);
    }

    static string HierarchyPath(Transform t)
    {
        if (t == null)
            return "";
        var sb = new StringBuilder();
        while (t != null)
        {
            sb.Insert(0, "/" + t.name);
            t = t.parent;
        }

        return sb.ToString();
    }

    static void PingRow(Component c)
    {
        if (c == null)
            return;
        Selection.activeObject = c.gameObject;
        EditorGUIUtility.PingObject(c.gameObject);
    }

    void DrawBrainAndReferencesPanel()
    {
        if (nervousSystem == null)
            return;

        EditorGUILayout.HelpBox(
            "Components under the same hierarchy root as this NervousSystem (typical ragdoll / actor subtree).",
            MessageType.None);

        scrollBrainRefs = EditorGUILayout.BeginScrollView(scrollBrainRefs, GUILayout.MaxHeight(280f));

        EditorGUILayout.LabelField("Brains", EditorStyles.boldLabel);
        if (cachedBrains == null || cachedBrains.Length == 0)
            EditorGUILayout.LabelField("  (none)", EditorStyles.miniLabel);
        else
        {
            foreach (Brain brain in cachedBrains)
            {
                if (brain == null)
                    continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(HierarchyPath(brain.transform), EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("priority", GUILayout.Width(52));
                EditorGUILayout.LabelField(brain.priority.ToString(), GUILayout.Width(40));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Ping", GUILayout.Width(44)))
                    PingRow(brain);
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Behavior tree", brain.behaviorTree, typeof(BehaviorTree), true);
                EditorGUILayout.ObjectField("Body part", brain.attachedBodyPart, typeof(GameObject), true);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.LabelField(
                    $"Dual LSTM: {brain.enableDualLSTM}  |  Lie detection: {brain.enableLieDetection}  |  Connected brains: {(brain.connectedBrains != null ? brain.connectedBrains.Count : 0)}",
                    EditorStyles.miniLabel);

                bool playbackPaused = false;
                var ras = brain.GetComponentInParent<RagdollAnimationSetManager>();
                if (ras != null)
                    playbackPaused = ras.IsPaused || ras.IsStopped;

                if (Application.isPlaying && ras != null)
                    EditorGUILayout.LabelField($"RagdollAnimationSetManager playback: {(playbackPaused ? "paused/stopped (BT gated)" : "running")}", EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("BehaviorTree components", EditorStyles.boldLabel);
        if (cachedBehaviorTrees == null || cachedBehaviorTrees.Length == 0)
            EditorGUILayout.LabelField("  (none)", EditorStyles.miniLabel);
        else
        {
            foreach (BehaviorTree bt in cachedBehaviorTrees)
            {
                if (bt == null)
                    continue;
                EditorGUILayout.BeginHorizontal();
                string rootName = bt.rootNode != null ? bt.rootNode.gameObject.name : "(no root)";
                EditorGUILayout.LabelField($"{bt.gameObject.name}  →  root: {rootName}", EditorStyles.miniLabel);
                if (GUILayout.Button("Ping", GUILayout.Width(44)))
                    PingRow(bt);
                EditorGUILayout.EndHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Scene registry", bt.sceneObjectRegistry, typeof(SceneObjectRegistry), true);
                EditorGUI.EndDisabledGroup();
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("AnimationBehaviorTree", EditorStyles.boldLabel);
        if (cachedAnimationBehaviorTrees == null || cachedAnimationBehaviorTrees.Length == 0)
            EditorGUILayout.LabelField("  (none)", EditorStyles.miniLabel);
        else
        {
            foreach (AnimationBehaviorTree abt in cachedAnimationBehaviorTrees)
            {
                if (abt == null)
                    continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(HierarchyPath(abt.transform), EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Ping", GUILayout.Width(44)))
                    PingRow(abt);
                EditorGUILayout.EndHorizontal();
            }
        }

        if (ragdollSystem != null && ragdollSystem.animationTree != null)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("RagdollSystem.animationTree (primary)", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(ragdollSystem.animationTree, typeof(AnimationBehaviorTree), true);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Ping", GUILayout.Width(44)))
                PingRow(ragdollSystem.animationTree);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Related components", EditorStyles.boldLabel);
        DrawRelatedRowGroup("PhysicsCardSolver", cachedPhysicsCardSolvers);
        DrawRelatedRowGroup("RagdollAnimationSetManager", cachedRagdollAnimationSetManagers);
        DrawRelatedRowGroup("WorldInteraction", cachedWorldInteractions);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("NervousSystem lists", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField($"Consider components (serialized): {(nervousSystem.considerComponents != null ? nervousSystem.considerComponents.Count : 0)}", EditorStyles.miniLabel);

        EditorGUILayout.EndScrollView();
    }

    static void DrawRelatedRowGroup<T>(string title, T[] items) where T : Component
    {
        EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
        if (items == null || items.Length == 0)
        {
            EditorGUILayout.LabelField("  (none)", EditorStyles.miniLabel);
            return;
        }

        foreach (T c in items)
        {
            if (c == null)
                continue;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("  " + c.gameObject.name, GUILayout.MinWidth(80f));
            EditorGUILayout.LabelField(HierarchyPath(c.transform), EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Ping", GUILayout.Width(44)))
                PingRow(c);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void RefreshFromScene()
    {
        var all = Object.FindObjectsByType<NervousSystem>(FindObjectsSortMode.None);
        var found = all.Length > 0 ? all[0].gameObject : null;
        if (found != null)
        {
            targetObject = found;
            RefreshTarget();
        }
    }

    private static BodySlot? NormalizeToSlot(string label)
    {
        if (string.IsNullOrEmpty(label))
            return null;
        string n = label.ToLowerInvariant().Replace(" ", "").Replace("_", "");
        if (n.Contains("brain") || n == "center") return BodySlot.Brain;
        if (n.Contains("head")) return BodySlot.Head;
        if (n.Contains("neck")) return BodySlot.Neck;
        if (n.Contains("torso") || n.Contains("pelvis") || n == "spine") return BodySlot.Torso;
        if (n.Contains("lefthand") || n.Contains("leftarm") || n.Contains("leftshoulder") || n.Contains("leftupperarm") || n.Contains("leftforearm") || n.Contains("leftelbow") || n.Contains("leftcollarbone")) return BodySlot.LeftArm;
        if (n.Contains("righthand") || n.Contains("rightarm") || n.Contains("rightshoulder") || n.Contains("rightupperarm") || n.Contains("rightforearm") || n.Contains("rightelbow") || n.Contains("rightcollarbone")) return BodySlot.RightArm;
        if (n.Contains("leftleg") || n.Contains("leftknee") || n.Contains("leftshin") || n.Contains("leftfoot")) return BodySlot.LeftLeg;
        if (n.Contains("rightleg") || n.Contains("rightknee") || n.Contains("rightshin") || n.Contains("rightfoot")) return BodySlot.RightLeg;
        return null;
    }

    private Vector2 DollToWindow(Rect dollRect, Vector2 normalized)
    {
        return new Vector2(
            dollRect.x + normalized.x * dollRect.width,
            dollRect.y + normalized.y * dollRect.height
        );
    }

    private void DrawPaperDoll(Rect dollRect)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        List<ImpulseEventRecord> recent = Application.isPlaying ? nervousSystem.GetRecentImpulseEvents(80) : new List<ImpulseEventRecord>();

        HashSet<BodySlot> sensoryActive = new HashSet<BodySlot>();
        HashSet<BodySlot> motorActive = new HashSet<BodySlot>();
        foreach (var r in recent)
        {
            if (r.impulseType == ImpulseType.Sensory)
            {
                var slot = NormalizeToSlot(r.source);
                if (slot.HasValue && slot.Value != BodySlot.Brain)
                    sensoryActive.Add(slot.Value);
            }
            else
            {
                var slot = NormalizeToSlot(r.detail);
                if (!slot.HasValue)
                    slot = NormalizeToSlot(r.target);
                if (slot.HasValue && slot.Value != BodySlot.Brain)
                    motorActive.Add(slot.Value);
            }
        }

        Vector2 brainPos = DollToWindow(dollRect, SlotPositionsNormalized[(int)BodySlot.Brain]);

        Handles.BeginGUI();
        Vector3 brainPos3 = new Vector3(brainPos.x, brainPos.y, 0f);
        for (int i = 1; i < SlotPositionsNormalized.Length; i++)
        {
            var slot = (BodySlot)i;
            Vector2 slotPos = DollToWindow(dollRect, SlotPositionsNormalized[i]);
            Vector3 slotPos3 = new Vector3(slotPos.x, slotPos.y, 0f);

            bool sensoryOn = sensoryActive.Contains(slot);
            bool motorOn = motorActive.Contains(slot);

            Handles.color = sensoryOn ? SensoryHighlight : GrayLine;
            Handles.DrawLine(slotPos3, brainPos3, sensoryOn ? 3f : 1.5f);
            Handles.color = motorOn ? MotorHighlight : GrayLine;
            Handles.DrawLine(brainPos3, slotPos3, motorOn ? 3f : 1.5f);
        }
        Handles.EndGUI();

        for (int i = 0; i < SlotPositionsNormalized.Length; i++)
        {
            Vector2 pos = DollToWindow(dollRect, SlotPositionsNormalized[i]);
            var slotRect = new Rect(pos.x - SlotRadius, pos.y - SlotRadius, SlotRadius * 2, SlotRadius * 2);
            EditorGUI.DrawRect(slotRect, (BodySlot)i == BodySlot.Brain ? new Color(0.5f, 0.35f, 0.6f) : new Color(0.35f, 0.35f, 0.4f));
        }

        var labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        string[] labels = { "Brain", "Head", "Neck", "Torso", "L.Arm", "R.Arm", "L.Leg", "R.Leg" };
        for (int i = 0; i < SlotPositionsNormalized.Length; i++)
        {
            Vector2 pos = DollToWindow(dollRect, SlotPositionsNormalized[i]);
            var labelRect = new Rect(pos.x - 28, pos.y + SlotRadius + 2, 56, 16);
            GUI.Label(labelRect, labels[i], labelStyle);
        }
    }

    private void DrawClearQueue()
    {
        if (nervousSystem.impulseChannels == null || nervousSystem.impulseChannels.Count == 0)
        {
            EditorGUILayout.HelpBox("No impulse channels.", MessageType.None);
            return;
        }
        EditorGUILayout.BeginHorizontal();
        string[] names = nervousSystem.impulseChannels.Where(c => c != null).Select(c => c.channelName).ToArray();
        int idx = System.Array.IndexOf(names, selectedChannelForClear);
        if (idx < 0) idx = 0;
        idx = EditorGUILayout.Popup("Channel", idx, names);
        selectedChannelForClear = names[idx];
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        if (GUILayout.Button("Clear queue", GUILayout.Width(90)))
        {
            nervousSystem.ClearChannelQueue(selectedChannelForClear);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawInjectImpulse()
    {
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        injectChannel = EditorGUILayout.TextField("Channel", injectChannel);
        injectType = (ImpulseType)EditorGUILayout.EnumPopup("Type", injectType);
        injectSource = EditorGUILayout.TextField("Source", injectSource);
        injectTarget = EditorGUILayout.TextField("Target", injectTarget);
        if (injectType == ImpulseType.Motor)
        {
            injectMuscleGroup = EditorGUILayout.TextField("Muscle group", injectMuscleGroup);
            injectActivation = EditorGUILayout.Slider("Activation", injectActivation, 0f, 1f);
        }
        if (GUILayout.Button("Inject impulse"))
        {
            if (injectType == ImpulseType.Sensory)
            {
                var sensory = new SensoryData(Vector3.zero, Vector3.up, 1f, null, "Editor", null);
                var data = new ImpulseData(ImpulseType.Sensory, injectSource, injectTarget, sensory, 0);
                nervousSystem.SendImpulseUp(injectChannel, data);
            }
            else
            {
                var motor = new MotorData(injectMuscleGroup, injectActivation);
                var data = new ImpulseData(ImpulseType.Motor, injectSource, injectTarget, motor, 0);
                nervousSystem.SendImpulseDown(injectChannel, data);
            }
        }
        EditorGUI.EndDisabledGroup();
    }
}
#endif
