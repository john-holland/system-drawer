using System;

/// <summary>RAII scope handle for PerfTrace.</summary>
public readonly struct PerfTraceScope : IDisposable
{
    readonly int _token;
    readonly PerfTraceGrade _grade;
    readonly bool _active;

    internal PerfTraceScope(int token, PerfTraceGrade grade, bool active)
    {
        _token = token;
        _grade = grade;
        _active = active;
    }

    public void Dispose()
    {
        if (_active)
            PerfTrace.EndScopeInternal(_token, _grade);
    }
}
