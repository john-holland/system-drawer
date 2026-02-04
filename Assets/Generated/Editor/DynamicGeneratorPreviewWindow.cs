#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor window for dynamic generators: isolated preview (3D/audio/texture) and history list
/// with Revert, Export, Delete, Delete all. Does not modify main scene hierarchy.
/// </summary>
public class DynamicGeneratorPreviewWindow : EditorWindow
{
    [SerializeField] private DynamicGeneratorBase selectedGenerator;

    public void SetSelectedGenerator(DynamicGeneratorBase gen) { selectedGenerator = gen; serializedGenerator = gen != null ? new SerializedObject(gen) : null; selectedHistoryIndex = -1; }
    private SerializedObject serializedGenerator;
    private Vector2 historyScroll;
    private Vector2 previewScroll;
    private int selectedHistoryIndex = -1;
    private Scene previewScene;
    private Camera previewCamera;
    private GameObject previewContainer;
    private RenderTexture previewRenderTexture;
    private AudioSource previewAudioSource;
    private const int PreviewSize = 256;
    private const string PreviewSceneName = "DynamicGeneratorPreview_Scene";
    private bool previewSceneDirty;

    [MenuItem("Window/Generated/Dynamic Generator Preview")]
    public static DynamicGeneratorPreviewWindow ShowWindow()
    {
        var w = GetWindow<DynamicGeneratorPreviewWindow>(false, "Generator Preview", true);
        w.minSize = new Vector2(400, 400);
        return w;
    }

    private void OnEnable()
    {
        EnsurePreviewScene();
    }

    private void OnDisable()
    {
        CleanupPreviewScene();
    }

    private void OnDestroy()
    {
        CleanupPreviewScene();
    }

    private void EnsurePreviewScene()
    {
        if (previewScene.IsValid() && previewScene.isLoaded) return;
        previewScene = SceneManager.CreateScene(PreviewSceneName);
        var camGo = new GameObject("PreviewCamera");
        camGo.AddComponent<Camera>();
        previewCamera = camGo.GetComponent<Camera>();
        previewCamera.orthographic = false;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        previewCamera.transform.position = new Vector3(0, 0, -5f);
        previewCamera.transform.LookAt(Vector3.zero);
        SceneManager.MoveGameObjectToScene(camGo, previewScene);
        var container = new GameObject("PreviewContainer");
        SceneManager.MoveGameObjectToScene(container, previewScene);
        previewContainer = container;
        var audioGo = new GameObject("PreviewAudio");
        previewAudioSource = audioGo.AddComponent<AudioSource>();
        SceneManager.MoveGameObjectToScene(audioGo, previewScene);
        if (previewRenderTexture == null || !previewRenderTexture.IsCreated())
        {
            previewRenderTexture = new RenderTexture(PreviewSize, PreviewSize, 24);
            previewRenderTexture.Create();
        }
        previewCamera.targetTexture = previewRenderTexture;
        previewCamera.enabled = true;
        previewSceneDirty = true;
    }

    private void CleanupPreviewScene()
    {
        if (previewRenderTexture != null && previewRenderTexture.IsCreated())
            previewRenderTexture.Release();
        previewRenderTexture = null;
        if (previewScene.IsValid() && previewScene.isLoaded)
            SceneManager.UnloadSceneAsync(previewScene);
        previewScene = default;
        previewCamera = null;
        previewContainer = null;
        previewAudioSource = null;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Dynamic Generator Preview", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        var newGen = (DynamicGeneratorBase)EditorGUILayout.ObjectField("Generator", selectedGenerator, typeof(DynamicGeneratorBase), false);
        if (newGen != selectedGenerator)
        {
            selectedGenerator = newGen;
            serializedGenerator = selectedGenerator != null ? new SerializedObject(selectedGenerator) : null;
            selectedHistoryIndex = -1;
        }

        if (selectedGenerator == null)
        {
            EditorGUILayout.HelpBox("Assign a generator asset (e.g. Dynamic Texture, 3D, Audio).", MessageType.Info);
            return;
        }

        serializedGenerator?.Update();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Preview", EditorStyles.miniBoldLabel);
        DrawPreviewArea();
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("History", EditorStyles.miniBoldLabel);
        DrawHistoryList();
        EditorGUILayout.Space(4);
        DrawActions();

        serializedGenerator?.ApplyModifiedPropertiesWithoutUndo();
    }

    private void DrawPreviewArea()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(PreviewSize + 16));
        previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.Height(PreviewSize + 8));
        if (previewRenderTexture != null && previewRenderTexture.IsCreated())
        {
            if (Event.current.type == EventType.Repaint)
            {
                EnsurePreviewScene();
                if (previewCamera != null) previewCamera.Render();
            }
            var rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize);
            EditorGUI.DrawPreviewTexture(rect, previewRenderTexture, null, ScaleMode.ScaleToFit);
        }
        else
        {
            GUILayout.Box("3D preview", GUILayout.Height(PreviewSize), GUILayout.Width(PreviewSize));
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        if (selectedGenerator is DynamicTextureUIGenerator texGen && selectedHistoryIndex >= 0 && texGen.history != null && selectedHistoryIndex < texGen.history.Count)
        {
            var entry = texGen.history[selectedHistoryIndex];
            var tex = entry.generatedAsset as Texture2D;
            if (tex == null && !string.IsNullOrEmpty(entry.generatedAssetPath))
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.generatedAssetPath);
            if (tex != null)
            {
                EditorGUILayout.Space(2);
                var r = GUILayoutUtility.GetRect(128, 128);
                EditorGUI.DrawPreviewTexture(r, tex, null, ScaleMode.ScaleToFit);
            }
        }
    }

    private void DrawHistoryList()
    {
        if (selectedGenerator.history == null || selectedGenerator.history.Count == 0)
        {
            EditorGUILayout.HelpBox("No history. Generate something from this generator.", MessageType.None);
            return;
        }
        historyScroll = EditorGUILayout.BeginScrollView(historyScroll, GUILayout.Height(120));
        for (int i = 0; i < selectedGenerator.history.Count; i++)
        {
            var entry = selectedGenerator.history[i];
            if (entry == null) continue;
            EditorGUILayout.BeginHorizontal();
            bool selected = selectedHistoryIndex == i;
            if (GUILayout.Toggle(selected, GUIContent.none, GUILayout.Width(18))) selectedHistoryIndex = i;
            if (entry.thumbnail != null && entry.thumbnail.Length > 0)
            {
                var thumb = new Texture2D(2, 2);
                if (thumb.LoadImage(entry.thumbnail))
                    GUILayout.Box(thumb, GUILayout.Width(32), GUILayout.Height(32));
            }
            string label = string.IsNullOrEmpty(entry.prompt) ? "(no prompt)" : (entry.prompt.Length > 40 ? entry.prompt.Substring(0, 40) + "…" : entry.prompt);
            GUILayout.Label($"{i}: {label} — {entry.TimestampString}", EditorStyles.miniLabel);
            if (GUILayout.Button("Revert", GUILayout.Width(50))) RevertToEntry(i);
            if (GUILayout.Button("Export…", GUILayout.Width(55))) ExportEntry(i);
            if (GUILayout.Button("Delete", GUILayout.Width(45))) DeleteEntry(i);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawActions()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Delete all", GUILayout.Height(22)))
        {
            if (EditorUtility.DisplayDialog("Delete all", "Remove all history entries and delete their assets?", "Delete all", "Cancel"))
            {
                selectedGenerator.ClearHistory(true);
                selectedHistoryIndex = -1;
                EditorUtility.SetDirty(selectedGenerator);
            }
        }
        if (GUILayout.Button("Delete all for this prompt", GUILayout.Height(22)))
        {
            DeleteAllForPrompt();
        }
        if (GUILayout.Button("Search for updates", GUILayout.Height(22)))
        {
            LmStudioModelService.SearchForUpdates(selectedGenerator, this);
        }
        EditorGUILayout.EndHorizontal();

        if (selectedGenerator is DynamicShaderGenerator shaderGen)
        {
            EditorGUILayout.Space(2);
            var currentResult = shaderGen.GetCurrentResult();
            if (currentResult != null && currentResult.assetDependencyKeys != null && currentResult.assetDependencyKeys.Count > 0)
            {
                EditorGUILayout.LabelField("Dependencies", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var key in currentResult.assetDependencyKeys)
                    EditorGUILayout.LabelField("  " + key, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("Generate textures for dependencies", GUILayout.Height(22)))
                    DynamicGeneratorEditor.GenerateTexturesForDependencies(shaderGen);
            }
            if (GUILayout.Button("Open dependency graph", GUILayout.Height(22)))
            {
                var graph = AssetDatabase.LoadAssetAtPath<AssetDependencyGraph>("Assets/Generated/AssetDependencyGraph.asset");
                if (graph != null) Selection.activeObject = graph;
                else EditorUtility.DisplayDialog("Dependency graph", "AssetDependencyGraph.asset not found. Generate a shader first to create it.", "OK");
            }
            if (GUILayout.Button("Open in Shader Editor (properties pane style)", GUILayout.Height(22)))
            {
                if (shaderGen.GetCurrentResult()?.generatedAsset is Shader shader)
                    AssetDatabase.OpenAsset(shader);
                else
                    EditorUtility.DisplayDialog("Shader Preview", "No shader result selected. Generate or revert to a shader first.", "OK");
            }
        }
    }

    private void RevertToEntry(int index)
    {
        if (selectedGenerator == null || selectedGenerator.history == null || index < 0 || index >= selectedGenerator.history.Count) return;
        selectedGenerator.currentResultIndex = index;
        selectedHistoryIndex = index;
        EditorUtility.SetDirty(selectedGenerator);
    }

    private void ExportEntry(int index)
    {
        if (selectedGenerator == null || selectedGenerator.history == null || index < 0 || index >= selectedGenerator.history.Count) return;
        var entry = selectedGenerator.history[index];
        var obj = entry.generatedAsset;
        if (obj == null && !string.IsNullOrEmpty(entry.generatedAssetPath))
            obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.generatedAssetPath);
        if (obj is Texture2D tex)
        {
            string path = EditorUtility.SaveFilePanel("Export texture", "", "export.png", "png");
            if (!string.IsNullOrEmpty(path)) System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        }
        else if (obj is AudioClip clip)
        {
            string path = EditorUtility.SaveFilePanel("Export audio", "", "export.wav", "wav");
            if (!string.IsNullOrEmpty(path)) ExportWav(clip, path);
        }
        else
        {
            EditorUtility.DisplayDialog("Export", "Export for this type not implemented yet. Use Project window to copy asset.", "OK");
        }
    }

    private static void ExportWav(AudioClip clip, string path)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create))
        using (var bw = new System.IO.BinaryWriter(fs))
        {
            int sampleRate = clip.frequency;
            int channels = clip.channels;
            int subchunk1Size = 16;
            int bitsPerSample = 16;
            int blockAlign = (int)(channels * (bitsPerSample / 8f));
            int byteRate = sampleRate * blockAlign;
            int dataSize = samples.Length * 2;
            int chunkSize = 4 + (8 + subchunk1Size) + (8 + dataSize);
            bw.Write(new char[] { 'R', 'I', 'F', 'F' });
            bw.Write(chunkSize);
            bw.Write(new char[] { 'W', 'A', 'V', 'E' });
            bw.Write(new char[] { 'f', 'm', 't', ' ' });
            bw.Write(subchunk1Size);
            bw.Write((short)1);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)blockAlign);
            bw.Write((short)bitsPerSample);
            bw.Write(new char[] { 'd', 'a', 't', 'a' });
            bw.Write(dataSize);
            foreach (float s in samples)
            {
                short v = (short)Mathf.Clamp((int)(s * 32767f), -32768, 32767);
                bw.Write(v);
            }
        }
    }

    private void DeleteEntry(int index)
    {
        if (selectedGenerator == null) return;
        selectedGenerator.RemoveHistoryEntry(index, true);
        if (selectedHistoryIndex >= selectedGenerator.history.Count) selectedHistoryIndex = selectedGenerator.history.Count - 1;
        EditorUtility.SetDirty(selectedGenerator);
    }

    private void DeleteAllForPrompt()
    {
        if (selectedGenerator == null || selectedGenerator.history == null || string.IsNullOrEmpty(selectedGenerator.prompt)) return;
        int removed = 0;
        for (int i = selectedGenerator.history.Count - 1; i >= 0; i--)
        {
            if (selectedGenerator.history[i] != null && string.Equals(selectedGenerator.history[i].prompt, selectedGenerator.prompt, StringComparison.Ordinal))
            {
                selectedGenerator.RemoveHistoryEntry(i, true);
                removed++;
            }
        }
        selectedHistoryIndex = -1;
        EditorUtility.SetDirty(selectedGenerator);
        Debug.Log($"[Generator Preview] Removed {removed} entries for current prompt.");
    }
}
#endif
