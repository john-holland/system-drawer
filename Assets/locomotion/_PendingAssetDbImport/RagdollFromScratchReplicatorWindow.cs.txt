#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Locomotion.EditorTools
{
    public sealed class RagdollFromScratchReplicatorWindow : EditorWindow
    {
        GameObject _source;
        string _outputName = "";
        string _outputFolder = "Assets/locomotion/Prefabs/ActorRagdolls";
        bool _copyColliders = true;
        bool _copyJointLimits = true;
        bool _stripAnimatorController = true;
        string _playerKey = "player";
        Vector2 _scroll;
        Vector2 _leftoverScroll;
        bool _showLeftovers = true;
        RagdollFromScratchResult _last;

        [MenuItem("Window/System Drawer/Ragdoll/From-Scratch Replicator")]
        public static void ShowWindow()
        {
            var w = GetWindow<RagdollFromScratchReplicatorWindow>("From-Scratch Replicator");
            w.minSize = new Vector2(420, 480);
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            EditorGUILayout.LabelField("Ragdoll From-Scratch Replicator", title);
            EditorGUILayout.HelpBox(
                "Clones skin/mesh + physics from a humanoid source, strips custom components " +
                "(listed as leftovers), then wires System-Drawer actor systems and optionally saves a prefab.",
                MessageType.Info);

            _source = (GameObject)EditorGUILayout.ObjectField("Source Ragdoll", _source, typeof(GameObject), true);
            if (_source != null)
            {
                var anim = RagdollAutoWire.FindAnimator(_source);
                EditorGUILayout.LabelField("Humanoid", RagdollAutoWire.IsHumanoid(anim) ? "Yes" : "No");
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            _outputName = EditorGUILayout.TextField("Output Name", _outputName);
            _outputFolder = EditorGUILayout.TextField("Prefab Folder", _outputFolder);
            _playerKey = EditorGUILayout.TextField("Also Register As Key", _playerKey);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            _copyColliders = EditorGUILayout.Toggle("Copy Colliders", _copyColliders);
            _copyJointLimits = EditorGUILayout.Toggle("Copy Joint Limits/Drives", _copyJointLimits);
            _stripAnimatorController = EditorGUILayout.Toggle("Strip Animator Controller", _stripAnimatorController);

            EditorGUILayout.Space(8);
            using (new EditorGUI.DisabledScope(_source == null || Application.isPlaying))
            {
                if (GUILayout.Button("Replicate In Scene", GUILayout.Height(28)))
                    Run(savePrefab: false);
                if (GUILayout.Button("Replicate And Save Prefab", GUILayout.Height(28)))
                    Run(savePrefab: true);
            }

            if (Application.isPlaying)
                EditorGUILayout.HelpBox("Exit Play Mode to replicate.", MessageType.Warning);

            DrawLastResult();
            EditorGUILayout.EndScrollView();
        }

        void Run(bool savePrefab)
        {
            var opts = new RagdollFromScratchOptions
            {
                copyColliders = _copyColliders,
                copyJointLimitsAndDrives = _copyJointLimits,
                stripAnimatorController = _stripAnimatorController,
                alsoRegisterAsPlayerKey = _playerKey,
                outputName = string.IsNullOrWhiteSpace(_outputName) ? null : _outputName.Trim(),
                outputFolder = string.IsNullOrWhiteSpace(_outputFolder)
                    ? "Assets/locomotion/Prefabs/ActorRagdolls"
                    : _outputFolder.Trim()
            };

            if (savePrefab)
            {
                string defaultName = string.IsNullOrEmpty(opts.outputName)
                    ? (_source != null ? _source.name + "_SystemDrawerRagdoll" : "RagdollActor")
                    : opts.outputName;
                RagdollFromScratchReplicator.EnsureFolder(opts.outputFolder);
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Replicated Ragdoll Prefab",
                    defaultName,
                    "prefab",
                    "Choose location for the new prefab.",
                    opts.outputFolder);
                if (string.IsNullOrEmpty(path))
                    return;
                _last = RagdollFromScratchReplicator.ReplicateAndSavePrefab(_source, opts, path);
            }
            else
            {
                _last = RagdollFromScratchReplicator.Replicate(_source, opts);
            }

            if (_last != null && _last.clone != null)
                Selection.activeGameObject = _last.clone;

            if (_last != null && !string.IsNullOrEmpty(_last.error))
                EditorUtility.DisplayDialog("Replicate Failed", _last.error, "OK");
            else if (_last != null && savePrefab && !string.IsNullOrEmpty(_last.prefabPath))
                EditorUtility.DisplayDialog("Saved", "Saved prefab:\n" + _last.prefabPath, "OK");
        }

        void DrawLastResult()
        {
            if (_last == null) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(_last.error))
                EditorGUILayout.HelpBox(_last.error, MessageType.Error);

            if (_last.clone != null)
                EditorGUILayout.ObjectField("Clone", _last.clone, typeof(GameObject), true);
            if (!string.IsNullOrEmpty(_last.prefabPath))
                EditorGUILayout.LabelField("Prefab", _last.prefabPath);

            EditorGUILayout.LabelField("Ready to save wiring", _last.isReadyToSave ? "Yes" : "Partial");
            if (_last.validationNotes != null && _last.validationNotes.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n", _last.validationNotes), MessageType.None);

            if (_last.wireReport != null)
            {
                if (_last.wireReport.errors.Count > 0)
                    EditorGUILayout.HelpBox(string.Join("\n", _last.wireReport.errors), MessageType.Error);
                if (_last.wireReport.warnings.Count > 0)
                    EditorGUILayout.HelpBox(string.Join("\n", _last.wireReport.warnings), MessageType.Warning);
                if (_last.wireReport.info.Count > 0)
                    EditorGUILayout.HelpBox(string.Join("\n", _last.wireReport.info), MessageType.None);
            }

            if (_last.mobility.HasWarnings)
            {
                foreach (var w in _last.mobility.warnings)
                    EditorGUILayout.HelpBox(w, MessageType.Warning);
            }

            _showLeftovers = EditorGUILayout.Foldout(_showLeftovers, "Components not brought into revamped ragdoll", true);
            if (_showLeftovers)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                int count = _last.leftovers?.entries?.Count ?? 0;
                EditorGUILayout.LabelField("Entries", count.ToString());
                _leftoverScroll = EditorGUILayout.BeginScrollView(_leftoverScroll, GUILayout.MinHeight(120), GUILayout.MaxHeight(260));
                EditorGUILayout.TextArea(_last.leftovers != null ? _last.leftovers.ToReadableText() : "(none)", GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy Leftover JSON"))
                {
                    EditorGUIUtility.systemCopyBuffer = _last.leftovers != null ? _last.leftovers.ToJson() : "{}";
                    Debug.Log("[From-Scratch Replicator] Leftover JSON copied to clipboard.");
                }
                if (GUILayout.Button("Log Leftovers"))
                    Debug.Log("[From-Scratch Replicator] Leftovers:\n" + (_last.leftovers?.ToReadableText() ?? "(none)"));
                if (GUILayout.Button("Clear"))
                {
                    _last.leftovers ??= new RagdollComponentLeftoverMap();
                    _last.leftovers.Clear();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }
    }
}
#endif
