#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Locomotion.Musculature;

namespace Locomotion.EditorTools
{
    [Serializable]
    public sealed class RagdollComponentLeftoverEntry
    {
        public string hierarchyPath;
        public List<string> componentTypes = new List<string>();
    }

    [Serializable]
    public sealed class RagdollComponentLeftoverMap
    {
        public List<RagdollComponentLeftoverEntry> entries = new List<RagdollComponentLeftoverEntry>();

        public string ToJson(bool pretty = true) => JsonUtility.ToJson(this, pretty);

        public void Clear()
        {
            if (entries == null) entries = new List<RagdollComponentLeftoverEntry>();
            else entries.Clear();
        }

        public string ToReadableText()
        {
            var sb = new StringBuilder();
            if (entries == null || entries.Count == 0)
            {
                sb.AppendLine("(none)");
                return sb.ToString();
            }
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.componentTypes == null || e.componentTypes.Count == 0) continue;
                sb.AppendLine(e.hierarchyPath);
                for (int t = 0; t < e.componentTypes.Count; t++)
                    sb.Append("  - ").AppendLine(e.componentTypes[t]);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Strips non-allowlisted components from a cloned ragdoll and records
    /// GameObject path → component types that did not make it into the revamped actor.
    /// </summary>
    public static class RagdollComponentStripper
    {
        static readonly HashSet<Type> KeepExact = new HashSet<Type>
        {
            typeof(Transform),
            typeof(Animator),
            typeof(SkinnedMeshRenderer),
            typeof(MeshRenderer),
            typeof(MeshFilter),
            typeof(LODGroup),
            typeof(Rigidbody),
            typeof(Cloth),
        };

        /// <summary>
        /// Inventory then destroy every non-allowlisted component under <paramref name="root"/>.
        /// Locomotion / System-Drawer scripts are stripped so AutoWire recreates clean defaults.
        /// </summary>
        public static RagdollComponentLeftoverMap StripAndCollectLeftovers(GameObject root)
        {
            var map = new RagdollComponentLeftoverMap();
            if (root == null) return map;

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                var t = transforms[i];
                if (t == null) continue;
                string path = HierarchyPath(root.transform, t);
                var comps = t.GetComponents<Component>();
                RagdollComponentLeftoverEntry entry = null;

                // Destroy in reverse so index stability does not matter.
                for (int c = comps.Length - 1; c >= 0; c--)
                {
                    var comp = comps[c];
                    if (comp == null) continue;
                    if (ShouldKeep(comp)) continue;

                    string typeName = comp.GetType().AssemblyQualifiedName ?? comp.GetType().FullName;
                    if (entry == null)
                    {
                        entry = new RagdollComponentLeftoverEntry { hierarchyPath = path };
                        map.entries.Add(entry);
                    }
                    entry.componentTypes.Add(typeName);
                    Undo.DestroyObjectImmediate(comp);
                }
            }

            return map;
        }

        public static bool ShouldKeep(Component comp)
        {
            if (comp == null) return true;
            Type type = comp.GetType();
            if (KeepExact.Contains(type)) return true;
            if (typeof(Joint).IsAssignableFrom(type)) return true;
            if (typeof(Collider).IsAssignableFrom(type)) return true;

            // Fingers / digits are not recreated by AutoWire — keep through replicate.
            if (typeof(RagdollFinger).IsAssignableFrom(type)) return true;
            if (typeof(RagdollDigit).IsAssignableFrom(type)) return true;
            if (typeof(RagdollNailbed).IsAssignableFrom(type)) return true;

            // Per-limb capsule pose offsets (hand wristwatch fix, etc.).
            if (typeof(RagdollLimbCapsuleFit).IsAssignableFrom(type)) return true;

            // Hair plume / helmet / capsule binder stack — keep visual + sim drivers.
            if (IsHairRuntimeComponent(type)) return true;

            // UnityEngine built-ins only (not custom MonoBehaviours in UnityEngine.* rare cases).
            string ns = type.Namespace ?? "";
            if (ns == "UnityEngine" || ns.StartsWith("UnityEngine.", StringComparison.Ordinal))
            {
                // Keep renderers / animation / physics; drop AudioListener etc. by default.
                if (typeof(Renderer).IsAssignableFrom(type)) return true;
                if (type == typeof(Animator)) return true;
                if (typeof(Joint).IsAssignableFrom(type)) return true;
                if (typeof(Collider).IsAssignableFrom(type)) return true;
                if (type == typeof(Rigidbody) || type == typeof(Cloth) || type == typeof(LODGroup)
                    || type == typeof(MeshFilter) || type == typeof(Transform))
                    return true;
                // Explicitly drop AudioListener, Camera, Light, etc. from donor characters.
                return false;
            }
            // Other project / third-party MonoBehaviours are leftovers (recreated by AutoWire if needed).
            return false;
        }

        /// <summary>Runtime hair MonoBehaviours that should survive strip (not editor-only tools).</summary>
        public static bool IsHairRuntimeComponent(Type type)
        {
            if (type == null || !typeof(MonoBehaviour).IsAssignableFrom(type)) return false;
            // Prefer type checks for known drivers; fall back to Hair* MonoBehaviour name prefix.
            if (type == typeof(HairPlumePhysicsDriver)
                || type == typeof(HairBodyCapsuleBinder)
                || type == typeof(HairColliderPrimitiveScanner)
                || type == typeof(HairHelmetTuckController)
                || type == typeof(HairHelmetTuckFrameMarker)
                || type == typeof(HairLinePartGizmo))
                return true;
            string name = type.Name ?? "";
            return name.StartsWith("Hair", StringComparison.Ordinal)
                   && !name.EndsWith("Editor", StringComparison.Ordinal)
                   && !name.EndsWith("Window", StringComparison.Ordinal)
                   && !name.EndsWith("Drawer", StringComparison.Ordinal);
        }

        public static string HierarchyPath(Transform root, Transform node)
        {
            if (node == null) return "";
            if (root == null || node == root) return node.name;
            var parts = new List<string>();
            Transform cur = node;
            while (cur != null)
            {
                parts.Add(cur.name);
                if (cur == root) break;
                cur = cur.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
#endif
