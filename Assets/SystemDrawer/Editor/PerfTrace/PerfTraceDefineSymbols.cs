#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;

/// <summary>Optional scripting define helpers for PerfTrace.</summary>
public static class PerfTraceDefineSymbols
{
    const string FineSymbol = "ENABLE_PERF_TRACE";

    static NamedBuildTarget ActiveNamedTarget =>
        NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

    public static void ToggleFineDefine()
    {
        bool enabled = HasDefine(FineSymbol);
        SetDefine(FineSymbol, !enabled);
        EditorUtility.DisplayDialog("Perf Trace",
            enabled ? "Removed ENABLE_PERF_TRACE from active build target." : "Added ENABLE_PERF_TRACE to active build target.",
            "OK");
    }

    public static bool HasDefine(string symbol)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbols(ActiveNamedTarget);
        return defines.Contains(symbol);
    }

    public static void SetDefine(string symbol, bool enabled)
    {
        var set = new System.Collections.Generic.HashSet<string>(
            PlayerSettings.GetScriptingDefineSymbols(ActiveNamedTarget).Split(';'));
        if (enabled)
            set.Add(symbol);
        else
            set.Remove(symbol);
        set.Remove("");
        PlayerSettings.SetScriptingDefineSymbols(ActiveNamedTarget, string.Join(";", set));
    }
}
#endif
