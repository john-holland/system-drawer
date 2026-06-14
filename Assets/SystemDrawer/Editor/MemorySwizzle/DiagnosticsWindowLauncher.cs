#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Registers Diagnostics menu entries from the Memory Swizzle editor assembly (always loaded).</summary>
public static class DiagnosticsWindowLauncher
{
    const string PerfTraceAssembly = "SystemDrawer.PerfTrace.Editor";
    const string PerfTraceWindowType = "PerfTraceViewWindow";

    [MenuItem("Window/System Drawer/Diagnostics/Perf Trace View", false, 51)]
    public static void OpenPerfTrace()
    {
        if (!TryInvokeStaticOpen(PerfTraceAssembly, PerfTraceWindowType))
        {
            Debug.LogWarning(
                "Perf Trace View is unavailable. Ensure SystemDrawer.PerfTrace.Editor compiles " +
                "(Window → System Drawer → Diagnostics → Perf Trace View).");
        }
    }

    [MenuItem("Window/System Drawer/Diagnostics/Perf Trace/Toggle ENABLE_PERF_TRACE Define", false, 120)]
    public static void TogglePerfTraceDefine()
    {
        if (!TryInvokeStaticOpen(PerfTraceAssembly, "PerfTraceDefineSymbols", "ToggleFineDefine"))
        {
            Debug.LogWarning(
                "Perf Trace define toggle unavailable. Ensure SystemDrawer.PerfTrace.Editor compiles.");
        }
    }

    public static bool TryOpenPerfTrace() => TryInvokeStaticOpen(PerfTraceAssembly, PerfTraceWindowType);

    static bool TryInvokeStaticOpen(string assemblyName, string typeName, string methodName = "Open")
    {
        var type = FindEditorType(assemblyName, typeName);
        if (type == null)
            return false;

        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            return false;

        method.Invoke(null, null);
        return true;
    }

    static Type FindEditorType(string assemblyName, string typeName)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);
        return assembly?.GetType(typeName);
    }
}
#endif
