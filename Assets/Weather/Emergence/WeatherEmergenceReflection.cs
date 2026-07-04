using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Weather.Emergence
{
    /// <summary>Optional reflection bridge to travel, quest, and 4D types without assembly cycles.</summary>
    internal static class WeatherEmergenceReflection
    {
        static bool _resolved;
        static Type _travelAgentType;
        static Type _registryType;
        static Type _adjusterType;
        static Type _questRunnerType;
        static Type _spatial4DType;
        static PropertyInfo _registryAll;
        static MethodInfo _buildPolyline;
        static PropertyInfo _questActiveObjective;
        static FieldInfo _objTravelBinding;
        static FieldInfo _objMapLayer;
        static FieldInfo _spatialEmergenceCount;

        static void Resolve()
        {
            if (_resolved)
                return;
            _resolved = true;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                _travelAgentType ??= asm.GetType("TravelAgent");
                _registryType ??= asm.GetType("TravelAgentRegistry");
                _adjusterType ??= asm.GetType("TravelMultibodyPathAdjuster");
                _questRunnerType ??= asm.GetType("Locomotion.Narrative.QuestRunner");
                _spatial4DType ??= asm.GetType("SpatialGenerator4D");
            }

            if (_registryType != null)
                _registryAll = _registryType.GetProperty("All", BindingFlags.Public | BindingFlags.Static);

            if (_adjusterType != null)
                _buildPolyline = _adjusterType.GetMethod("BuildEffectivePolyline", BindingFlags.Public | BindingFlags.Static);

            if (_questRunnerType != null)
            {
                _questActiveObjective = _questRunnerType.GetProperty("ActiveObjective", BindingFlags.Public | BindingFlags.Instance);
                Type objType = _questRunnerType.Assembly.GetType("Locomotion.Narrative.QuestObjectiveView");
                if (objType != null)
                {
                    _objTravelBinding = objType.GetField("travelBinding");
                    _objMapLayer = objType.GetField("mapLayer");
                }
            }

            if (_spatial4DType != null)
                _spatialEmergenceCount = _spatial4DType.GetField("emergenceLayerCount");
        }

        public static void CollectTravel(List<EmergenceVector> into, float previewWeight = 0.45f)
        {
            Resolve();
            if (_registryAll == null || _buildPolyline == null || _travelAgentType == null)
                return;

            var agents = _registryAll.GetValue(null) as System.Collections.IEnumerable;
            if (agents == null)
                return;

            foreach (object agent in agents)
            {
                if (agent == null)
                    continue;
                var poly = _buildPolyline.Invoke(null, new[] { agent }) as IList<Vector3>;
                if (poly == null || poly.Count < 2)
                    continue;

                float radius = 4f;
                var mbField = agent.GetType().GetField("multibody");
                if (mbField?.GetValue(agent) != null)
                {
                    var clearanceProp = mbField.FieldType.GetField("clearanceRadius");
                    if (clearanceProp != null)
                        radius = Mathf.Max(radius, (float)clearanceProp.GetValue(mbField.GetValue(agent)));
                }

                bool hasPlan = poly.Count > 2;
                float w = hasPlan ? 1f : previewWeight;
                for (int i = 0; i < poly.Count - 1; i++)
                {
                    into.Add(EmergenceVector.Segment(
                        poly[i], poly[i + 1], radius, w,
                        $"travel:{agent.GetHashCode()}:{i}"));
                }
            }
        }

        public static void CollectQuest(List<EmergenceVector> into, float boostWeight = 1.25f)
        {
            Resolve();
            if (_questRunnerType == null || _questActiveObjective == null)
                return;

            var runner = UnityEngine.Object.FindAnyObjectByType(_questRunnerType);
            if (runner == null)
                return;

            object objective = _questActiveObjective.GetValue(runner);
            if (objective == null)
                return;

            string mapLayer = _objMapLayer?.GetValue(objective) as string;
            if (mapLayer != null && mapLayer != "emergence" && mapLayer != "composite")
                return;

            string binding = _objTravelBinding?.GetValue(objective) as string;
            Vector3 origin = runner is Component c ? c.transform.position : Vector3.zero;
            float radius = 12f;
            into.Add(EmergenceVector.Point(origin, radius, boostWeight, $"quest:{binding ?? "active"}"));
        }

        public static void CollectSpatial4D(List<EmergenceVector> into, Transform focus, float weight = 0.8f)
        {
            Resolve();
            if (_spatial4DType == null || focus == null)
                return;

            var sg = UnityEngine.Object.FindAnyObjectByType(_spatial4DType);
            if (sg == null)
                return;

            int layers = 8;
            if (_spatialEmergenceCount != null)
                layers = (int)_spatialEmergenceCount.GetValue(sg);

            Vector3 pos = focus.position;
            Vector3 dir = Vector3.up;
            var boundsField = _spatial4DType.GetField("spatialBounds");
            if (boundsField?.GetValue(sg) is Bounds b)
            {
                Vector3 local = pos - b.center;
                dir = new Vector3(local.x, layers * 0.05f, local.z).normalized;
            }

            float radius = 20f + layers * 2f;
            into.Add(new EmergenceVector
            {
                origin = pos,
                direction = dir,
                length = radius * 0.5f,
                influenceRadius = radius,
                weight = weight,
                sourceId = "spatial4d",
            });
        }
    }
}
