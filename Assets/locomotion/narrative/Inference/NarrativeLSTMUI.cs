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
        public int panelWidth = 440;
        public int panelHeight = 360;
        [Tooltip("Font size for labels, buttons, and text fields.")]
        [SerializeField] private int uiFontSize = 17;

        private string _promptInput = "Add event meeting at 9am";
        private string _summaryText = "";
        private string _interpretResult = "";
        private Vector2 _scroll;

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _textAreaStyle;
        private bool _guiStylesReady;

        private void Awake()
        {
            if (summarizer == null) summarizer = GetComponent<NarrativeLSTMSummarizer>();
            if (promptInterpreter == null) promptInterpreter = GetComponent<NarrativeLSTMPromptInterpreter>();
        }

        private void EnsureGuiStyles()
        {
            if (_guiStylesReady) return;
            _guiStylesReady = true;
            int fs = Mathf.Max(10, uiFontSize);
            int row = Mathf.RoundToInt(fs + 14);
            _titleStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = fs + 1,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = row + 4
            };
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = fs, wordWrap = true };
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = fs, fixedHeight = row };
            _textFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = fs };
            _textAreaStyle = new GUIStyle(GUI.skin.textArea) { fontSize = fs, wordWrap = true };
        }

        private void OnGUI()
        {
            if (!showPanel) return;
            EnsureGuiStyles();
            int textAreaH = Mathf.RoundToInt(uiFontSize * 2.5f);
            int scrollH = Mathf.RoundToInt(uiFontSize * 4f);
            GUILayout.BeginArea(new Rect(Screen.width - panelWidth - 10, 10, panelWidth, panelHeight));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Narrative LSTM", _titleStyle);
            if (summarizer != null)
            {
                GUILayout.Label("What's going on:", _labelStyle);
                _summaryText = GUILayout.TextArea(_summaryText, _textAreaStyle, GUILayout.Height(textAreaH));
                if (GUILayout.Button("Summarize", _buttonStyle))
                    _summaryText = summarizer.Summarize();
            }
            if (promptInterpreter != null)
            {
                GUILayout.Label("Prompt:", _labelStyle);
                _promptInput = GUILayout.TextField(_promptInput, _textFieldStyle, GUILayout.Height(_buttonStyle.fixedHeight));
                if (GUILayout.Button("Interpret", _buttonStyle))
                {
                    promptInterpreter.Interpret(_promptInput);
                    _interpretResult = "";
                    foreach (var ev in promptInterpreter.lastInterpretedEvents)
                        _interpretResult += $"{ev.title} @ {ev.startSeconds:F0}s\n";
                    if (string.IsNullOrEmpty(_interpretResult)) _interpretResult = "(no events)";
                }
                GUILayout.Label("Interpreted:", _labelStyle);
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(scrollH));
                GUILayout.Label(_interpretResult, _labelStyle);
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
