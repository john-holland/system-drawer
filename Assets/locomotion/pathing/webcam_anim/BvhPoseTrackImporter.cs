using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>Parse a BVH hierarchy + motion into a PoseTrack. Joint names stay source names until the fitter remaps them.</summary>
public static class BvhPoseTrackImporter
{
    public sealed class Joint
    {
        public string name;
        public int parent = -1;
        public Vector3 offset;
        public readonly List<string> channels = new List<string>();
        public int channelOffset;
    }

    public static PoseTrack FromFile(string path, string modelSpec = "")
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return new PoseTrack { modelSpec = modelSpec ?? "" };
        return FromText(File.ReadAllText(path), modelSpec);
    }

    public static PoseTrack FromText(string text, string modelSpec = "")
    {
        var track = new PoseTrack { modelSpec = modelSpec ?? "" };
        if (string.IsNullOrEmpty(text))
            return track;

        var tokens = Tokenize(text);
        int i = 0;
        var joints = new List<Joint>();
        if (!TryParseHierarchy(tokens, ref i, joints))
            return track;

        float frameTime = 1f / 30f;
        int frameCount = 0;
        if (!TryParseMotionHeader(tokens, ref i, out frameCount, out frameTime))
            return track;

        int channelCount = 0;
        for (int j = 0; j < joints.Count; j++)
            channelCount += joints[j].channels.Count;

        for (int f = 0; f < frameCount; f++)
        {
            var values = new float[channelCount];
            for (int c = 0; c < channelCount; c++)
            {
                if (i >= tokens.Count || !TryParseFloat(tokens[i], out values[c]))
                    return track;
                i++;
            }

            float timeMs = f * frameTime * 1000f;
            for (int j = 0; j < joints.Count; j++)
            {
                SampleJoint(joints[j], values, timeMs, track);
            }
        }

        return track;
    }

    public static void CollectJoints(string text, List<Joint> dest)
    {
        if (dest == null || string.IsNullOrEmpty(text))
            return;
        var tokens = Tokenize(text);
        int i = 0;
        TryParseHierarchy(tokens, ref i, dest);
    }

    static void SampleJoint(Joint joint, float[] values, float timeMs, PoseTrack track)
    {
        Vector3 pos = joint.offset;
        Vector3 euler = Vector3.zero;
        int off = joint.channelOffset;
        for (int c = 0; c < joint.channels.Count; c++)
        {
            float v = values[off + c];
            switch (joint.channels[c])
            {
                case "Xposition": pos.x = v; break;
                case "Yposition": pos.y = v; break;
                case "Zposition": pos.z = v; break;
                case "Xrotation": euler.x = v; break;
                case "Yrotation": euler.y = v; break;
                case "Zrotation": euler.z = v; break;
            }
        }

        track.samples.Add(new PoseBoneSample
        {
            traitId = joint.name,
            timeMs = timeMs,
            localPosition = pos,
            localRotation = Quaternion.Euler(euler)
        });
    }

    static bool TryParseHierarchy(List<string> tokens, ref int i, List<Joint> joints)
    {
        if (!Match(tokens, ref i, "HIERARCHY"))
            return false;
        var stack = new List<int>();
        while (i < tokens.Count)
        {
            string t = tokens[i];
            if (t == "MOTION")
                return true;
            if (t == "ROOT" || t == "JOINT")
            {
                i++;
                if (i >= tokens.Count)
                    return false;
                var joint = new Joint { name = tokens[i], parent = stack.Count > 0 ? stack[stack.Count - 1] : -1 };
                i++;
                if (!Match(tokens, ref i, "{"))
                    return false;
                if (!Match(tokens, ref i, "OFFSET"))
                    return false;
                if (!TryReadVec3(tokens, ref i, out joint.offset))
                    return false;
                if (Match(tokens, ref i, "CHANNELS"))
                {
                    if (i >= tokens.Count || !int.TryParse(tokens[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                        return false;
                    i++;
                    for (int c = 0; c < n && i < tokens.Count; c++)
                    {
                        joint.channels.Add(tokens[i]);
                        i++;
                    }
                }
                joint.channelOffset = 0;
                for (int p = 0; p < joints.Count; p++)
                    joint.channelOffset += joints[p].channels.Count;
                joints.Add(joint);
                stack.Add(joints.Count - 1);
                continue;
            }
            if (t == "End")
            {
                i++;
                if (Match(tokens, ref i, "Site") && Match(tokens, ref i, "{"))
                {
                    while (i < tokens.Count && tokens[i] != "}")
                        i++;
                    if (i < tokens.Count)
                        i++;
                }
                continue;
            }
            if (t == "}")
            {
                i++;
                if (stack.Count > 0)
                    stack.RemoveAt(stack.Count - 1);
                continue;
            }
            i++;
        }
        return joints.Count > 0;
    }

    static bool TryParseMotionHeader(List<string> tokens, ref int i, out int frames, out float frameTime)
    {
        frames = 0;
        frameTime = 1f / 30f;
        if (!Match(tokens, ref i, "MOTION"))
            return false;
        if (!Match(tokens, ref i, "Frames:"))
            return false;
        if (i >= tokens.Count || !int.TryParse(tokens[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out frames))
            return false;
        i++;
        if (i + 1 < tokens.Count && tokens[i] == "Frame" && tokens[i + 1] == "Time:")
            i += 2;
        else if (!Match(tokens, ref i, "Frame") || !Match(tokens, ref i, "Time:"))
            return false;
        if (i >= tokens.Count || !TryParseFloat(tokens[i], out frameTime))
            return false;
        i++;
        return frames >= 0;
    }

    static bool TryReadVec3(List<string> tokens, ref int i, out Vector3 v)
    {
        v = Vector3.zero;
        if (i + 2 >= tokens.Count)
            return false;
        if (!TryParseFloat(tokens[i], out v.x) || !TryParseFloat(tokens[i + 1], out v.y) || !TryParseFloat(tokens[i + 2], out v.z))
            return false;
        i += 3;
        return true;
    }

    static bool Match(List<string> tokens, ref int i, string expected)
    {
        if (i >= tokens.Count || !string.Equals(tokens[i], expected, StringComparison.OrdinalIgnoreCase))
            return false;
        i++;
        return true;
    }

    static bool TryParseFloat(string s, out float v) =>
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    static List<string> Tokenize(string text)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
            {
                Flush(sb, list);
                continue;
            }
            if (c == '{' || c == '}')
            {
                Flush(sb, list);
                list.Add(c.ToString());
                continue;
            }
            sb.Append(c);
        }
        Flush(sb, list);
        return list;
    }

    static void Flush(StringBuilder sb, List<string> list)
    {
        if (sb.Length == 0)
            return;
        list.Add(sb.ToString());
        sb.Length = 0;
    }
}
