using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Optional in-game UI: prompt input, "Interpret" and "Summarize" buttons, and display of summary / interpreted events.
    /// Uses OnGUI for a minimal panel; assign Summarizer and PromptInterpreter on the same or child GameObjects.
    /// </summary>
    public class NarrativeLSTMUI : MonoBehaviour
    {
        [Header("References")]
        public NarrativeLSTMSummarizer summarizer;
        public NarrativeLSTMPromptInterpreter promptInterpreter;

        [Header("UI")]
        [Tooltip("Show panel at runtime.")]
        public bool showPanel = true;
        public int panelWidth = 360;
        public int panelHeight = 280;

        private string _promptInput = "Add event meeting at 9am";
        private string _summaryText = "";
        private string _interpretResult = "";
        private Vector2 _scroll;

        private void Awake()
        {
            if (summarizer == null) summarizer = GetComponent<NarrativeLSTMSummarizer>();
            if (promptInterpreter == null) promptInterpreter = GetComponent<NarrativeLSTMPromptInterpreter>();
        }

        private void OnGUI()
        {
            if (!showPanel) return;
            GUILayout.BeginArea(new Rect(Screen.width - panelWidth - 10, 10, panelWidth, panelHeight));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Narrative LSTM", GUI.skin.box);
            if (summarizer != null)
            {
                GUILayout.Label("What's going on:", GUILayout.Width(120));
                _summaryText = GUILayout.TextArea(_summaryText, GUILayout.Height(40));
                if (GUILayout.Button("Summarize", GUILayout.Height(24)))
                    _summaryText = summarizer.Summarize();
            }
            if (promptInterpreter != null)
            {
                GUILayout.Label("Prompt:", GUILayout.Width(60));
                _promptInput = GUILayout.TextField(_promptInput, GUILayout.Height(22));
                if (GUILayout.Button("Interpret", GUILayout.Height(24)))
                {
                    promptInterpreter.Interpret(_promptInput);
                    _interpretResult = "";
                    foreach (var ev in promptInterpreter.lastInterpretedEvents)
                        _interpretResult += $"{ev.title} @ {ev.startSeconds:F0}s\n";
                    if (string.IsNullOrEmpty(_interpretResult)) _interpretResult = "(no events)";
                }
                GUILayout.Label("Interpreted:", GUILayout.Width(80));
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(60));
                GUILayout.Label(_interpretResult);
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
