using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Planetary.Composition;
using UnityEngine;
using Weather;

namespace Planetary.TimeTravel
{
    public sealed class PlanetaryWeatherTimeTravelSystem : MonoBehaviour
    {
        public PlanetBody planet;
        readonly Stack<WeatherTimeTravelFrame> _undo = new Stack<WeatherTimeTravelFrame>();
        readonly Stack<WeatherTimeTravelFrame> _redo = new Stack<WeatherTimeTravelFrame>();
        string CacheDir => Path.Combine(Application.persistentDataPath, "PlanetWeatherCache");

        public void PushFrameBeforeApply(WeatherTimeTravelFrame frame)
        {
            if (frame == null)
                frame = CaptureCurrentPublic();
            _undo.Push(frame);
            _redo.Clear();
            SaveFrameToDisk(frame, _undo.Count);
        }

        public bool Undo()
        {
            if (_undo.Count == 0)
                return false;
            var frame = _undo.Pop();
            _redo.Push(CaptureCurrentPublic());
            ApplyFramePublic(frame);
            return true;
        }

        public bool Redo()
        {
            if (_redo.Count == 0)
                return false;
            var frame = _redo.Pop();
            _undo.Push(CaptureCurrentPublic());
            ApplyFramePublic(frame);
            return true;
        }

        public void OnCalendarJump(float targetTime)
        {
            Time.timeScale = 0f;
            var frame = LoadNearestFrame(targetTime);
            if (frame != null)
                ApplyFramePublic(frame);
            var estimator = new AtmosphereCompositionEstimator();
            WeatherPhysicsManifold weatherManifold = null;
            SceneServiceLookup.TryResolve("weather.physicsManifold", out weatherManifold);
            if (planet != null)
                estimator.Estimate(planet, weatherManifold);
            Time.timeScale = 1f;
        }

        public WeatherTimeTravelFrame CaptureCurrentPublic() => CaptureCurrent();

        public void ApplyFramePublic(WeatherTimeTravelFrame frame) => ApplyFrame(frame);

        WeatherTimeTravelFrame CaptureCurrent()
        {
            var est = new AtmosphereCompositionEstimator();
            WeatherPhysicsManifold weatherManifold = null;
            SceneServiceLookup.TryResolve("weather.physicsManifold", out weatherManifold);
            var frame = new WeatherTimeTravelFrame
            {
                narrativeTime = ResolveNarrativeTimeSeconds(),
                atmosphereSnapshot = planet != null ? est.Estimate(planet, weatherManifold) : null,
                roadWearSnapshot = CaptureRoadWear()
            };
            if (weatherManifold != null)
            {
                var bundle = CaptureManifoldDiff(weatherManifold);
                frame.sparseManifoldDiff = ManifoldDiffCodec.Encode(bundle);
            }
            return frame;
        }

        static float ResolveNarrativeTimeSeconds()
        {
            var clockType = Type.GetType("Locomotion.Narrative.NarrativeClock, Locomotion.Narrative.Runtime");
            if (clockType == null)
                return Time.time;
            var clock = UnityEngine.Object.FindAnyObjectByType(clockType) as MonoBehaviour;
            if (clock == null)
                return Time.time;
            var nowProp = clockType.GetProperty("Now");
            if (nowProp == null)
                return Time.time;
            object now = nowProp.GetValue(clock);
            if (now == null)
                return Time.time;
            var mathType = Type.GetType("Locomotion.Narrative.NarrativeCalendarMath, Locomotion.Narrative.Runtime");
            if (mathType == null)
                return Time.time;
            var method = mathType.GetMethod("DateTimeToSeconds", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                return Time.time;
            return (float)method.Invoke(null, new[] { now });
        }

        static ManifoldDiffBundle CaptureManifoldDiff(WeatherPhysicsManifold manifold)
        {
            if (manifold == null)
                return null;
            var entries = new List<ManifoldDiffEntry>();
            var bounds = manifold.worldBounds;
            float step = Mathf.Max(manifold.cellResolution * 4f, 1f);
            for (float x = bounds.min.x; x <= bounds.max.x; x += step)
            for (float y = bounds.min.y; y <= bounds.max.y; y += step)
            for (float z = bounds.min.z; z <= bounds.max.z; z += step)
            {
                var pos = new Vector3(x, y, z);
                entries.Add(new ManifoldDiffEntry
                {
                    position = pos,
                    data = manifold.GetDataAtPosition(pos)
                });
            }
            if (entries.Count == 0)
                return null;
            return new ManifoldDiffBundle { entries = entries.ToArray() };
        }

        static RoadWearSnapshotDto CaptureRoadWear()
        {
            var integrations = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < integrations.Length; i++)
            {
                var mb = integrations[i];
                if (mb == null || mb.GetType().Name != "RoadWeatherIntegration")
                    continue;
                var method = mb.GetType().GetMethod("CaptureWearSnapshot");
                if (method == null)
                    continue;
                return method.Invoke(mb, null) as RoadWearSnapshotDto;
            }
            return null;
        }

        void ApplyFrame(WeatherTimeTravelFrame frame)
        {
            if (frame == null || planet == null)
                return;

            WeatherPhysicsManifold manifold = null;
            SceneServiceLookup.TryResolve("weather.physicsManifold", out manifold);
            if (manifold != null && frame.sparseManifoldDiff != null && frame.sparseManifoldDiff.Length > 0)
            {
                ManifoldDiffBundle bundle = ManifoldDiffCodec.Decode(frame.sparseManifoldDiff);
                ManifoldDiffCodec.ApplyToManifold(bundle, manifold);
            }

            if (frame.atmosphereSnapshot != null && planet.interiorUpdater != null)
                planet.interiorUpdater.ApplyAtmosphereSnapshot(frame.atmosphereSnapshot);

            ApplyRoadWearSlice(frame.roadWearSnapshot);
        }

        void ApplyRoadWearSlice(RoadWearSnapshotDto wear)
        {
            if (wear == null)
                return;
            var integrations = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in integrations)
            {
                if (mb == null || mb.GetType().Name != "RoadWeatherIntegration")
                    continue;
                mb.SendMessage("RestoreWearFromTimeTravelFrame", new WeatherTimeTravelFrame { roadWearSnapshot = wear }, SendMessageOptions.DontRequireReceiver);
                break;
            }
        }

        void SaveFrameToDisk(WeatherTimeTravelFrame frame, int index)
        {
            Directory.CreateDirectory(CacheDir);
            string path = Path.Combine(CacheDir, $"frame_{index}.json");
            File.WriteAllText(path, WeatherTimeTravelFrameSerializer.ToJson(frame));
        }

        WeatherTimeTravelFrame LoadNearestFrame(float targetTime)
        {
            if (!Directory.Exists(CacheDir))
                return null;
            var files = Directory.GetFiles(CacheDir, "frame_*.json");
            if (files.Length == 0)
                return null;
            WeatherTimeTravelFrame best = null;
            float bestTime = float.MinValue;
            for (int i = 0; i < files.Length; i++)
            {
                string json = File.ReadAllText(files[i]);
                var frame = WeatherTimeTravelFrameSerializer.FromJson(json);
                if (frame == null || frame.narrativeTime > targetTime)
                    continue;
                if (frame.narrativeTime >= bestTime)
                {
                    bestTime = frame.narrativeTime;
                    best = frame;
                }
            }
            return best ?? new WeatherTimeTravelFrame { narrativeTime = targetTime };
        }

        public WeatherTimeTravelFrame LoadNearestFramePublic(float targetTime) => LoadNearestFrame(targetTime);
    }
}
