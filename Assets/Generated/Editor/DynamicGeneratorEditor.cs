#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DynamicGeneratorBase), true)]
public class DynamicGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var gen = (DynamicGeneratorBase)target;
        if (gen is DynamicShaderGenerator shaderGen)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Shader template or script", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            shaderGen.scriptOrTemplate = EditorGUILayout.ObjectField(shaderGen.scriptOrTemplate, typeof(UnityEngine.Object), false);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(shaderGen);
            if (shaderGen.scriptOrTemplate != null && GUILayout.Button("Open in IDE", GUILayout.Height(22)))
                AssetDatabase.OpenAsset(shaderGen.scriptOrTemplate);
        }
        EditorGUILayout.Space(4);
        if (GUILayout.Button("Generate (stub)", GUILayout.Height(24)))
        {
            GenerateStub(gen);
        }
        if (GUILayout.Button("Open Preview Window", GUILayout.Height(22)))
        {
            var w = DynamicGeneratorPreviewWindow.ShowWindow();
            if (w != null) w.SetSelectedGenerator(gen);
        }
        if (gen is DynamicShaderGenerator shaderGen2 && GUILayout.Button("Generate textures for dependencies", GUILayout.Height(22)))
            GenerateTexturesForDependencies(shaderGen2);
    }

    private static void GenerateStub(DynamicGeneratorBase gen)
    {
        if (gen is DynamicTextureUIGenerator texGen)
        {
            var tex = new Texture2D(texGen.resolutionX, texGen.resolutionY, texGen.format, false);
            for (int y = 0; y < tex.height; y++)
                for (int x = 0; x < tex.width; x++)
                    tex.SetPixel(x, y, new Color((float)x / tex.width, (float)y / tex.height, 0.5f, 1f));
            tex.Apply();
            string dir = "Assets/Generated/Texture";
            if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
            if (!AssetDatabase.IsValidFolder("Assets/Generated/Texture")) AssetDatabase.CreateFolder("Assets/Generated", "Texture");
            string path = dir + "/" + gen.name + "_" + System.DateTime.UtcNow.Ticks + ".asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(tex, path);
            var entry = gen.AddHistoryEntry(gen.prompt, path, tex, "stub");
            var thumb = ResizeTexture(tex, 64, 64);
            if (thumb != null) entry.thumbnail = thumb.EncodeToPNG();
            if (gen.prebakeAndSave)
            {
                var registry = Object.FindAnyObjectByType<SceneObjectRegistry>();
                if (registry != null)
                {
                    string prefabDir = "Assets/Generated/Prefabs";
                    if (!AssetDatabase.IsValidFolder("Assets/Generated/Prefabs")) AssetDatabase.CreateFolder("Assets/Generated", "Prefabs");
                    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    go.name = "Texture_" + gen.GetOrmKey();
                    var mat = new Material(Shader.Find("Unlit/Texture"));
                    mat.mainTexture = tex;
                    go.GetComponent<Renderer>().sharedMaterial = mat;
                    string prefabPath = prefabDir + "/" + go.name + ".prefab";
                    prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
                    var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                    Object.DestroyImmediate(go);
                    if (prefab != null)
                        registry.Register(gen.GetOrmKey(), prefab, true);
                }
            }
            EditorUtility.SetDirty(gen);
            AssetDatabase.SaveAssets();
            return;
        }
        if (gen is DynamicAudioGenerator || gen is DynamicMusicGenerator)
        {
            float len = gen is DynamicMusicGenerator m ? m.lengthSeconds : ((DynamicAudioGenerator)gen).lengthSeconds;
            int sampleRate = gen is DynamicMusicGenerator m2 ? 44100 : ((DynamicAudioGenerator)gen).sampleRate;
            int channels = gen is DynamicMusicGenerator m3 ? 2 : ((DynamicAudioGenerator)gen).channels;
            int numSamples = Mathf.RoundToInt(len * sampleRate) * channels;
            float[] data = new float[numSamples];
            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Sin(2f * Mathf.PI * 440f * i / sampleRate / channels) * 0.1f;
            string dir = "Assets/Generated/Audio";
            if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
            if (!AssetDatabase.IsValidFolder("Assets/Generated/Audio")) AssetDatabase.CreateFolder("Assets/Generated", "Audio");
            string wavPath = dir + "/" + gen.name + "_" + System.DateTime.UtcNow.Ticks + ".wav";
            wavPath = AssetDatabase.GenerateUniqueAssetPath(wavPath);
            WriteWavFile(wavPath, data, sampleRate, channels);
            AssetDatabase.ImportAsset(wavPath);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(wavPath);
            gen.AddHistoryEntry(gen.prompt, wavPath, clip, "stub");
            EditorUtility.SetDirty(gen);
            AssetDatabase.SaveAssets();
            return;
        }
        if (gen is DynamicSoundToMLGenerator soundMLGen)
        {
            EnsureGeneratedFolders();
            string dir = "Assets/Generated/Audio";
            if (!AssetDatabase.IsValidFolder("Assets/Generated/Audio")) AssetDatabase.CreateFolder("Assets/Generated", "Audio");
            int sampleRate = 44100;
            int channels = 2;
            int numSamples = Mathf.RoundToInt(soundMLGen.lengthSeconds * sampleRate) * channels;
            float[] data = new float[numSamples];
            for (int i = 0; i < data.Length; i++)
                data[i] = Mathf.Sin(2f * Mathf.PI * 440f * i / sampleRate / channels) * 0.1f;
            string wavPath = dir + "/" + soundMLGen.name + "_" + System.DateTime.UtcNow.Ticks + ".wav";
            wavPath = AssetDatabase.GenerateUniqueAssetPath(wavPath);
            WriteWavFile(wavPath, data, sampleRate, channels);
            AssetDatabase.ImportAsset(wavPath);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(wavPath);
            var entry = gen.AddHistoryEntry(gen.prompt, wavPath, clip, "stub");
            if (entry.sourcePrimitiveKeys != null && !string.IsNullOrWhiteSpace(gen.soundKey))
                entry.sourcePrimitiveKeys.Add(gen.soundKey);
            if (gen.prebakeAndSave)
            {
                var loader = Object.FindAnyObjectByType<AssetLoader>();
                if (loader != null)
                    loader.RegisterAudioClipKey(soundMLGen.GetOrmKey() + "_audio", wavPath);
            }
            EditorUtility.SetDirty(gen);
            AssetDatabase.SaveAssets();
            return;
        }
        if (gen is Dynamic3DObjectGenerator threeDGen)
        {
            EnsureGeneratedFolders();
            PrimitiveAssetStoreEditor.EnsurePrimitivesFolderAndStore();
            var store = AssetDatabase.LoadAssetAtPath<PrimitiveAssetStore>("Assets/Generated/Primitives/PrimitiveAssetStore.asset");
            string promptKey = null;
            if (store != null && !string.IsNullOrWhiteSpace(gen.prompt))
            {
                promptKey = threeDGen.GetOrmKey() + "_prompt";
                store.AddOrReplace(new PrimitiveAssetEntry(promptKey, PrimitiveAssetType.Prompt, "", gen.prompt));
                EditorUtility.SetDirty(store);
            }
            string prefabDir = "Assets/Generated/Prefabs";
            PrimitiveType primitiveType = threeDGen.isCharacter ? PrimitiveType.Capsule : PrimitiveType.Cube;
            var go = GameObject.CreatePrimitive(primitiveType);
            go.name = (threeDGen.isCharacter ? "Char_" : "Obj_") + threeDGen.GetOrmKey();
            if (threeDGen.scale != Vector3.one)
                go.transform.localScale = threeDGen.scale;
            string prefabPath = prefabDir + "/" + go.name + ".prefab";
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            if (prefab != null)
            {
                var entry = gen.AddHistoryEntry(gen.prompt, prefabPath, prefab, "stub");
                if (!string.IsNullOrEmpty(promptKey) && entry.sourcePrimitiveKeys != null)
                    entry.sourcePrimitiveKeys.Add(promptKey);
                if (gen.prebakeAndSave)
                {
                    var registry = Object.FindAnyObjectByType<SceneObjectRegistry>();
                    if (registry != null)
                        registry.Register(threeDGen.GetOrmKey(), prefab, true);
                }
            }
            EditorUtility.SetDirty(gen);
            AssetDatabase.SaveAssets();
            return;
        }
        if (gen is DynamicAnimationGenerator animGen)
        {
            EnsureGeneratedFolders();
            PrimitiveAssetStoreEditor.EnsurePrimitivesFolderAndStore();
            var store = AssetDatabase.LoadAssetAtPath<PrimitiveAssetStore>("Assets/Generated/Primitives/PrimitiveAssetStore.asset");
            string promptKey = null;
            if (store != null && !string.IsNullOrWhiteSpace(gen.prompt))
            {
                promptKey = animGen.GetOrmKey() + "_prompt";
                store.AddOrReplace(new PrimitiveAssetEntry(promptKey, PrimitiveAssetType.Prompt, "", gen.prompt));
                EditorUtility.SetDirty(store);
            }
            string dir = "Assets/Generated/Animation";
            if (!AssetDatabase.IsValidFolder("Assets/Generated/Animation")) AssetDatabase.CreateFolder("Assets/Generated", "Animation");
            var clip = new AnimationClip();
            clip.frameRate = 30f;
            clip.legacy = false;
            string path = dir + "/" + animGen.name + "_" + System.DateTime.UtcNow.Ticks + ".anim";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(clip, path);
            var entry = gen.AddHistoryEntry(gen.prompt, path, clip, "stub");
            if (!string.IsNullOrEmpty(promptKey) && entry.sourcePrimitiveKeys != null)
                entry.sourcePrimitiveKeys.Add(promptKey);
            if (gen.prebakeAndSave)
            {
                var loader = Object.FindAnyObjectByType<AssetLoader>();
                if (loader != null)
                    loader.RegisterAnimationClipKey(animGen.GetOrmKey() + "_clip", path);
            }
            EditorUtility.SetDirty(gen);
            AssetDatabase.SaveAssets();
            return;
        }
        if (gen is DynamicVideoToAnimationGenerator video2AnimGen)
        {
            EnsureGeneratedFolders();
            string dir = "Assets/Generated/Animation";
            if (!AssetDatabase.IsValidFolder("Assets/Generated/Animation")) AssetDatabase.CreateFolder("Assets/Generated", "Animation");
            var clip = new AnimationClip();
            clip.frameRate = 30f;
            clip.legacy = false;
            string path = dir + "/" + video2AnimGen.name + "_" + System.DateTime.UtcNow.Ticks + ".anim";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(clip, path);
            var entry = gen.AddHistoryEntry(gen.prompt, path, clip, "stub");
            if (entry.sourcePrimitiveKeys != null && !string.IsNullOrWhiteSpace(gen.videoKey))
                entry.sourcePrimitiveKeys.Add(gen.videoKey);
            if (gen.prebakeAndSave)
            {
                var loader = Object.FindAnyObjectByType<AssetLoader>();
                if (loader != null)
                    loader.RegisterAnimationClipKey(video2AnimGen.GetOrmKey() + "_clip", path);
            }
            EditorUtility.SetDirty(gen);
            AssetDatabase.SaveAssets();
            return;
        }
        if (gen is DynamicImagesToAnimationGenerator imgs2AnimGen)
        {
            EnsureGeneratedFolders();
            string dir = "Assets/Generated/Animation";
            if (!AssetDatabase.IsValidFolder("Assets/Generated/Animation")) AssetDatabase.CreateFolder("Assets/Generated", "Animation");
            var clip = new AnimationClip();
            clip.frameRate = 30f;
            clip.legacy = false;
            string path = dir + "/" + imgs2AnimGen.name + "_" + System.DateTime.UtcNow.Ticks + ".anim";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(clip, path);
            var entry = gen.AddHistoryEntry(gen.prompt, path, clip, "stub");
            if (entry.sourcePrimitiveKeys != null && imgs2AnimGen.imageKeys != null)
            {
                foreach (var k in imgs2AnimGen.imageKeys)
                    if (!string.IsNullOrWhiteSpace(k))
                        entry.sourcePrimitiveKeys.Add(k);
            }
            if (gen.prebakeAndSave)
            {
                var loader = Object.FindAnyObjectByType<AssetLoader>();
                if (loader != null)
                    loader.RegisterAnimationClipKey(imgs2AnimGen.GetOrmKey() + "_clip", path);
            }
            EditorUtility.SetDirty(gen);
            AssetDatabase.SaveAssets();
            return;
        }
        if (gen is DynamicImageTo3DCharacterGenerator img2CharGen)
        {
            EnsureGeneratedFolders();
            string prefabDir = "Assets/Generated/Prefabs";
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Img2Char_" + img2CharGen.GetOrmKey();
            if (img2CharGen.scale != Vector3.one)
                go.transform.localScale = img2CharGen.scale;
            string prefabPath = prefabDir + "/" + go.name + ".prefab";
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            if (prefab != null)
            {
                var entry = gen.AddHistoryEntry(gen.prompt, prefabPath, prefab, "stub");
                if (entry.sourcePrimitiveKeys != null && !string.IsNullOrWhiteSpace(gen.imageKey))
                    entry.sourcePrimitiveKeys.Add(gen.imageKey);
                if (gen.prebakeAndSave)
                {
                    var registry = Object.FindAnyObjectByType<SceneObjectRegistry>();
                    if (registry != null)
                        registry.Register(img2CharGen.GetOrmKey(), prefab, true);
                }
            }
            EditorUtility.SetDirty(gen);
            AssetDatabase.SaveAssets();
            return;
        }
        if (gen is DynamicShaderGenerator shaderGen)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
            if (!AssetDatabase.IsValidFolder("Assets/Generated/Shaders")) AssetDatabase.CreateFolder("Assets/Generated", "Shaders");
            string shaderSource = null;
            string modelUsed = "stub";
            if (shaderGen.useLmStudioForGenerate)
            {
                string systemPrompt = "You generate Unity ShaderLab shaders. Output only valid shader code, no markdown or explanation.";
                string userPrompt = BuildShaderLmPrompt(shaderGen);
                string modelId = !string.IsNullOrEmpty(shaderGen.lmStudioModelId)
                    ? shaderGen.lmStudioModelId
                    : GetFirstFilteredLmStudioModel(gen);
                if (!string.IsNullOrEmpty(modelId) && LmStudioModelService.RequestChatCompletion(modelId, systemPrompt, userPrompt, out string response))
                {
                    shaderSource = response;
                    modelUsed = modelId;
                }
            }
            if (shaderSource == null)
            {
                if (shaderGen.scriptOrTemplate is TextAsset textAsset && !string.IsNullOrEmpty(textAsset.text))
                    shaderSource = string.IsNullOrEmpty(shaderGen.prompt) ? textAsset.text : "// Prompt: " + shaderGen.prompt.Replace("\n", " ") + "\n" + textAsset.text;
                else
                {
                    string promptComment = string.IsNullOrEmpty(shaderGen.prompt) ? "" : "// Prompt: " + shaderGen.prompt.Replace("\n", " ") + "\n";
                    shaderSource = promptComment + @"Shader ""Generated/UnlitStub""
{
    Properties { _Color (""Color"", Color) = (1,1,1,1) }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            float4 _Color;
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert(appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
            float4 frag(v2f i) : SV_Target { return _Color; }
            ENDCG
        }
    }
    Fallback Off
}";
                }
            }
            string path = "Assets/Generated/Shaders/" + shaderGen.name + "_" + System.DateTime.UtcNow.Ticks + ".shader";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            System.IO.File.WriteAllText(path, shaderSource);
            AssetDatabase.ImportAsset(path);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null)
            {
                var entry = shaderGen.AddHistoryEntry(shaderGen.prompt, path, shader, modelUsed);
                var depKeys = GetShaderDependencyKeys(shaderGen);
                if (depKeys != null && depKeys.Count > 0)
                {
                    entry.assetDependencyKeys.Clear();
                    entry.assetDependencyKeys.AddRange(depKeys);
                }
                var graph = EnsureAssetDependencyGraph();
                if (graph != null)
                    graph.Register(shaderGen.GetOrmKey(), depKeys);
            }
            EditorUtility.SetDirty(shaderGen);
            AssetDatabase.SaveAssets();
            return;
        }
        EditorUtility.DisplayDialog("Generate", "Stub generation for this generator type not implemented yet.", "OK");
    }

    private static void EnsureGeneratedFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder("Assets/Generated/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Generated", "Prefabs");
    }

    private static Texture2D ResizeTexture(Texture2D src, int w, int h)
    {
        if (src == null || w <= 0 || h <= 0) return null;
        var rt = RenderTexture.GetTemporary(w, h);
        RenderTexture.active = rt;
        var resized = new Texture2D(w, h);
        Graphics.Blit(src, rt);
        resized.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        resized.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return resized;
    }

    private static void WriteWavFile(string path, float[] samples, int sampleRate, int channels)
    {
        using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create))
        using (var bw = new System.IO.BinaryWriter(fs))
        {
            int subchunk1Size = 16;
            int bitsPerSample = 16;
            int blockAlign = channels * (bitsPerSample / 8);
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

    private static string BuildShaderLmPrompt(DynamicShaderGenerator shaderGen)
    {
        var parts = new System.Collections.Generic.List<string> { shaderGen.prompt ?? "" };
        if (shaderGen.grammarIndex != null)
        {
            var spec = shaderGen.grammarIndex.ToPromptSpec();
            if (!string.IsNullOrEmpty(spec)) parts.Add(spec);
        }
        if (shaderGen.parameterSchema != null)
        {
            var spec = shaderGen.parameterSchema.ToPromptSpec();
            if (!string.IsNullOrEmpty(spec)) parts.Add(spec);
        }
        return string.Join("\n\n", parts);
    }

    private static string GetFirstFilteredLmStudioModel(DynamicGeneratorBase gen)
    {
        var models = LmStudioModelService.GetLmStudioModels();
        var keywords = gen.modelKeywords != null ? gen.modelKeywords : new System.Collections.Generic.List<string>();
        var filtered = LmStudioModelService.FilterByKeywords(models, keywords);
        return filtered.Count > 0 ? filtered[0] : null;
    }

    private static System.Collections.Generic.List<string> GetShaderDependencyKeys(DynamicShaderGenerator shaderGen)
    {
        var list = new System.Collections.Generic.List<string>();
        var schema = shaderGen.parameterSchema;
        var slotIds = schema != null ? schema.GetDependencySlotIdsForMaterialType(shaderGen.materialType) : new System.Collections.Generic.List<string> { "albedo" };
        string baseKey = shaderGen.GetOrmKey();
        foreach (var slotId in slotIds)
            list.Add(baseKey + "_" + slotId);
        return list;
    }

    private static AssetDependencyGraph EnsureAssetDependencyGraph()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        const string path = "Assets/Generated/AssetDependencyGraph.asset";
        var graph = AssetDatabase.LoadAssetAtPath<AssetDependencyGraph>(path);
        if (graph == null)
        {
            graph = ScriptableObject.CreateInstance<AssetDependencyGraph>();
            AssetDatabase.CreateAsset(graph, path);
        }
        return graph;
    }

    /// <summary>Create texture generators for the current shader result's dependency keys. Call from inspector or preview window.</summary>
    public static void GenerateTexturesForDependencies(DynamicShaderGenerator shaderGen)
    {
        var entry = shaderGen != null ? shaderGen.GetCurrentResult() : null;
        if (entry == null || entry.assetDependencyKeys == null || entry.assetDependencyKeys.Count == 0)
        {
            EditorUtility.DisplayDialog("Dependencies", "No dependency keys on current shader result. Generate a shader first.", "OK");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder("Assets/Generated/Texture")) AssetDatabase.CreateFolder("Assets/Generated", "Texture");
        int created = 0;
        foreach (var key in entry.assetDependencyKeys)
        {
            if (string.IsNullOrEmpty(key)) continue;
            string slotName = key.Contains("_") ? key.Substring(key.IndexOf('_') + 1) : key;
            var texGen = ScriptableObject.CreateInstance<DynamicTextureUIGenerator>();
            texGen.prompt = (shaderGen.prompt ?? "").Trim() + " " + slotName + " map";
            texGen.name = shaderGen.name + "_" + slotName;
            string assetPath = "Assets/Generated/Texture/" + texGen.name + ".asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            AssetDatabase.CreateAsset(texGen, assetPath);
            created++;
        }
        EditorUtility.SetDirty(shaderGen);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Generate textures for dependencies", "Created " + created + " texture generator(s). Run them from their inspectors or the Preview window.", "OK");
    }
}
#endif
