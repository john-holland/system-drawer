using System;
using UnityEngine;

namespace Weather.Executor
{
    [Serializable]
    public sealed class WeatherEggClientPayload
    {
        public string clientId;
        public int frameIndex;
        public Vector3 eggCenter;
        public Vector3 eggRadii;
        public float confidence;
        public byte[] regressionPayload;
        public byte[] sparseDiffPayload;
        public float residualVariance;
        public int timeoutOrder;
    }

    [Serializable]
    public sealed class WeatherEggApplyPayload
    {
        public int frameIndex;
        public string authorityClientId;
        public Vector3 eggCenter;
        public Vector3 eggRadii;
        public byte[] regressionPayload;
        public byte[] sparseDiffPayload;
        public float definitionLevel = 1f;
    }

    [Serializable]
    public sealed class WeatherEggBootstrapPayload
    {
        public float latDeg;
        public float lonDeg;
        public string weatherFrameJson;
        public Vector3 suggestedCenter;
        public Vector3 suggestedRadii;
    }

    public static class WeatherEggPayloadSerializer
    {
        public static string ToJson<T>(T payload) where T : class =>
            payload == null ? "" : JsonUtility.ToJson(payload);

        public static T FromJson<T>(string json) where T : class =>
            string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<T>(json);
    }
}
