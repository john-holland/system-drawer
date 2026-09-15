using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>Thin SVG path → bolt-UV polylines (M/L/H/V/Z). No Unity SVG importer.</summary>
public static class ClothSvgPathParser
{
    public static List<Vector2> ParsePolylines(string svg, float widthM, float lengthM)
    {
        var pts = new List<Vector2>();
        if (string.IsNullOrWhiteSpace(svg)) return pts;
        float w = Mathf.Max(0.01f, widthM);
        float h = Mathf.Max(0.01f, lengthM);
        var tokens = Tokenize(svg);
        float cx = 0f, cy = 0f, sx = 0f, sy = 0f;
        char cmd = 'M';
        int i = 0;
        float minX = 0f, minY = 0f, maxX = 1f, maxY = 1f;
        bool bounds = false;
        var raw = new List<Vector2>();
        while (i < tokens.Count)
        {
            string t = tokens[i];
            if (t.Length == 1 && char.IsLetter(t[0]))
            {
                cmd = t[0];
                i++;
                continue;
            }
            if (!TryNum(t, out float n)) { i++; continue; }
            switch (char.ToUpperInvariant(cmd))
            {
                case 'M':
                case 'L':
                    if (i + 1 >= tokens.Count) { i++; break; }
                    float x = n;
                    if (!TryNum(tokens[i + 1], out float y)) { i++; break; }
                    if (char.IsLower(cmd)) { x += cx; y += cy; }
                    cx = x; cy = y;
                    if (char.ToUpperInvariant(cmd) == 'M') { sx = cx; sy = cy; }
                    raw.Add(new Vector2(cx, cy));
                    Expand(ref bounds, ref minX, ref minY, ref maxX, ref maxY, cx, cy);
                    i += 2;
                    cmd = char.IsLower(cmd) ? 'l' : 'L';
                    break;
                case 'H':
                    cx = char.IsLower(cmd) ? cx + n : n;
                    raw.Add(new Vector2(cx, cy));
                    Expand(ref bounds, ref minX, ref minY, ref maxX, ref maxY, cx, cy);
                    i++;
                    break;
                case 'V':
                    cy = char.IsLower(cmd) ? cy + n : n;
                    raw.Add(new Vector2(cx, cy));
                    Expand(ref bounds, ref minX, ref minY, ref maxX, ref maxY, cx, cy);
                    i++;
                    break;
                case 'Z':
                    raw.Add(new Vector2(sx, sy));
                    cx = sx; cy = sy;
                    i++;
                    break;
                default:
                    i++;
                    break;
            }
        }
        float sxn = maxX - minX;
        float syn = maxY - minY;
        if (sxn < 1e-6f) sxn = 1f;
        if (syn < 1e-6f) syn = 1f;
        for (int p = 0; p < raw.Count; p++)
        {
            var r = raw[p];
            pts.Add(new Vector2((r.x - minX) / sxn * w, (r.y - minY) / syn * h));
        }
        return pts;
    }

    public static ClothSplinePath ToCutPath(string svg, float widthM, float lengthM)
    {
        var path = new ClothSplinePath { kind = ClothSplineKind.Cut, pathId = "svg_cut" };
        var pts = ParsePolylines(svg, widthM, lengthM);
        for (int i = 0; i < pts.Count; i++)
            path.controlPoints.Add(new Vector3(pts[i].x, 0f, pts[i].y));
        return path;
    }

    static void Expand(ref bool any, ref float minX, ref float minY, ref float maxX, ref float maxY, float x, float y)
    {
        if (!any)
        {
            minX = maxX = x;
            minY = maxY = y;
            any = true;
            return;
        }
        if (x < minX) minX = x;
        if (y < minY) minY = y;
        if (x > maxX) maxX = x;
        if (y > maxY) maxY = y;
    }

    static bool TryNum(string t, out float n)
        => float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out n);

    static List<string> Tokenize(string svg)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        for (int i = 0; i < svg.Length; i++)
        {
            char c = svg[i];
            if (char.IsLetter(c))
            {
                Flush(sb, list);
                list.Add(c.ToString());
            }
            else if (c == ',' || char.IsWhiteSpace(c))
                Flush(sb, list);
            else
                sb.Append(c);
        }
        Flush(sb, list);
        return list;
    }

    static void Flush(StringBuilder sb, List<string> list)
    {
        if (sb.Length == 0) return;
        list.Add(sb.ToString());
        sb.Length = 0;
    }
}
