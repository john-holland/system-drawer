using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    public enum NarrativeValueType
    {
        None,
        Bool,
        Int,
        Float,
        String,
        Vector3,
        ObjectKey
    }

    /// <summary>
    /// Small serializable value container for persistence (JSON/YAML) and editor editing.
    /// </summary>
    [Serializable]
    public struct NarrativeValue
    {
        public NarrativeValueType type;
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;
        public Vector3 vector3Value;

        /// <summary>
        /// For values that are references, this is a key resolved by NarrativeBindings at runtime.
        /// </summary>
        public string objectKey;

        public override string ToString()
        {
            return type switch
            {
                NarrativeValueType.Bool => boolValue.ToString(),
                NarrativeValueType.Int => intValue.ToString(),
                NarrativeValueType.Float => floatValue.ToString("0.###"),
                NarrativeValueType.String => stringValue ?? "",
                NarrativeValueType.Vector3 => vector3Value.ToString("0.###"),
                NarrativeValueType.ObjectKey => objectKey ?? "",
                _ => ""
            };
        }

        public static NarrativeValue FromObject(object value)
        {
            if (value == null)
                return new NarrativeValue { type = NarrativeValueType.None };
            if (value is bool b)
                return new NarrativeValue { type = NarrativeValueType.Bool, boolValue = b };
            if (value is int i)
                return new NarrativeValue { type = NarrativeValueType.Int, intValue = i };
            if (value is float f)
                return new NarrativeValue { type = NarrativeValueType.Float, floatValue = f };
            if (value is double d)
                return new NarrativeValue { type = NarrativeValueType.Float, floatValue = (float)d };
            if (value is string s)
                return new NarrativeValue { type = NarrativeValueType.String, stringValue = s };
            if (value is Vector3 v)
                return new NarrativeValue { type = NarrativeValueType.Vector3, vector3Value = v };
            return new NarrativeValue { type = NarrativeValueType.String, stringValue = value.ToString() };
        }
    }
}

