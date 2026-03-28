#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>
    /// Reports prefab/asset completeness for SceneObjectEntry or GameObject.
    /// Used by the Prompt Tree Inspector asset property view wizards.
    /// </summary>
    public static class AssetCompletenessUtility
    {
        public const int StepCount = 5;

        public enum StepStatus { Missing, Ok }

        /// <summary>Completeness result for one asset.</summary>
        public struct CompletenessResult
        {
            public StepStatus ormRegistered;
            public StepStatus hasReferencePrefab;
            public StepStatus hasMesh;
            public StepStatus hasMaterials;
            public StepStatus hasAnimations;
            public int CompletedCount;
        }

        /// <summary>Check if entry exists in registry (caller provides registry).</summary>
        public static bool IsOrmRegistered(SceneObjectRegistry registry, string keyOrPhrase)
        {
            if (registry == null || string.IsNullOrEmpty(keyOrPhrase)) return false;
            return !string.IsNullOrEmpty(registry.ResolveKey(keyOrPhrase.Trim()));
        }

        /// <summary>Check if entry has reference or prefab.</summary>
        public static bool HasReferenceOrPrefab(SceneObjectEntry entry)
        {
            if (entry == null) return false;
            return (entry.reference != null) || (entry.prefabForClone != null);
        }

        /// <summary>Check if prefab/GO has mesh (Renderer or MeshFilter).</summary>
        public static bool HasMesh(GameObject go)
        {
            if (go == null) return false;
            if (go.GetComponent<Renderer>() != null) return true;
            var mf = go.GetComponent<MeshFilter>();
            return mf != null && mf.sharedMesh != null;
        }

        /// <summary>Check if prefab has materials/textures.</summary>
        public static bool HasMaterials(GameObject go)
        {
            if (go == null) return false;
            var r = go.GetComponent<Renderer>();
            if (r == null) return false;
            if (r.sharedMaterials == null || r.sharedMaterials.Length == 0) return false;
            foreach (var m in r.sharedMaterials)
            {
                if (m != null && m.mainTexture != null) return true;
                if (m != null) return true; // has material
            }
            return false;
        }

        /// <summary>Check if prefab has animations (Animator or Animation with clips).</summary>
        public static bool HasAnimations(GameObject go)
        {
            if (go == null) return false;
            var anim = go.GetComponent<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null) return true;
            var legacy = go.GetComponent<Animation>();
            if (legacy != null && legacy.clip != null) return true;
            return false;
        }

        /// <summary>Get full completeness for an entry.</summary>
        public static CompletenessResult GetCompleteness(SceneObjectRegistry registry, SceneObjectEntry entry, string keyOrPhrase)
        {
            var r = new CompletenessResult();
            r.ormRegistered = IsOrmRegistered(registry, keyOrPhrase) ? StepStatus.Ok : StepStatus.Missing;
            r.hasReferencePrefab = HasReferenceOrPrefab(entry) ? StepStatus.Ok : StepStatus.Missing;

            GameObject prefab = entry != null ? (entry.prefabForClone ?? entry.reference) : null;
            r.hasMesh = HasMesh(prefab) ? StepStatus.Ok : StepStatus.Missing;
            r.hasMaterials = HasMaterials(prefab) ? StepStatus.Ok : StepStatus.Missing;
            r.hasAnimations = HasAnimations(prefab) ? StepStatus.Ok : StepStatus.Missing;

            int n = 0;
            if (r.ormRegistered == StepStatus.Ok) n++;
            if (r.hasReferencePrefab == StepStatus.Ok) n++;
            if (r.hasMesh == StepStatus.Ok) n++;
            if (r.hasMaterials == StepStatus.Ok) n++;
            if (r.hasAnimations == StepStatus.Ok) n++;
            r.CompletedCount = n;
            return r;
        }
    }
}
#endif
