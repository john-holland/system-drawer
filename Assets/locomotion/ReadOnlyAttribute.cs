using UnityEngine;

/// <summary>
/// Marks a serialized field as read-only in the Unity Inspector.
/// Runtime-safe: the attribute exists in player builds; the editor drawer is editor-only.
/// </summary>
public sealed class ReadOnlyAttribute : PropertyAttribute
{
}

