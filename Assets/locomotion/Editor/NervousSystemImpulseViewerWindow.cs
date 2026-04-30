#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Realtime paper-doll viewer for NervousSystem: slots at body positions,
/// gray lines (sensory up / motor down) that light up when impulses are processed.
/// Step through in Play mode to see impulses up to brain and activations down to limbs.
/// </summary>
public class NervousSystemImpulseViewerWindow : EditorWindow
{
    private GameObject targetObject;
    private NervousSystem nervousSystem;
    private RagdollSystem ragdollSystem;
    private Vector2 scroll;
    private bool showEditPanel = true;
    private string selectedChannelForClear = "";
    private string injectChannel = "Spinal";
    private ImpulseType injectType = ImpulseType.Sensory;
    private string injectSource = "WorldInteraction";
    private string injectTarget = "Brain";
    private string injectMuscleGroup = "Torso";
    private float injectActivation = 0.5f;

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
        w.minSize = new Vector2(380, 520);
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
        if (nervousSystem != null && selectedChannelForClear == "" && nervousSystem.impulseChannels != null && nervousSystem.impulseChannels.Count > 0)
            selectedChannelForClear = nervousSystem.impulseChannels[0].channelName;
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
