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
    }
}

