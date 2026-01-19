#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Locomotion.Rig;

namespace Locomotion.EditorTools
{
    /// <summary>
    /// Cross-functional management matrix for ragdoll actors:
    /// rows = actors, columns = systems/concerns.
    /// </summary>
    public class RagdollSystemMatrixWindow : EditorWindow
    {
        private Vector2 scroll;
        private bool includeInactive = true;

        private List<GameObject> actors = new List<GameObject>();

        [MenuItem("Window/Locomotion/Ragdoll Systems Matrix")]
        public static void ShowWindow()
        {
            var w = GetWindow<RagdollSystemMatrixWindow>("Ragdoll Systems Matrix");
            w.minSize = new Vector2(760, 420);
            w.Refresh();
            w.Show();
        }

        private void OnFocus()
        {
            Refresh();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(120)))
            {
                Refresh();
            }
            includeInactive = EditorGUILayout.ToggleLeft("Include inactive", includeInactive, GUILayout.Width(140));
            if (GUILayout.Button("Copy TSV", GUILayout.Width(120)))
            {
                CopyTsv();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            if (actors.Count == 0)
            {
                EditorGUILayout.HelpBox("No ragdoll actors found. Add `RagdollSystem` or `RagdollActor` to your actor roots.", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawHeaderRow();
            for (int i = 0; i < actors.Count; i++)
            {
                DrawActorRow(actors[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void Refresh()
        {
            // Prefer explicit marker if present, but also include any RagdollSystem roots.
            var found = new HashSet<GameObject>();
            var markers = Resources.FindObjectsOfTypeAll<RagdollActor>();
            foreach (var m in markers)
            {
                if (m == null) continue;
                if (!includeInactive && !m.gameObject.activeInHierarchy) continue;
                found.Add(m.gameObject);
            }

            var ragdolls = Resources.FindObjectsOfTypeAll<RagdollSystem>();
            foreach (var r in ragdolls)
            {
                if (r == null) continue;
                if (!includeInactive && !r.gameObject.activeInHierarchy) continue;
                found.Add(r.gameObject);
            }

            actors = found
                .Where(go => go != null)
                .OrderBy(go => go.name)
                .ToList();
        }

        private void DrawHeaderRow()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            DrawCell("Actor", 200, bold: true);
            DrawCell("BoneMap", 70, true);
            DrawCell("Ragdoll", 70, true);
            DrawCell("Muscles", 70, true);
            DrawCell("Nervous", 70, true);
            DrawCell("Brain", 70, true);
            DrawCell("World", 70, true);
            DrawCell("Sensors", 70, true);
            DrawCell("Ears", 70, true);
            DrawCell("Fix", 80, true);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActorRow(GameObject actor)
        {
            if (actor == null) return;

            var boneMap = actor.GetComponent<BoneMap>();
            var ragdoll = actor.GetComponent<RagdollSystem>();
            var nervous = actor.GetComponent<NervousSystem>();
            var brain = actor.GetComponentInChildren<Brain>();
            var world = actor.GetComponent<WorldInteraction>();
            var anySensor = actor.GetComponentInChildren<Sensor>();
            var anyEar = actor.GetComponentInChildren<Locomotion.Audio.Ears>();

            bool hasMuscles = false;
            if (ragdoll != null && ragdoll.muscleGroups != null)
            {
                for (int i = 0; i < ragdoll.muscleGroups.Count; i++)
                {
                    var g = ragdoll.muscleGroups[i];
                    if (g != null && g.muscles != null && g.muscles.Count > 0)
                    {
                        hasMuscles = true;
                        break;
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();

            // Actor cell with select button
            EditorGUILayout.BeginHorizontal(GUILayout.Width(200));
            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeGameObject = actor;
            }
            EditorGUILayout.ObjectField(actor, typeof(GameObject), true, GUILayout.Width(145));
            EditorGUILayout.EndHorizontal();

            DrawStatusCell(boneMap != null);
            DrawStatusCell(ragdoll != null);
            DrawStatusCell(hasMuscles);
            DrawStatusCell(nervous != null);
            DrawStatusCell(brain != null);
            DrawStatusCell(world != null);
            DrawStatusCell(anySensor != null);
            DrawStatusCell(anyEar != null);

            // Fix
            using (new EditorGUI.DisabledScope(actor == null))
            {
                if (GUILayout.Button("Auto-fix", GUILayout.Width(80)))
                {
                    var rep = new RagdollAutoWire.Report();
                    var anim = RagdollAutoWire.FindAnimator(actor);
                    var bm = RagdollAutoWire.EnsureBoneMap(actor);
                    if (RagdollAutoWire.IsHumanoid(anim))
                        RagdollAutoWire.AutoFillHumanBoneMap(bm, anim);

                    RagdollAutoWire.EnsureGlobalSolvers(rep);
                    RagdollAutoWire.EnsureLocomotionCore(actor, rep);
                    RagdollAutoWire.EnsureSensors(actor, bm, anim, rep);
                    RagdollAutoWire.EnsureRagdollPhysicsHybrid(actor, anim, bm, rep);

                    Refresh();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawCell(string label, float width, bool bold)
        {
            var style = bold ? EditorStyles.boldLabel : EditorStyles.label;
            EditorGUILayout.LabelField(label, style, GUILayout.Width(width));
        }

        private static void DrawStatusCell(bool ok)
        {
            Color prev = GUI.color;
            GUI.color = ok ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.6f, 0.6f);
            GUILayout.Label(ok ? "OK" : "—", EditorStyles.helpBox, GUILayout.Width(70));
            GUI.color = prev;
        }

        private void CopyTsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Actor\tBoneMap\tRagdoll\tMuscles\tNervous\tBrain\tWorld\tSensors\tEars");

            foreach (var actor in actors)
            {
                if (actor == null) continue;
                bool boneMap = actor.GetComponent<BoneMap>() != null;
                bool ragdoll = actor.GetComponent<RagdollSystem>() != null;
                bool nervous = actor.GetComponent<NervousSystem>() != null;
                bool brain = actor.GetComponentInChildren<Brain>() != null;
                bool world = actor.GetComponent<WorldInteraction>() != null;
                bool sensors = actor.GetComponentInChildren<Sensor>() != null;
                bool ears = actor.GetComponentInChildren<Locomotion.Audio.Ears>() != null;

                bool muscles = false;
                var rs = actor.GetComponent<RagdollSystem>();
                if (rs != null && rs.muscleGroups != null)
                {
                    for (int i = 0; i < rs.muscleGroups.Count; i++)
                    {
                        var g = rs.muscleGroups[i];
                        if (g != null && g.muscles != null && g.muscles.Count > 0)
                        {
                            muscles = true;
                            break;
                        }
                    }
                }

                sb.Append(actor.name).Append('\t')
                    .Append(boneMap ? "1" : "0").Append('\t')
                    .Append(ragdoll ? "1" : "0").Append('\t')
                    .Append(muscles ? "1" : "0").Append('\t')
                    .Append(nervous ? "1" : "0").Append('\t')
                    .Append(brain ? "1" : "0").Append('\t')
                    .Append(world ? "1" : "0").Append('\t')
                    .Append(sensors ? "1" : "0").Append('\t')
                    .Append(ears ? "1" : "0")
                    .AppendLine();
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            EditorUtility.DisplayDialog("Copied", "Matrix copied to clipboard as TSV.", "OK");
        }
    }
}
#endif

