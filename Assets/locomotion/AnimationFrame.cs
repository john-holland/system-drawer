using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single frame of animation data.
/// Contains bone transforms, root motion, and metadata.
/// </summary>
[System.Serializable]
public class AnimationFrame
{
    [Header("Frame Info")]
    [Tooltip("Frame index in animation")]
    public int frameIndex;

    [Tooltip("Time in seconds")]
    public float time;

    [Header("Bone Transforms")]
    [Tooltip("Bone transforms at this frame (bone name -> transform data)")]
    public Dictionary<string, TransformData> boneTransforms = new Dictionary<string, TransformData>();

    [Header("Root Motion")]
    [Tooltip("Root motion delta")]
    public Vector3 rootMotion = Vector3.zero;

    [Tooltip("Root rotation delta")]
    public Quaternion rootRotation = Quaternion.identity;

    [Header("Metadata")]
    [Tooltip("True if this frame requires tool usage")]
    public bool requiresTool = false;

    [Tooltip("Tool detected from animation analysis")]
    public GameObject detectedTool;

    [Tooltip("True if this frame was dropped/trimmed")]
    public bool isDropped = false;

    /// <summary>
    /// Get transform data for a specific bone.
    /// </summary>
    public TransformData GetBoneTransform(string boneName)
    {
        boneTransforms.TryGetValue(boneName, out TransformData transform);
        return transform;
    }

    /// <summary>
    /// Set transform data for a bone.
    /// </summary>
    public void SetBoneTransform(string boneName, TransformData transform)
    {
        boneTransforms[boneName] = transform;
    }

    /// <summary>
    /// Copy this frame to a new instance.
    /// </summary>
    public AnimationFrame Copy()
    {
        AnimationFrame copy = new AnimationFrame
        {
            frameIndex = this.frameIndex,
            time = this.time,
            rootMotion = this.rootMotion,
            rootRotation = this.rootRotation,
            requiresTool = this.requiresTool,
            detectedTool = this.detectedTool,
            isDropped = this.isDropped
        };

        // Deep copy bone transforms
        copy.boneTransforms = new Dictionary<string, TransformData>();
        foreach (var kvp in this.boneTransforms)
        {
            copy.boneTransforms[kvp.Key] = kvp.Value; // TransformData is a struct, so this is a copy
        }

        return copy;
    }
}

/// <summary>
/// Transform data for a bone at a specific frame.
/// </summary>
[System.Serializable]
public struct TransformData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public TransformData(Vector3 pos, Quaternion rot, Vector3 scl)
    {
        position = pos;
        rotation = rot;
        scale = scl;
    }

    public TransformData(Transform transform)
    {
        position = transform.localPosition;
        rotation = transform.localRotation;
        scale = transform.localScale;
    }
}
