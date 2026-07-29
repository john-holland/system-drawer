using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SdfMax;
using UnityEngine;

/// <summary>
/// Compact sexpr printer/parser for hairdo SDF Max graphs (hair subset only).
/// </summary>
public static class HairdoSdfSexpr
{
    public static string Format(SdfMaxCompositionAsset asset, IList<string> sectionComments = null)
    {
        if (asset?.nodes == null || asset.nodes.Count == 0)
            return ";; empty hairdo sdf\n";

        int root = asset.ResolveRootIndex();
        var sb = new StringBuilder(4096);
        sb.AppendLine(";; hairdo sdf — regenerate from blend, or edit by hand then Apply Expression");
        if (sectionComments != null)
        {
            for (int i = 0; i < sectionComments.Count; i++)
                sb.AppendLine(sectionComments[i]);
        }

        FormatNode(sb, asset, root, 0);
        sb.AppendLine();
        return sb.ToString();
    }

    public static bool TryParse(string text, out SdfMaxCompositionAsset asset, out string error)
    {
        asset = null;
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Expression is empty.";
            return false;
        }

        try
        {
            var tokens = Tokenize(StripComments(text));
            int i = 0;
            var nodes = new List<SdfMaxNode>();
            int root = ParseExpr(tokens, ref i, nodes);
            if (i < tokens.Count)
            {
                error = $"Unexpected token near '{tokens[i]}'.";
                return false;
            }

            asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            asset.name = "HairdoSdf";
            asset.nodes = nodes;
            asset.rootNodeIndex = root;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    static void FormatNode(StringBuilder sb, SdfMaxCompositionAsset asset, int index, int indent)
    {
        if (index < 0 || index >= asset.nodes.Count)
        {
            Indent(sb, indent).AppendLine("(sphere r=0.01)");
            return;
        }

        var n = asset.nodes[index];
        Indent(sb, indent);
        switch (n.op)
        {
            case SdfMaxOp.Max:
                sb.AppendLine("(max");
                FormatNode(sb, asset, n.childIndexA, indent + 1);
                FormatNode(sb, asset, n.childIndexB, indent + 1);
                Indent(sb, indent).AppendLine(")");
                break;
            case SdfMaxOp.Min:
                sb.AppendLine("(min");
                FormatNode(sb, asset, n.childIndexA, indent + 1);
                FormatNode(sb, asset, n.childIndexB, indent + 1);
                Indent(sb, indent).AppendLine(")");
                break;
            case SdfMaxOp.Subtract:
                sb.AppendLine("(sub");
                FormatNode(sb, asset, n.childIndexA, indent + 1);
                FormatNode(sb, asset, n.childIndexB, indent + 1);
                Indent(sb, indent).AppendLine(")");
                break;
            case SdfMaxOp.SmoothMax:
                sb.Append("(smax ").Append(F(n.smoothRadius)).AppendLine();
                FormatNode(sb, asset, n.childIndexA, indent + 1);
                FormatNode(sb, asset, n.childIndexB, indent + 1);
                Indent(sb, indent).AppendLine(")");
                break;
            case SdfMaxOp.Add:
                sb.AppendLine("(add");
                FormatNode(sb, asset, n.childIndexA, indent + 1);
                FormatNode(sb, asset, n.childIndexB, indent + 1);
                Indent(sb, indent).AppendLine(")");
                break;
            case SdfMaxOp.PrimitiveLeaf:
                FormatPrimitive(sb, n);
                sb.AppendLine();
                break;
            default:
                sb.AppendLine($"(sphere r {F(Mathf.Max(0.01f, n.sphereRadius))} pos ( {F(n.localPosition.x)} {F(n.localPosition.y)} {F(n.localPosition.z)} ))");
                break;
        }
    }

    static void FormatPrimitive(StringBuilder sb, SdfMaxNode n)
    {
        string pos = $"pos ( {F(n.localPosition.x)} {F(n.localPosition.y)} {F(n.localPosition.z)} )";
        string rot = $"rot ( {F(n.localRotationEuler.x)} {F(n.localRotationEuler.y)} {F(n.localRotationEuler.z)} )";
        switch (n.primitiveType)
        {
            case SdfPrimitiveType.Sphere:
                sb.Append($"(sphere r {F(n.sphereRadius)} {pos})");
                break;
            case SdfPrimitiveType.Box:
                sb.Append(
                    $"(box he ( {F(n.halfExtents.x)} {F(n.halfExtents.y)} {F(n.halfExtents.z)} ) {pos} {rot})");
                break;
            case SdfPrimitiveType.Capsule:
                sb.Append($"(capsule r {F(Mathf.Max(n.radius, n.sphereRadius))} {pos} {rot})");
                break;
            case SdfPrimitiveType.DisplacedSphere:
                sb.Append(
                    $"(dsphere r {F(n.sphereRadius)} noiseF {F(n.noiseFrequency)} oct {n.noiseOctaves} persist {F(n.noisePersistence)} {pos})");
                break;
            case SdfPrimitiveType.Plane:
                sb.Append($"(plane {pos} {rot})");
                break;
            default:
                sb.Append($"(sphere r {F(Mathf.Max(0.01f, n.sphereRadius))} {pos})");
                break;
        }
    }

    static int ParseExpr(List<string> tokens, ref int i, List<SdfMaxNode> nodes)
    {
        Expect(tokens, ref i, "(");
        if (i >= tokens.Count)
            throw new Exception("Unexpected end of expression.");
        string head = tokens[i++];
        switch (head)
        {
            case "max":
            case "min":
            case "sub":
            case "add":
            {
                int a = ParseExpr(tokens, ref i, nodes);
                int b = ParseExpr(tokens, ref i, nodes);
                Expect(tokens, ref i, ")");
                var node = new SdfMaxNode
                {
                    op = head switch
                    {
                        "min" => SdfMaxOp.Min,
                        "sub" => SdfMaxOp.Subtract,
                        "add" => SdfMaxOp.Add,
                        _ => SdfMaxOp.Max
                    },
                    childIndexA = a,
                    childIndexB = b
                };
                nodes.Add(node);
                return nodes.Count - 1;
            }
            case "smax":
            {
                float k = ParseFloat(tokens, ref i);
                int a = ParseExpr(tokens, ref i, nodes);
                int b = ParseExpr(tokens, ref i, nodes);
                Expect(tokens, ref i, ")");
                nodes.Add(new SdfMaxNode
                {
                    op = SdfMaxOp.SmoothMax,
                    smoothRadius = k,
                    childIndexA = a,
                    childIndexB = b
                });
                return nodes.Count - 1;
            }
            case "sphere":
            case "box":
            case "capsule":
            case "dsphere":
            case "plane":
            {
                var node = new SdfMaxNode { op = SdfMaxOp.PrimitiveLeaf };
                node.primitiveType = head switch
                {
                    "box" => SdfPrimitiveType.Box,
                    "capsule" => SdfPrimitiveType.Capsule,
                    "dsphere" => SdfPrimitiveType.DisplacedSphere,
                    "plane" => SdfPrimitiveType.Plane,
                    _ => SdfPrimitiveType.Sphere
                };
                while (i < tokens.Count && tokens[i] != ")")
                    ParseAttr(tokens, ref i, node);
                Expect(tokens, ref i, ")");
                if (node.radius <= 0f && node.sphereRadius > 0f)
                    node.radius = node.sphereRadius;
                if (node.sphereRadius <= 0f && node.radius > 0f)
                    node.sphereRadius = node.radius;
                nodes.Add(node);
                return nodes.Count - 1;
            }
            default:
                throw new Exception($"Unknown form '{head}'.");
        }
    }

    static void ParseAttr(List<string> tokens, ref int i, SdfMaxNode node)
    {
        string key = tokens[i++];
        if (key.Contains("="))
        {
            int eq = key.IndexOf('=');
            string k = key.Substring(0, eq);
            string v = key.Substring(eq + 1);
            ApplyAttr(node, k, v, tokens, ref i);
            return;
        }

        // key then value tokens: r 0.1  OR  pos ( 0 1 0 )
        if (i >= tokens.Count)
            throw new Exception($"Missing value for '{key}'.");
        ApplyAttr(node, key, null, tokens, ref i);
    }

    static void ApplyAttr(SdfMaxNode node, string key, string inlineValue, List<string> tokens, ref int i)
    {
        switch (key)
        {
            case "r":
                node.sphereRadius = node.radius = inlineValue != null
                    ? ParseFloatToken(inlineValue)
                    : ParseFloat(tokens, ref i);
                break;
            case "noiseF":
                node.noiseFrequency = inlineValue != null
                    ? ParseFloatToken(inlineValue)
                    : ParseFloat(tokens, ref i);
                break;
            case "oct":
                node.noiseOctaves = Mathf.RoundToInt(inlineValue != null
                    ? ParseFloatToken(inlineValue)
                    : ParseFloat(tokens, ref i));
                break;
            case "persist":
                node.noisePersistence = inlineValue != null
                    ? ParseFloatToken(inlineValue)
                    : ParseFloat(tokens, ref i);
                break;
            case "he":
                node.halfExtents = ParseVec3(inlineValue, tokens, ref i);
                break;
            case "pos":
                node.localPosition = ParseVec3(inlineValue, tokens, ref i);
                break;
            case "rot":
                node.localRotationEuler = ParseVec3(inlineValue, tokens, ref i);
                break;
            default:
                // skip unknown attr value
                if (inlineValue == null)
                {
                    if (i < tokens.Count && tokens[i] == "(")
                        ParseVec3(null, tokens, ref i);
                    else if (i < tokens.Count)
                        i++;
                }
                break;
        }
    }

    static Vector3 ParseVec3(string inline, List<string> tokens, ref int i)
    {
        if (!string.IsNullOrEmpty(inline) && inline.StartsWith("("))
        {
            // rare: pos=(0 — incomplete; fall through to tokens
        }

        Expect(tokens, ref i, "(");
        float x = ParseFloat(tokens, ref i);
        float y = ParseFloat(tokens, ref i);
        float z = ParseFloat(tokens, ref i);
        Expect(tokens, ref i, ")");
        return new Vector3(x, y, z);
    }

    static float ParseFloat(List<string> tokens, ref int i)
    {
        if (i >= tokens.Count)
            throw new Exception("Expected number.");
        return ParseFloatToken(tokens[i++]);
    }

    static float ParseFloatToken(string t) =>
        float.Parse(t, CultureInfo.InvariantCulture);

    static void Expect(List<string> tokens, ref int i, string want)
    {
        if (i >= tokens.Count || tokens[i] != want)
            throw new Exception($"Expected '{want}'.");
        i++;
    }

    static string StripComments(string text)
    {
        var sb = new StringBuilder(text.Length);
        var lines = text.Split('\n');
        for (int li = 0; li < lines.Length; li++)
        {
            string line = lines[li];
            int c = line.IndexOf(";;", StringComparison.Ordinal);
            if (c >= 0)
                line = line.Substring(0, c);
            sb.AppendLine(line);
        }
        return sb.ToString();
    }

    static List<string> Tokenize(string text)
    {
        var list = new List<string>();
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '(' || c == ')')
            {
                list.Add(c.ToString());
                i++;
                continue;
            }

            int start = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '(' && text[i] != ')')
                i++;
            list.Add(text.Substring(start, i - start));
        }

        return list;
    }

    static StringBuilder Indent(StringBuilder sb, int indent)
    {
        for (int i = 0; i < indent; i++)
            sb.Append("  ");
        return sb;
    }

    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
