using System;
using UnityEngine;

/// <summary>
/// Optional per-dimension open/close topology BT reference for transformers/convertibles.
/// Topology asset is a ScriptableObject (typically OpenCloseTopologyAsset) resolved via runner.
/// </summary>
[Serializable]
public sealed class DimensionalOpenCloseBtEntry
{
    public int dimIndex;
    public ScriptableObject topology;
    /// <summary>-1 = asset/BT default; &gt;=0 force sequence length in milliseconds.</summary>
    public int runtimeMilliseconds = -1;
    public bool runOnEnter = true;
    public bool runOnExit = false;
}
