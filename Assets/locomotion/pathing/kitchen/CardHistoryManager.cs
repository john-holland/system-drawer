using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene service: ring-buffer history of card pool snapshots (no live card references).
/// </summary>
[AddComponentMenu("Locomotion/Kitchen/Card History Manager")]
public sealed class CardHistoryManager : MonoBehaviour
{
    public static CardHistoryManager Instance { get; private set; }

    [Tooltip("Ring buffer capacity for history snapshots.")]
    public int historyBufferSize = 5000;

    readonly List<CardHistorySnapshot> _history = new List<CardHistorySnapshot>(512);
    readonly List<CardHistorySnapshot> _activeScratch = new List<CardHistorySnapshot>(64);
    int _writeIndex;

    void Awake()
    {
        if (Instance == null) Instance = this;
        if (historyBufferSize < 16) historyBufferSize = 16;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int HistoryCount => _history.Count;

    public void ClearHistory()
    {
        _history.Clear();
        _writeIndex = 0;
    }

    public void SetBufferSize(int size)
    {
        historyBufferSize = Mathf.Max(16, size);
        while (_history.Count > historyBufferSize)
            _history.RemoveAt(0);
    }

    public void RecordPool(PhysicsCardSolver solver, string eventKind = "pool")
    {
        if (solver == null || solver.availableCards == null) return;
        string sid = solver.gameObject != null ? solver.gameObject.name : "solver";
        for (int i = 0; i < solver.availableCards.Count; i++)
        {
            var card = solver.availableCards[i];
            Push(CardHistorySnapshot.FromCard(card, sid, eventKind));
        }
        // Also push a sentinel pool marker when empty so clear is visible
        if (solver.availableCards.Count == 0)
            Push(new CardHistorySnapshot
            {
                typeName = "PoolEmpty",
                displayName = "(empty)",
                actorOrSolverId = sid,
                eventKind = eventKind,
                unixMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
    }

    public void RecordCard(GoodSection card, PhysicsCardSolver solver, string eventKind)
    {
        string sid = solver != null && solver.gameObject != null ? solver.gameObject.name : "";
        Push(CardHistorySnapshot.FromCard(card, sid, eventKind));
    }

    void Push(CardHistorySnapshot snap)
    {
        if (snap == null) return;
        if (_history.Count < historyBufferSize)
        {
            _history.Add(snap);
            _writeIndex = _history.Count;
            return;
        }
        // Drop oldest
        _history.RemoveAt(0);
        _history.Add(snap);
    }

    public IReadOnlyList<CardHistorySnapshot> GetHistoryNewestFirst(int max = 200)
    {
        int n = Mathf.Min(max, _history.Count);
        var list = new List<CardHistorySnapshot>(n);
        for (int i = _history.Count - 1; i >= 0 && list.Count < n; i--)
            list.Add(_history[i]);
        return list;
    }

    public IReadOnlyList<CardHistorySnapshot> CopyActiveFrom(PhysicsCardSolver solver)
    {
        _activeScratch.Clear();
        if (solver == null || solver.availableCards == null) return _activeScratch;
        string sid = solver.gameObject != null ? solver.gameObject.name : "solver";
        for (int i = 0; i < solver.availableCards.Count; i++)
            _activeScratch.Add(CardHistorySnapshot.FromCard(solver.availableCards[i], sid, "active"));
        return _activeScratch;
    }
}
