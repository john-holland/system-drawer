#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Batch/WebGL build for <see cref="Continuuuum.Library.ContinuuuumLibraryWebController"/> hosted under continuuuum Flask static <c>library/</c>.
/// CLI: <c>-batchmode -nographics -quit -executeMethod BuildContinuuuumWebGL.BuildFromCli -continuuuumWebGlOut &lt;dir&gt;</c>
/// </summary>
public static class BuildContinuuuumWebGL
{
    public const string ScenePath = "Assets/Scenes/ContinuuuumLibraryWeb.unity";

    [MenuItem("Continuuuum/Build WebGL Library Editor")]
    public static void BuildMenu()
    {
        string continuuuumSibling =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "continuuuum", "library", "continuuuum_editor_webgl"));
        RunBuild(continuuuumSibling, exitEditorWhenDone: false);
    }

    /// <summary>Entry point for Unity CLI (<c>-executeMethod BuildContinuuuumWebGL.BuildFromCli</c>).</summary>
    public static void BuildFromCli()
    {
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "continuuuum", "library", "continuuuum_editor_webgl"));
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "-continuuuumWebGlOut", StringComparison.OrdinalIgnoreCase))
                outDir = Path.GetFullPath(args[i + 1]);
        }

        RunBuild(outDir, exitEditorWhenDone: true);
    }

    private static void RunBuild(string outputDirectory, bool exitEditorWhenDone)
    {
        string sceneFull = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ScenePath));
        if (!File.Exists(sceneFull))
        {
            Debug.LogError($"[Continuuuum WebGL] Scene missing: {sceneFull}");
            if (exitEditorWhenDone) EditorApplication.Exit(1);
            return;
        }

        if (!ScenePath.StartsWith("Assets/", StringComparison.Ordinal))
        {
            Debug.LogError("[Continuuuum WebGL] ScenePath must be under Assets/.");
            if (exitEditorWhenDone) EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory(outputDirectory);

        var scenes = EditorBuildSettings.scenes;
        var injected = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        try
        {
            EditorBuildSettings.scenes = injected;

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[Continuuuum WebGL] Build failed: " + report.summary.result);
                if (exitEditorWhenDone) EditorApplication.Exit(1);
                return;
            }

            PatchIndexHtml(outputDirectory);
            Debug.Log("[Continuuuum WebGL] Built to " + outputDirectory);
        }
        finally
        {
            EditorBuildSettings.scenes = scenes;
        }

        if (exitEditorWhenDone)
            EditorApplication.Exit(0);
    }

    /// <summary>Injects <c>&lt;base href&gt;</c> so the player loads when nested under <c>/library/continuuuum_editor_webgl/</c>.</summary>
    private static void PatchIndexHtml(string buildDir)
    {
        string baseHref = Environment.GetEnvironmentVariable("CONTINUUUUM_WEBGL_BASE_HREF");
        if (string.IsNullOrWhiteSpace(baseHref))
            baseHref = "/library/continuuuum_editor_webgl/";
        baseHref = baseHref.Trim();
        if (!baseHref.EndsWith("/", StringComparison.Ordinal))
            baseHref += "/";

        string indexPath = Path.Combine(buildDir, "index.html");
        if (!File.Exists(indexPath))
        {
            Debug.LogWarning("[Continuuuum WebGL] No index.html to patch at " + indexPath);
            return;
        }

        string html = File.ReadAllText(indexPath);
        if (html.IndexOf("<base ", StringComparison.OrdinalIgnoreCase) >= 0)
            return;

        const string needle = "<head>";
        int idx = html.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            Debug.LogWarning("[Continuuuum WebGL] index.html has no <head>; skipping base href inject.");
            return;
        }

        string inject = needle + "\n    <base href=\"" + baseHref + "\">";
        html = html.Substring(0, idx) + inject + html.Substring(idx + needle.Length);
        File.WriteAllText(indexPath, html);
        Debug.Log("[Continuuuum WebGL] Patched index.html base href -> " + baseHref);
    }
}
#endif
