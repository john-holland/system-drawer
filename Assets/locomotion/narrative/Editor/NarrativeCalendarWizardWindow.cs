#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Locomotion.Narrative.EditorTools
{
    public class NarrativeCalendarWizardWindow : EditorWindow
    {
        private NarrativeCalendarAsset calendar;
        private SerializedObject calendarSO;

        private int viewYear = 2025;
        private int viewMonth = 2;
        private int selectedDay = 1;

        private VisualElement gridRoot;
        private ScrollView stickyList;
        private Label headerLabel;

        [MenuItem("Window/Locomotion/Narrative/Calendar Wizard")]
        public static void ShowWindow()
        {
            var w = GetWindow<NarrativeCalendarWizardWindow>("Narrative Calendar");
            w.minSize = new Vector2(980, 620);
            w.Show();
        }

        private void OnEnable()
        {
            if (calendar == null)
                calendar = Selection.activeObject as NarrativeCalendarAsset;

            RebindCalendar();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 6;
            root.style.paddingBottom = 6;

            // Top bar
            var top = new VisualElement();
            top.style.flexDirection = FlexDirection.Row;
            top.style.alignItems = Align.Center;

            // Use IMGUIContainer for ObjectField to ensure scene objects work properly
            var calField = new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                var newCalendar = EditorGUILayout.ObjectField("Calendar", calendar, typeof(NarrativeCalendarAsset), true) as NarrativeCalendarAsset;
                if (EditorGUI.EndChangeCheck())
                {
                    calendar = newCalendar;
                    RebindCalendar();
                    RefreshAll();
                }
            });
            calField.style.flexGrow = 1f;
            calField.style.marginRight = 8;

            var monthYearText = new TextField("Month Year");
            monthYearText.value = $"{viewMonth} {viewYear}";
            monthYearText.style.width = 180;
            monthYearText.style.marginRight = 8;
            monthYearText.RegisterValueChangedCallback(evt =>
            {
                if (TryParseMonthYear(evt.newValue, out int m, out int y))
                {
                    viewMonth = Mathf.Clamp(m, 1, 12);
                    viewYear = Mathf.Clamp(y, 1, 9999);
                    selectedDay = Mathf.Clamp(selectedDay, 1, DateTime.DaysInMonth(viewYear, viewMonth));
                    RefreshAll();
                }
            });

            var jumpBtn = new Button(() =>
            {
                selectedDay = Mathf.Clamp(selectedDay, 1, DateTime.DaysInMonth(viewYear, viewMonth));
                RefreshAll();
            })
            { text = "Jump" };
            jumpBtn.style.marginRight = 8;

            var addBtn = new Button(() => CreateEventOnSelectedDate())
            { text = "Add Event" };
            addBtn.style.marginRight = 8;

            headerLabel = new Label();
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.marginLeft = 8;

            top.Add(calField);
            top.Add(monthYearText);
            top.Add(jumpBtn);
            top.Add(addBtn);
            top.Add(headerLabel);
            root.Add(top);

            // Main split
            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;
            body.style.marginTop = 8;

            // Left: calendar grid
            var left = new VisualElement();
            left.style.flexGrow = 1.6f;
            left.style.flexDirection = FlexDirection.Column;
            left.style.marginRight = 10;

            // weekday header
            var weekdays = new VisualElement();
            weekdays.style.flexDirection = FlexDirection.Row;
            weekdays.style.justifyContent = Justify.SpaceBetween;
            weekdays.style.height = 22;
            string[] wd = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            for (int i = 0; i < 7; i++)
            {
                var l = new Label(wd[i]);
                l.style.unityFontStyleAndWeight = FontStyle.Bold;
                l.style.flexGrow = 1f;
                l.style.unityTextAlign = TextAnchor.MiddleCenter;
                weekdays.Add(l);
            }

            gridRoot = new VisualElement();
            gridRoot.style.flexGrow = 1f;
            gridRoot.style.flexDirection = FlexDirection.Column;
            gridRoot.style.borderTopWidth = 1;
            gridRoot.style.borderLeftWidth = 1;
            gridRoot.style.borderRightWidth = 1;
            gridRoot.style.borderBottomWidth = 1;
            gridRoot.style.borderTopColor = new Color(0, 0, 0, 0.25f);
            gridRoot.style.borderLeftColor = new Color(0, 0, 0, 0.25f);
            gridRoot.style.borderRightColor = new Color(0, 0, 0, 0.25f);
            gridRoot.style.borderBottomColor = new Color(0, 0, 0, 0.25f);

            left.Add(weekdays);
            left.Add(gridRoot);

            // Right: sticky notes
            var right = new VisualElement();
            right.style.flexGrow = 1f;
            right.style.flexDirection = FlexDirection.Column;

            var rightTitle = new Label("Events");
            rightTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            rightTitle.style.marginBottom = 4;
            right.Add(rightTitle);

            stickyList = new ScrollView();
            stickyList.style.flexGrow = 1f;
            right.Add(stickyList);

            body.Add(left);
            body.Add(right);
            root.Add(body);

            RefreshAll();
        }

        private void RebindCalendar()
        {
            calendarSO = calendar != null ? new SerializedObject(calendar) : null;
        }

        private void RefreshAll()
        {
            headerLabel.text = $"{new DateTime(viewYear, viewMonth, 1):MMMM yyyy}  (selected {viewMonth}/{selectedDay}/{viewYear})";
            RebuildGrid();
            RebuildStickyList();
        }

        private void RebuildGrid()
        {
            gridRoot.Clear();

            int daysInMonth = DateTime.DaysInMonth(viewYear, viewMonth);
            int firstDow = (int)new DateTime(viewYear, viewMonth, 1).DayOfWeek; // 0=Sun

            // 6 rows x 7 columns
            int cellCount = 42;
            for (int r = 0; r < 6; r++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.flexGrow = 1f;

                for (int c = 0; c < 7; c++)
                {
                    int idx = r * 7 + c;
                    int dayNum = idx - firstDow + 1;

                    var cell = new VisualElement();
                    cell.style.flexGrow = 1f;
                    cell.style.borderRightWidth = 1;
                    cell.style.borderBottomWidth = 1;
                    cell.style.borderRightColor = new Color(0, 0, 0, 0.2f);
                    cell.style.borderBottomColor = new Color(0, 0, 0, 0.2f);
                    cell.style.paddingLeft = 6;
                    cell.style.paddingTop = 4;
                    cell.style.paddingRight = 4;
                    cell.style.paddingBottom = 4;
                    cell.style.minHeight = 80;

                    if (dayNum >= 1 && dayNum <= daysInMonth)
                    {
                        bool isSelected = dayNum == selectedDay;
                        if (isSelected)
                        {
                            cell.style.backgroundColor = new Color(0.25f, 0.5f, 1f, 0.14f);
                        }

                        var dayLabel = new Label(dayNum.ToString());
                        dayLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                        dayLabel.style.fontSize = 12;
                        cell.Add(dayLabel);

                        // small list of events
                        foreach (var title in GetEventTitlesForDay(dayNum))
                        {
                            var ev = new Label(title);
                            ev.style.fontSize = 10;
                            ev.style.color = new Color(0, 0, 0, 0.8f);
                            ev.style.unityTextAlign = TextAnchor.UpperLeft;
                            ev.style.marginTop = 2;
                            cell.Add(ev);
                        }

                        int capturedDay = dayNum;
                        cell.RegisterCallback<MouseDownEvent>(_ =>
                        {
                            selectedDay = capturedDay;
                            RefreshAll();
                        });
                    }
                    else
                    {
                        cell.style.backgroundColor = new Color(0, 0, 0, 0.03f);
                    }

                    row.Add(cell);
                }

                gridRoot.Add(row);
            }
        }

        private void RebuildStickyList()
        {
            stickyList.Clear();

            if (calendar == null || calendarSO == null)
            {
                stickyList.Add(new Label("Assign a NarrativeCalendarAsset to view events."));
                return;
            }

            var eventsProp = calendarSO.FindProperty("events");
            if (eventsProp == null || !eventsProp.isArray)
                return;

            var dayEvents = GetEventIndicesForSelectedDay(eventsProp);
            if (dayEvents.Count == 0)
            {
                stickyList.Add(new Label("No events on this date."));
                return;
            }

            foreach (int idx in dayEvents)
            {
                var evtProp = eventsProp.GetArrayElementAtIndex(idx);
                if (evtProp == null) continue;

                var card = new VisualElement();
                card.style.marginBottom = 8;
                card.style.paddingLeft = 8;
                card.style.paddingRight = 8;
                card.style.paddingTop = 6;
                card.style.paddingBottom = 6;
                card.style.borderTopWidth = 1;
                card.style.borderLeftWidth = 1;
                card.style.borderRightWidth = 1;
                card.style.borderBottomWidth = 1;
                card.style.borderTopColor = new Color(0, 0, 0, 0.25f);
                card.style.borderLeftColor = new Color(0, 0, 0, 0.25f);
                card.style.borderRightColor = new Color(0, 0, 0, 0.25f);
                card.style.borderBottomColor = new Color(0, 0, 0, 0.25f);
                card.style.backgroundColor = new Color(1f, 0.95f, 0.7f, 0.55f); // sticky-note-ish

                var titleProp = evtProp.FindPropertyRelative("title");
                var treeProp = evtProp.FindPropertyRelative("tree");
                string title = titleProp != null ? titleProp.stringValue : "(event)";

                var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var tLabel = new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1f } };
                var openBtn = new Button(() =>
                {
                    calendarSO.ApplyModifiedProperties();
                    if (treeProp != null && treeProp.objectReferenceValue is NarrativeTreeAsset tree)
                        NarrativeTreeEditorWindow.ShowWindow(tree);
                })
                { text = "Open" };
                header.Add(tLabel);
                header.Add(openBtn);
                card.Add(header);

                // Properties (using PropertyField so it works with nested structs)
                card.Add(new UnityEditor.UIElements.PropertyField(evtProp.FindPropertyRelative("startDateTime"), "Start"));
                card.Add(new UnityEditor.UIElements.PropertyField(evtProp.FindPropertyRelative("durationSeconds"), "Duration (s)"));
                card.Add(new UnityEditor.UIElements.PropertyField(treeProp, "Tree"));
                card.Add(new UnityEditor.UIElements.PropertyField(evtProp.FindPropertyRelative("actions"), "Actions"));

                // Commit changes on UI change
                card.RegisterCallback<ChangeEvent<string>>(_ =>
                {
                    calendarSO.ApplyModifiedProperties();
                    EditorUtility.SetDirty(calendar);
                    RefreshAll();
                });

                stickyList.Add(card);
            }

            // Bind property fields
            // Older Unity versions may not support per-element binding consistently; binding at root is safest.
            rootVisualElement.Bind(calendarSO);
        }

        private List<int> GetEventIndicesForSelectedDay(SerializedProperty eventsProp)
        {
            var indices = new List<int>();
            for (int i = 0; i < eventsProp.arraySize; i++)
            {
                var evt = eventsProp.GetArrayElementAtIndex(i);
                var start = evt.FindPropertyRelative("startDateTime");
                if (start == null) continue;

                int y = start.FindPropertyRelative("year").intValue;
                int m = start.FindPropertyRelative("month").intValue;
                int d = start.FindPropertyRelative("day").intValue;

                if (y == viewYear && m == viewMonth && d == selectedDay)
                    indices.Add(i);
            }
            return indices;
        }

        private IEnumerable<string> GetEventTitlesForDay(int dayNum)
        {
            if (calendar == null || calendar.events == null)
                yield break;

            int count = 0;
            for (int i = 0; i < calendar.events.Count; i++)
            {
                var e = calendar.events[i];
                if (e == null) continue;
                if (e.startDateTime.year != viewYear || e.startDateTime.month != viewMonth || e.startDateTime.day != dayNum)
                    continue;

                yield return e.title;
                count++;
                if (count >= 3) yield break; // keep cells readable
            }
        }

        private void CreateEventOnSelectedDate()
        {
            if (calendar == null)
                return;

            Undo.RecordObject(calendar, "Add Narrative Event");
            calendar.events.Add(new NarrativeCalendarEvent
            {
                title = "New Event",
                startDateTime = new NarrativeDateTime(viewYear, viewMonth, selectedDay, 9, 0, 0)
            });
            EditorUtility.SetDirty(calendar);
            AssetDatabase.SaveAssets();

            RebindCalendar();
            RefreshAll();
        }

        private static bool TryParseMonthYear(string text, out int month, out int year)
        {
            month = 1;
            year = 2000;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var parts = text.Trim().Split(new[] { ' ', '/', '-', '.', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return false;

            return int.TryParse(parts[0], out month) && int.TryParse(parts[1], out year);
        }
    }
}
#endif

