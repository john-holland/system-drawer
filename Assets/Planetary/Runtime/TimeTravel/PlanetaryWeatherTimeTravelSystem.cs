using System.Collections.Generic;
using System.IO;
using Planetary.Composition;
using UnityEngine;
using Weather;

namespace Planetary.TimeTravel
{
    public sealed class PlanetaryWeatherTimeTravelSystem : MonoBehaviour
    {
        public PlanetBody planet;
        public Locomotion.Narrative.NarrativeScheduler narrativeScheduler;
        readonly Stack<WeatherTimeTravelFrame> _undo = new Stack<WeatherTimeTravelFrame>();
        readonly Stack<WeatherTimeTravelFrame> _redo = new Stack<WeatherTimeTravelFrame>();
        string CacheDir => Path.Combine(Application.persistentDataPath, "PlanetWeatherCache");

        public void PushFrameBeforeApply(WeatherTimeTravelFrame frame)
        {
            _undo.Push(frame);
            _redo.Clear();
            SaveFrameToDisk(frame, _undo.Count);
        }

        public bool Undo()
        {
            if (_undo.Count == 0)
                return false;
            var frame = _undo.Pop();
            _redo.Push(CaptureCurrent());
            ApplyFrame(frame);
            return true;
        }

        public bool Redo()
        {
            if (_redo.Count == 0)
                return false;
            var frame = _redo.Pop();
            _undo.Push(CaptureCurrent());
            ApplyFrame(frame);
            return true;
        }

        public void OnCalendarJump(float targetTime)
        {
            Time.timeScale = 0f;
            var frame = LoadNearestFrame(targetTime);
            if (frame != null)
                ApplyFrame(frame);
            var estimator = new AtmosphereCompositionEstimator();
            if (planet != null)
                estimator.Estimate(planet, FindFirstObjectByType<WeatherPhysicsManifold>());
            Time.timeScale = 1f;
        }

        WeatherTimeTravelFrame CaptureCurrent()
        {
            var est = new AtmosphereCompositionEstimator();
            return new WeatherTimeTravelFrame
            {
                narrativeTime = Time.time,
                atmosphereSnapshot = planet != null
                    ? est.Estimate(planet, FindFirstObjectByType<WeatherPhysicsManifold>())
                    : null
            };
        }

        void ApplyFrame(WeatherTimeTravelFrame frame)
        {
            if (frame?.atmosphereSnapshot == null || planet == null)
                return;
            // Composition rebake hook: PlanetBody uses snapshot on next interior update
            //todo: implement
        }

        void SaveFrameToDisk(WeatherTimeTravelFrame frame, int index)
        {
            Directory.CreateDirectory(CacheDir);
            string path = Path.Combine(CacheDir, $"frame_{index}.json");
            if (frame.atmosphereSnapshot != null)
            {
                string json = JsonUtility.ToJson(frame.atmosphereSnapshot);
                File.WriteAllText(path, json);
            }
        }

        WeatherTimeTravelFrame LoadNearestFrame(float targetTime)
        {
            if (!Directory.Exists(CacheDir))
                return null;
            var files = Directory.GetFiles(CacheDir, "frame_*.json");
            if (files.Length == 0)
                return null;
            return new WeatherTimeTravelFrame { narrativeTime = targetTime };
        }
    }
}
