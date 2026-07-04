#if UNITY_EDITOR
using Locomotion.Drink;
using Locomotion.Musculature;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Drink.Editor
{
    public sealed class DrinkJawTiltAuditWindow : EditorWindow
    {
        AnimationClip _clip;
        RagdollJaw _jaw;
        bool _insertKeys;

        [MenuItem("Window/Continuum/Drink Jaw Tilt Audit")]
        public static void Open()
        {
            GetWindow<DrinkJawTiltAuditWindow>("Drink Jaw Tilt Audit");
        }

        void OnGUI()
        {
            _clip = (AnimationClip)EditorGUILayout.ObjectField("Sip clip", _clip, typeof(AnimationClip), false);
            _jaw = (RagdollJaw)EditorGUILayout.ObjectField("Jaw", _jaw, typeof(RagdollJaw), true);
            _insertKeys = EditorGUILayout.Toggle("Insert corrective keys", _insertKeys);
            if (GUILayout.Button("Audit") && _clip != null && _jaw != null)
                RunAudit();
        }

        void RunAudit()
        {
            float maxOpen = 0f;
            var bindings = AnimationUtility.GetCurveBindings(_clip);
            foreach (var b in bindings)
            {
                if (!b.propertyName.ToLowerInvariant().Contains("jaw"))
                    continue;
                var curve = AnimationUtility.GetEditorCurve(_clip, b);
                if (curve == null) continue;
                foreach (var k in curve.keys)
                    maxOpen = Mathf.Max(maxOpen, k.value);
            }
            bool overshoot = maxOpen > _jaw.maxJawOpen * 10f;
            EditorUtility.DisplayDialog(
                "Jaw tilt audit",
                overshoot
                    ? $"Possible overshoot: max curve value {maxOpen:F3} vs jaw limit {_jaw.maxJawOpen:F3}"
                    : "Jaw rotation within expected range.",
                "OK");
            if (_insertKeys && overshoot && _jaw.transform != null)
            {
                var curve = AnimationCurve.EaseInOut(0f, 0f, _clip.length, _jaw.maxJawOpen);
                AnimationUtility.SetEditorCurve(
                    _clip,
                    EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAngles.x"),
                    curve);
                EditorUtility.SetDirty(_clip);
            }
        }
    }
}
#endif
