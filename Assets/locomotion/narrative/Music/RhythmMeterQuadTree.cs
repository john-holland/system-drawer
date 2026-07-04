using System;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    /// <summary>2D quad-tree walk over beat grid (X) and meter stress (Y) for rhythm templates.</summary>
    public sealed class RhythmMeterQuadTree
    {
        public enum Quadrant
        {
            UpperRight,
            TopLeft,
            BottomLeft,
            LowerRight
        }

        sealed class Node
        {
            public Rect bounds;
            public RhythmMeterTemplate template;
            public Node[] children;
            public bool isLeaf = true;
            public string pathId = "R";
        }

        readonly Node _root;
        readonly int _maxDepth;
        readonly System.Random _rng;

        public RhythmMeterQuadTree(int maxDepth = 6, int seed = 0)
        {
            _maxDepth = maxDepth;
            _root = new Node { bounds = new Rect(0f, 0f, 1f, 1f) };
            _rng = new System.Random(seed);
            Build(_root, 0);
        }

        void Build(Node node, int depth)
        {
            if (depth >= _maxDepth)
            {
                node.template = TemplateFromLeaf(node.pathId, node.bounds);
                return;
            }

            node.isLeaf = false;
            node.children = new Node[4];
            float hx = node.bounds.width * 0.5f;
            float hy = node.bounds.height * 0.5f;
            float x = node.bounds.x;
            float y = node.bounds.y;

            node.children[0] = MakeChild(new Rect(x + hx, y + hy, hx, hy), node.pathId + ".0", depth);
            node.children[1] = MakeChild(new Rect(x, y + hy, hx, hy), node.pathId + ".1", depth);
            node.children[2] = MakeChild(new Rect(x, y, hx, hy), node.pathId + ".2", depth);
            node.children[3] = MakeChild(new Rect(x + hx, y, hx, hy), node.pathId + ".3", depth);
        }

        Node MakeChild(Rect bounds, string pathId, int depth)
        {
            var child = new Node { bounds = bounds, pathId = pathId };
            Build(child, depth + 1);
            return child;
        }

        public RhythmMeterTemplate Walk(string causalityLeafId, int dayCollapseSeed, float beatBias, float stressBias,
            int dialogueFootOverride = 0)
        {
            int seed = StableHash(causalityLeafId) ^ dayCollapseSeed;
            var walkRng = new System.Random(seed);
            Node node = _root;

            while (!node.isLeaf)
            {
                float bx = (float)walkRng.NextDouble() * 0.5f + beatBias * 0.5f;
                float sy = (float)walkRng.NextDouble() * 0.5f + stressBias * 0.5f;
                bx = Mathf.Clamp01(bx);
                sy = Mathf.Clamp01(sy);

                int qi = PickQuadrant(node.bounds, bx, sy);
                node = node.children[qi];
            }

            RhythmMeterTemplate t = node.template.Clone();
            t.quadPathId = "Q" + (node.pathId.StartsWith("R.") ? node.pathId.Substring(2) : "0");

            if (dialogueFootOverride > 0)
            {
                t.feetPerLine = dialogueFootOverride;
                t.stressPattern = Mathf.Clamp01(stressBias);
            }

            return t;
        }

        static int PickQuadrant(Rect bounds, float normX, float normY)
        {
            float lx = bounds.x + normX * bounds.width;
            float ly = bounds.y + normY * bounds.height;
            float mx = bounds.x + bounds.width * 0.5f;
            float my = bounds.y + bounds.height * 0.5f;
            bool top = ly >= my;
            bool right = lx >= mx;
            if (top && right) return 0;
            if (top && !right) return 1;
            if (!top && !right) return 2;
            return 3;
        }

        static RhythmMeterTemplate TemplateFromLeaf(string pathId, Rect bounds)
        {
            int hash = StableHash(pathId);
            var t = new RhythmMeterTemplate
            {
                beatsPerBar = 2 + (hash % 5),
                beatSubdivision = 2 + ((hash >> 3) % 7),
                swingAmount = (hash & 7) / 14f,
                stressPattern = bounds.center.y,
                footTemplate = (PoeticFootTemplate)((hash >> 5) % 4),
                feetPerLine = 4 + ((hash >> 7) % 3),
                quantizationMs = 250 + (hash % 5) * 125
            };
            return t;
        }

        public static int StableHash(string s)
        {
            unchecked
            {
                int hash = 23;
                if (s != null)
                {
                    for (int i = 0; i < s.Length; i++)
                        hash = hash * 31 + s[i];
                }
                return hash;
            }
        }

        public static int FeetFromFareySpanDuration(float spanDurationSeconds, float bpm)
        {
            if (bpm <= 0f || spanDurationSeconds <= 0f) return 5;
            float beats = spanDurationSeconds * bpm / 60f;
            int feet = Mathf.RoundToInt(beats / 2f);
            return Mathf.Clamp(feet, 1, 12);
        }
    }
}
