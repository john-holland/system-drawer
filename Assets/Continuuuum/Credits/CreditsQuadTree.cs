using System.Collections.Generic;
using UnityEngine;

namespace Continuuuum.Credits
{
    /// <summary>Screen-space quad tree for credits Canvas layout (path ids like R.0.2).</summary>
    public sealed class CreditsQuadTree
    {
        public sealed class Node
        {
            public string pathId;
            public Rect rect;
            public Node[] children;
            public bool IsLeaf => children == null || children.Length == 0;
            public CreditsSectionDto section;
            public bool specialUi;
        }

        public Node Root { get; private set; }

        public CreditsQuadTree(Rect rootRect)
        {
            Root = new Node { pathId = "R", rect = rootRect };
        }

        public Node EnsurePath(string pathId)
        {
            if (string.IsNullOrEmpty(pathId) || pathId == "R")
                return Root;

            string[] parts = pathId.Split('.');
            Node cur = Root;
            string built = "R";
            for (int i = 1; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out int qi))
                    qi = 0;
                qi = Mathf.Clamp(qi, 0, 3);
                if (cur.IsLeaf)
                    Subdivide(cur);
                built += "." + qi;
                cur = cur.children[qi];
                cur.pathId = built;
            }
            return cur;
        }

        public static void Subdivide(Node node)
        {
            if (!node.IsLeaf)
                return;
            var r = node.rect;
            float hw = r.width * 0.5f;
            float hh = r.height * 0.5f;
            // 0 UpperRight, 1 TopLeft, 2 BottomLeft, 3 LowerRight (SGQuadTree order)
            node.children = new[]
            {
                new Node { pathId = node.pathId + ".0", rect = new Rect(r.x + hw, r.y + hh, hw, hh) },
                new Node { pathId = node.pathId + ".1", rect = new Rect(r.x, r.y + hh, hw, hh) },
                new Node { pathId = node.pathId + ".2", rect = new Rect(r.x, r.y, hw, hh) },
                new Node { pathId = node.pathId + ".3", rect = new Rect(r.x + hw, r.y, hw, hh) },
            };
        }

        public IEnumerable<Node> Leaves()
        {
            var stack = new Stack<Node>();
            stack.Push(Root);
            while (stack.Count > 0)
            {
                var n = stack.Pop();
                if (n.IsLeaf)
                    yield return n;
                else
                    for (int i = 0; i < n.children.Length; i++)
                        stack.Push(n.children[i]);
            }
        }
    }
}
