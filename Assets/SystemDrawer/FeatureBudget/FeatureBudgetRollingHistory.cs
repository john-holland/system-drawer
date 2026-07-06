using System.Collections.Generic;
using UnityEngine;

public sealed class FeatureBudgetRollingHistory
{
    readonly Queue<float> _samples = new Queue<float>();
    readonly int _capacity;
    float _sum;

    public FeatureBudgetRollingHistory(int capacity)
    {
        _capacity = Mathf.Max(1, capacity);
    }

    public float RollingAverage { get; private set; }
    public float LastSample { get; private set; }

    public void Push(float sample)
    {
        LastSample = sample;
        _samples.Enqueue(sample);
        _sum += sample;
        while (_samples.Count > _capacity)
            _sum -= _samples.Dequeue();
        RollingAverage = _sum / _samples.Count;
    }

    public void Reset()
    {
        _samples.Clear();
        _sum = 0f;
        RollingAverage = 0f;
        LastSample = 0f;
    }
}
