using System;
using System.Collections.Generic;
using UnityEngine;

public struct KeyStroke
{
    public ComputerKeyId id;
    public char unicode;
    public ComputerKey key;
    public bool isPress;
}

/// <summary>Queues typed words / key configs for the computer keyboard.</summary>
[AddComponentMenu("Locomotion/Periphery/Keyboard Message Pump")]
public sealed class KeyboardMessagePump : MonoBehaviour
{
    public ComputerKeyboardRuntime keyboard;
    readonly Queue<KeyStroke> _queue = new Queue<KeyStroke>();

    public int Count => _queue.Count;

    public void Clear() => _queue.Clear();

    public void EnqueueText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            ComputerKey key = null;
            keyboard?.TryGetKeyByUnicode(c, out key);
            _queue.Enqueue(new KeyStroke
            {
                unicode = c,
                key = key,
                id = key != null ? key.id : ComputerKeyId.Space,
                isPress = true
            });
        }
    }

    public void EnqueueKeys(IEnumerable<ComputerKeyId> ids)
    {
        if (ids == null) return;
        foreach (var id in ids)
        {
            ComputerKey key = null;
            keyboard?.TryGetKey(id, out key);
            _queue.Enqueue(new KeyStroke { id = id, key = key, unicode = key != null ? key.unicode : '\0', isPress = true });
        }
    }

    public void EnqueueFromGameObject(GameObject configRoot)
    {
        if (configRoot == null) return;
        var keys = configRoot.GetComponentsInChildren<ComputerKey>(true);
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] == null) continue;
            _queue.Enqueue(new KeyStroke { id = keys[i].id, key = keys[i], unicode = keys[i].unicode, isPress = true });
        }
    }

    public bool TryDequeue(out KeyStroke stroke)
    {
        if (_queue.Count == 0)
        {
            stroke = default;
            return false;
        }
        stroke = _queue.Dequeue();
        return true;
    }
}
