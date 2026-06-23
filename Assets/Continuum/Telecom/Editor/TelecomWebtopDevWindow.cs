#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Continuum.Telecom.Editor
{
    public class TelecomWebtopDevWindow : EditorWindow
    {
        [MenuItem("Window/Continuum/Telecom Webtop Dev")]
        static void Open() => GetWindow<TelecomWebtopDevWindow>("Telecom Webtop");

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Open apps/telecom-webtop (npm run dev) then load http://127.0.0.1:5175 in ContinuumWebViewHost spike window.",
                MessageType.Info);
            if (GUILayout.Button("Open WebView Spike"))
                ContinuumWebViewSpikeWindow.Open();
        }
    }
}
#endif
