using System;
using UnityEngine;

/// <summary>Named menu event for bubble-up / broadcast-descend routing.</summary>
public readonly struct MenuRagdollEvent
{
    public string Name { get; }
    public MenuRagdollNode Source { get; }
    public object Payload { get; }

    public MenuRagdollEvent(string name, MenuRagdollNode source, object payload = null)
    {
        Name = name ?? "";
        Source = source;
        Payload = payload;
    }
}
