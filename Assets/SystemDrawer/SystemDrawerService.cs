using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service conglomerator with weak links to library assets (narrative calendar, weather, spatial 4D, etc.).
/// Query by string key; references are optional so packages can be excluded. Register via Register(key, obj).
/// </summary>
public class SystemDrawerService : MonoBehaviour
{
    public static SystemDrawerService Instance { get; private set; }

    [Tooltip("Optional: persist as single instance across scenes.")]
    [SerializeField] private bool dontDestroyOnLoad;

    [Header("Weak links (key = string, value = Object)")]
    [SerializeField] private List<string> keys = new List<string>();
    [SerializeField] private List<Object> values = new List<Object>();

    private Dictionary<string, Object> _map;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (dontDestroyOnLoad) return;
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
        RebuildMap();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void RebuildMap()
    {
        _map = new Dictionary<string, Object>();
        int n = Mathf.Min(keys != null ? keys.Count : 0, values != null ? values.Count : 0);
        for (int i = 0; i < n; i++)
            if (!string.IsNullOrEmpty(keys[i]) && values[i] != null)
                _map[keys[i]] = values[i];
    }

    /// <summary>Find the first SystemDrawerService in the scene (for editor or when Instance not set).</summary>
    public static SystemDrawerService FindInScene()
    {
        return Instance != null ? Instance : Object.FindAnyObjectByType<SystemDrawerService>();
    }

    /// <summary>Get a registered system by key. Returns null if not found or package not present.</summary>
    public T Get<T>(string key) where T : Object
    {
        if (_map == null) RebuildMap();
        if (_map != null && _map.TryGetValue(key, out Object obj) && obj != null)
            return obj as T;
        return null;
    }

    /// <summary>Register a system under the given key. Use from wizards or OnEnable of systems.</summary>
    public void Register(string key, Object obj)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (_map == null) RebuildMap();
        if (_map == null) _map = new Dictionary<string, Object>();
        _map[key] = obj;
        if (keys == null) keys = new List<string>();
        if (values == null) values = new List<Object>();
        int i = keys.IndexOf(key);
        if (i >= 0)
        {
            values[i] = obj;
            return;
        }
        keys.Add(key);
        values.Add(obj);
    }

    /// <summary>Remove registration for a key.</summary>
    public void Unregister(string key)
    {
        if (_map != null) _map.Remove(key);
        if (keys != null)
        {
            int i = keys.IndexOf(key);
            if (i >= 0 && values != null && i < values.Count)
            {
                keys.RemoveAt(i);
                values.RemoveAt(i);
            }
        }
    }
}
