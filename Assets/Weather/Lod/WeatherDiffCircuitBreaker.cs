using System;
using UnityEngine;

namespace Weather.Lod
{
    public enum WeatherDiffCircuitState
    {
        Closed,
        Open,
        HalfOpen
    }

    /// <summary>Circuit breaker for sparse diff vs hyperplane fold.</summary>
    public sealed class WeatherDiffCircuitBreaker
    {
        public int byteBudget = 8192;
        public float residualEpsilon = 0.05f;
        public float halfOpenCooldownSeconds = 2f;

        WeatherDiffCircuitState _state = WeatherDiffCircuitState.Closed;
        float _openedAt = -999f;

        public WeatherDiffCircuitState State => _state;

        public bool ShouldFoldToRegression(int diffBytes, float residualVariance)
        {
            if (_state == WeatherDiffCircuitState.Open)
            {
                if (Time.time - _openedAt >= halfOpenCooldownSeconds)
                    _state = WeatherDiffCircuitState.HalfOpen;
                else
                    return true;
            }

            bool fold = diffBytes > byteBudget || residualVariance < residualEpsilon;
            if (fold)
            {
                _state = WeatherDiffCircuitState.Open;
                _openedAt = Time.time;
            }
            else if (_state == WeatherDiffCircuitState.HalfOpen)
            {
                _state = WeatherDiffCircuitState.Closed;
            }

            return fold;
        }

        public void RecordTimeout(int order)
        {
            if (order > 0)
            {
                _state = WeatherDiffCircuitState.Open;
                _openedAt = Time.time;
            }
        }
    }
}
