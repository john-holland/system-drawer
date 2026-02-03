#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Locomotion.Narrative;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>
    /// Editor-only snapshot of a NarrativeCalendarEvent for comparison and transaction (no side effects until ApplyTo).
    /// Uses a hidden holder calendar with one event so SerializedObject can deep-copy [SerializeReference] actions.
    /// </summary>
    public class NarrativeCalendarEventSnapshot
    {
        private readonly NarrativeCalendarAsset _holder;
        private readonly SerializedObject _holderSO;
        private const string PrefsSaveProjectKey = "CalendarEventEditorWindow.SaveProjectWhenCardSaved";

        /// <summary>Create a snapshot. Pass calendar's transform so the holder lives in the scene and PropertyField stays editable.</summary>
        public NarrativeCalendarEventSnapshot(Transform sceneParent = null)
        {
            var go = new GameObject("CalendarEventSnapshotHolder") { hideFlags = HideFlags.HideAndDontSave };
            if (sceneParent != null)
                go.transform.SetParent(sceneParent, false);
            _holder = go.AddComponent<NarrativeCalendarAsset>();
            _holder.events = new List<NarrativeCalendarEvent> { new NarrativeCalendarEvent() };
            _holderSO = new SerializedObject(_holder);
        }

        /// <summary>Deep copy from calendar.events[index] into this snapshot.</summary>
        public void CopyFrom(NarrativeCalendarAsset calendar, int index)
        {
            if (calendar == null || calendar.events == null || index < 0 || index >= calendar.events.Count)
                return;
            var calSO = new SerializedObject(calendar);
            var srcProp = calSO.FindProperty("events").GetArrayElementAtIndex(index);
            _holderSO.Update();
            var destProp = _holderSO.FindProperty("events").GetArrayElementAtIndex(0);
            CopySerializedPropertyRecursive(srcProp, destProp);
            _holderSO.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Deep copy from another snapshot (e.g. for Revert).</summary>
        public void CopyFrom(NarrativeCalendarEventSnapshot other)
        {
            if (other == null) return;
            other._holderSO.Update();
            _holderSO.Update();
            var src = other._holderSO.FindProperty("events").GetArrayElementAtIndex(0);
            var dest = _holderSO.FindProperty("events").GetArrayElementAtIndex(0);
            CopySerializedPropertyRecursive(src, dest);
            _holderSO.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Deep copy from this snapshot into calendar.events[index].</summary>
        public void ApplyTo(NarrativeCalendarAsset calendar, int index)
        {
            if (calendar == null || index < 0)
                return;
            if (calendar.events == null)
                calendar.events = new List<NarrativeCalendarEvent>();
            while (calendar.events.Count <= index)
                calendar.events.Add(new NarrativeCalendarEvent());
            _holderSO.Update();
            var srcProp = _holderSO.FindProperty("events").GetArrayElementAtIndex(0);
            var calSO = new SerializedObject(calendar);
            var destProp = calSO.FindProperty("events").GetArrayElementAtIndex(index);
            CopySerializedPropertyRecursive(srcProp, destProp);
            calSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CopySerializedPropertyRecursive(SerializedProperty src, SerializedProperty dest)
        {
            if (src == null || dest == null || src.propertyType != dest.propertyType) return;
            switch (src.propertyType)
            {
                case SerializedPropertyType.Integer: dest.intValue = src.intValue; break;
                case SerializedPropertyType.Boolean: dest.boolValue = src.boolValue; break;
                case SerializedPropertyType.Float: dest.floatValue = src.floatValue; break;
                case SerializedPropertyType.String: dest.stringValue = src.stringValue; break;
                case SerializedPropertyType.ObjectReference: dest.objectReferenceValue = src.objectReferenceValue; break;
                case SerializedPropertyType.Enum: dest.enumValueIndex = src.enumValueIndex; break;
                case SerializedPropertyType.Vector3: dest.vector3Value = src.vector3Value; break;
                case SerializedPropertyType.Generic:
                    if (src.isArray)
                    {
                        dest.arraySize = src.arraySize;
                        for (int i = 0; i < src.arraySize; i++)
                        {
                            var srcEl = src.GetArrayElementAtIndex(i);
                            var destEl = dest.GetArrayElementAtIndex(i);
                            CopySerializedPropertyRecursive(srcEl, destEl);
                        }
                    }
                    else
                    {
                        var srcIter = src.Copy();
                        var destIter = dest.Copy();
                        var end = src.GetEndProperty();
                        if (srcIter.Next(true))
                        {
                            do
                            {
                                if (SerializedProperty.EqualContents(srcIter, end)) break;
                                var destChild = dest.FindPropertyRelative(srcIter.name);
                                if (destChild != null)
                                    CopySerializedPropertyRecursive(srcIter, destChild);
                            } while (srcIter.Next(false));
                        }
                    }
                    break;
            }
        }

        public NarrativeCalendarEvent Event => _holder != null && _holder.events != null && _holder.events.Count > 0 ? _holder.events[0] : null;

        public SerializedObject GetSerializedObject() => _holderSO;

        public override bool Equals(object obj)
        {
            if (obj is NarrativeCalendarEventSnapshot other)
                return ContentEquals(other);
            return false;
        }

        public override int GetHashCode()
        {
            var evt = Event;
            if (evt == null) return 0;
            return HashCode.Combine(evt.id, evt.title, evt.startDateTime.GetHashCode(), evt.durationSeconds);
        }

        /// <summary>Value equality for diff and status icon.</summary>
        public bool ContentEquals(NarrativeCalendarEventSnapshot other)
        {
            if (other == null) return false;
            var a = Event;
            var b = other.Event;
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.id != b.id || a.title != b.title || a.notes != b.notes || a.durationSeconds != b.durationSeconds)
                return false;
            if (!a.startDateTime.Equals(b.startDateTime)) return false;
            if (a.tree != b.tree) return false;
            if ((a.spatiotemporalVolume.HasValue != b.spatiotemporalVolume.HasValue) ||
                (a.spatiotemporalVolume.HasValue && !Bounds4ContentEquals(a.spatiotemporalVolume.Value, b.spatiotemporalVolume.Value)))
                return false;
            if (!ListEquals(a.tags, b.tags) || !ListEquals(a.positionKeys, b.positionKeys))
                return false;
            if ((a.actions == null) != (b.actions == null) || (a.actions != null && a.actions.Count != b.actions.Count))
                return false;
            return true;
        }

        private static bool Bounds4ContentEquals(Bounds4 a, Bounds4 b)
        {
            return a.center == b.center && a.size == b.size && Mathf.Approximately(a.tMin, b.tMin) && Mathf.Approximately(a.tMax, b.tMax);
        }

        public static bool ListEquals(List<string> a, List<string> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        public static bool GetSaveProjectWhenCardSavedPref() => EditorPrefs.GetBool(PrefsSaveProjectKey, false);
        public static void SetSaveProjectWhenCardSavedPref(bool value) => EditorPrefs.SetBool(PrefsSaveProjectKey, value);
    }

    /// <summary>One field diff for the red card.</summary>
    public struct CalendarEventFieldDiff
    {
        public string fieldName;
        public string valueBefore;
        public string valueAfter;
    }

    public static class CalendarEventDiffHelper
    {
        public static List<CalendarEventFieldDiff> GetDiff(NarrativeCalendarEventSnapshot before, NarrativeCalendarEventSnapshot working)
        {
            var list = new List<CalendarEventFieldDiff>();
            if (before == null || working == null) return list;
            var a = before.Event;
            var b = working.Event;
            if (a == null || b == null) return list;

            if (a.id != b.id) list.Add(new CalendarEventFieldDiff { fieldName = "id", valueBefore = a.id, valueAfter = b.id });
            if (a.title != b.title) list.Add(new CalendarEventFieldDiff { fieldName = "title", valueBefore = a.title, valueAfter = b.title });
            if (a.notes != b.notes) list.Add(new CalendarEventFieldDiff { fieldName = "notes", valueBefore = a.notes, valueAfter = b.notes });
            if (a.durationSeconds != b.durationSeconds) list.Add(new CalendarEventFieldDiff { fieldName = "durationSeconds", valueBefore = a.durationSeconds.ToString(), valueAfter = b.durationSeconds.ToString() });
            if (!a.startDateTime.Equals(b.startDateTime)) list.Add(new CalendarEventFieldDiff { fieldName = "startDateTime", valueBefore = a.startDateTime.ToString(), valueAfter = b.startDateTime.ToString() });
            if (a.tree != b.tree) list.Add(new CalendarEventFieldDiff { fieldName = "tree", valueBefore = a.tree != null ? a.tree.name : "null", valueAfter = b.tree != null ? b.tree.name : "null" });
            if (a.spatiotemporalVolume.HasValue != b.spatiotemporalVolume.HasValue)
                list.Add(new CalendarEventFieldDiff { fieldName = "spatiotemporalVolume", valueBefore = a.spatiotemporalVolume.HasValue ? "set" : "null", valueAfter = b.spatiotemporalVolume.HasValue ? "set" : "null" });
            else if (a.spatiotemporalVolume.HasValue && b.spatiotemporalVolume.HasValue)
            {
                var va = a.spatiotemporalVolume.Value;
                var vb = b.spatiotemporalVolume.Value;
                if (va.center != vb.center || va.size != vb.size || !Mathf.Approximately(va.tMin, vb.tMin) || !Mathf.Approximately(va.tMax, vb.tMax))
                    list.Add(new CalendarEventFieldDiff { fieldName = "spatiotemporalVolume", valueBefore = $"{va.center},{va.size},{va.tMin},{va.tMax}", valueAfter = $"{vb.center},{vb.size},{vb.tMin},{vb.tMax}" });
            }
            if (!NarrativeCalendarEventSnapshot.ListEquals(a.tags, b.tags))
                list.Add(new CalendarEventFieldDiff { fieldName = "tags", valueBefore = a.tags != null ? string.Join(",", a.tags) : "", valueAfter = b.tags != null ? string.Join(",", b.tags) : "" });
            if (!NarrativeCalendarEventSnapshot.ListEquals(a.positionKeys, b.positionKeys))
                list.Add(new CalendarEventFieldDiff { fieldName = "positionKeys", valueBefore = a.positionKeys != null ? string.Join(",", a.positionKeys) : "", valueAfter = b.positionKeys != null ? string.Join(",", b.positionKeys) : "" });
            int ac = a.actions != null ? a.actions.Count : 0;
            int bc = b.actions != null ? b.actions.Count : 0;
            if (ac != bc)
                list.Add(new CalendarEventFieldDiff { fieldName = "actions", valueBefore = $"{ac} items", valueAfter = $"{bc} items" });

            return list;
        }
    }

    public class CalendarEventEditorWindow : EditorWindow
    {
        private NarrativeCalendarAsset _calendar;
        private int _eventIndex;
        private string _eventId;
        private NarrativeCalendarEventSnapshot _workingCopy;
        private NarrativeCalendarEventSnapshot _beforeEdit;
        private readonly List<NarrativeCalendarEventSnapshot> _lastSavedList = new List<NarrativeCalendarEventSnapshot>();
        private bool _saveProjectWhenCardSaved;
        private Vector2 _scroll;
        private SerializedObject _workingSO;

        private enum SyncState { Blue, Yellow, Green, Red }
        private const float IconSize = 18f;

        public static void ShowWindow(NarrativeCalendarAsset calendar, int eventIndex)
        {
            var w = GetWindow<CalendarEventEditorWindow>("Calendar Event Editor");
            w.minSize = new Vector2(420, 520);
            w._calendar = calendar;
            w._eventIndex = eventIndex;
            w._eventId = calendar != null && calendar.events != null && eventIndex >= 0 && eventIndex < calendar.events.Count
                ? calendar.events[eventIndex].id
                : null;
            var sceneParent = calendar != null ? calendar.transform : null;
            w._workingCopy = new NarrativeCalendarEventSnapshot(sceneParent);
            w._beforeEdit = new NarrativeCalendarEventSnapshot(sceneParent);
            w._lastSavedList.Clear();
            w._saveProjectWhenCardSaved = NarrativeCalendarEventSnapshot.GetSaveProjectWhenCardSavedPref();
            if (calendar != null && eventIndex >= 0 && eventIndex < calendar.events.Count)
            {
                w._workingCopy.CopyFrom(calendar, eventIndex);
                w._beforeEdit.CopyFrom(calendar, eventIndex);
            }
            w._workingSO = w._workingCopy.GetSerializedObject();
            w.Show();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            if (_calendar == null || _eventIndex < 0) return;
            ResolveEventIndex();
            if (_eventIndex < 0) return;
            _workingCopy.CopyFrom(_calendar, _eventIndex);
            _workingSO?.Update();
            Repaint();
        }

        private void ResolveEventIndex()
        {
            if (_calendar?.events == null || string.IsNullOrEmpty(_eventId)) return;
            for (int i = 0; i < _calendar.events.Count; i++)
            {
                if (_calendar.events[i] != null && _calendar.events[i].id == _eventId)
                {
                    _eventIndex = i;
                    return;
                }
            }
            _eventIndex = -1;
        }

        private bool IsEventMissing()
        {
            ResolveEventIndex();
            return _calendar == null || _calendar.events == null || _eventIndex < 0 || _eventIndex >= _calendar.events.Count;
        }

        private SyncState GetSyncState()
        {
            if (_calendar == null || _workingCopy == null) return SyncState.Blue;
            ResolveEventIndex();
            if (_eventIndex < 0) return SyncState.Blue;
            var currentFromCalendar = new NarrativeCalendarEventSnapshot();
            currentFromCalendar.CopyFrom(_calendar, _eventIndex);
            bool workingEqualsCalendar = _workingCopy.ContentEquals(currentFromCalendar);
            if (!workingEqualsCalendar)
                return SyncState.Yellow;
            if (_lastSavedList.Count == 0)
                return SyncState.Blue;
            if (_lastSavedList.Count > 0 && _workingCopy.ContentEquals(_lastSavedList[_lastSavedList.Count - 1]))
                return SyncState.Blue;
            for (int i = 0; i < _lastSavedList.Count - 1; i++)
            {
                if (_workingCopy.ContentEquals(_lastSavedList[i]))
                    return SyncState.Green;
            }
            return SyncState.Red;
        }

        private void DoSave()
        {
            if (IsEventMissing()) return;
            Undo.RecordObject(_calendar, "Edit Calendar Event");
            _workingCopy.ApplyTo(_calendar, _eventIndex);
            var savedSnapshot = new NarrativeCalendarEventSnapshot();
            savedSnapshot.CopyFrom(_calendar, _eventIndex);
            _lastSavedList.Add(savedSnapshot);
            EditorUtility.SetDirty(_calendar);
            if (_saveProjectWhenCardSaved)
                AssetDatabase.SaveAssets();
            _workingSO?.Update();
            Repaint();
        }

        private void DoRevertFromBeforeEdit()
        {
            if (_beforeEdit == null || _workingCopy == null) return;
            _workingCopy.CopyFrom(_beforeEdit);
            _workingSO?.Update();
            Repaint();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Calendar Event Editor", EditorStyles.boldLabel);

            if (IsEventMissing())
            {
                EditorGUILayout.HelpBox("Event no longer exists or calendar is missing.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            _saveProjectWhenCardSaved = EditorGUILayout.Toggle("Save project when card is saved", _saveProjectWhenCardSaved);
            NarrativeCalendarEventSnapshot.SetSaveProjectWhenCardSavedPref(_saveProjectWhenCardSaved);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Width(60)))
                DoSave();
            if (GUILayout.Button("Revert", GUILayout.Width(60)))
                DoRevertFromBeforeEdit();
            var state = GetSyncState();
            DrawStatusIcon(state);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Editable event", EditorStyles.boldLabel);
            DrawEditableCard();
            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Before edit (blue)", EditorStyles.boldLabel);
            DrawReadOnlyCard(_beforeEdit, new Color(0.4f, 0.6f, 1f, 0.35f));
            EditorGUILayout.Space(4);

            var diffs = CalendarEventDiffHelper.GetDiff(_beforeEdit, _workingCopy);
            EditorGUILayout.LabelField("Changes (red)", EditorStyles.boldLabel);
            DrawDiffCard(diffs);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("After save (yellow)", EditorStyles.boldLabel);
            if (_lastSavedList.Count > 0)
                DrawReadOnlyCard(_lastSavedList[_lastSavedList.Count - 1], new Color(1f, 0.95f, 0.5f, 0.4f));
            else
                DrawReadOnlyCard(null, new Color(1f, 0.95f, 0.5f, 0.2f));

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusIcon(SyncState state)
        {
            Color c = state switch
            {
                SyncState.Blue => new Color(0.2f, 0.5f, 1f),
                SyncState.Yellow => new Color(0.95f, 0.85f, 0.2f),
                SyncState.Green => new Color(0.2f, 0.75f, 0.3f),
                SyncState.Red => new Color(0.9f, 0.25f, 0.2f),
                _ => Color.gray
            };
            string tooltip = state switch
            {
                SyncState.Blue => "In sync with last save.",
                SyncState.Yellow => "Unsaved edits.",
                SyncState.Green => "Undo used; still above save.",
                SyncState.Red => "Undone behind last save.",
                _ => ""
            };
            var rect = GUILayoutUtility.GetRect(IconSize, IconSize);
            EditorGUI.DrawRect(rect, c);
            EditorGUI.LabelField(rect, new GUIContent("", tooltip));
        }

        private void DrawEditableCard()
        {
            if (_workingSO == null) return;
            _workingSO.Update();
            var evtProp = _workingSO.FindProperty("events").GetArrayElementAtIndex(0);
            if (evtProp == null) return;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginDisabledGroup(false);
            DrawPropertyFields(evtProp, true);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            _workingSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private void DrawReadOnlyCard(NarrativeCalendarEventSnapshot snapshot, Color bgColor)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (snapshot?.Event != null)
            {
                var evt = snapshot.Event;
                EditorGUILayout.LabelField("Id", evt.id ?? "");
                EditorGUILayout.LabelField("Title", evt.title);
                EditorGUILayout.LabelField("Notes", evt.notes ?? "");
                EditorGUILayout.LabelField("Start", evt.startDateTime.ToString());
                EditorGUILayout.LabelField("Duration (s)", evt.durationSeconds.ToString());
                EditorGUILayout.LabelField("Tree", evt.tree != null ? evt.tree.name : "null");
                EditorGUILayout.LabelField("Tags", evt.tags != null ? string.Join(", ", evt.tags) : "");
                EditorGUILayout.LabelField("Actions", evt.actions != null ? $"{evt.actions.Count} items" : "0");
            }
            else
                EditorGUILayout.LabelField("No save yet");
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = prev;
        }

        private void DrawDiffCard(List<CalendarEventFieldDiff> diffs)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.55f, 0.35f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (diffs != null && diffs.Count > 0)
            {
                foreach (var d in diffs)
                    EditorGUILayout.LabelField(d.fieldName, $"{d.valueBefore} → {d.valueAfter}");
            }
            else
                EditorGUILayout.LabelField("No changes");
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = prev;
        }

        private void DrawPropertyFields(SerializedProperty evtProp, bool editable)
        {
            if (evtProp == null) return;
            GUI.enabled = editable;
            EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("title"));
            EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("notes"));
            EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("startDateTime"));
            EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("durationSeconds"));
            EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("tree"));
            EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("tags"), true);
            EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("actions"), true);
            var volProp = evtProp.FindPropertyRelative("spatiotemporalVolume");
            if (volProp != null)
                EditorGUILayout.PropertyField(volProp);
            EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("positionKeys"), true);
            GUI.enabled = true;
        }
    }
}
#endif
