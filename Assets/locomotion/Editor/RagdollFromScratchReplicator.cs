#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Locomotion.Senses;
using Locomotion.Audio;
using Locomotion.Rig;
using Locomotion.Musculature;

namespace Locomotion.EditorTools
{
    public sealed class RagdollFromScratchOptions
    {
        public bool copyColliders = true;
        public bool copyJointLimitsAndDrives = true;
        public bool stripAnimatorController = true;
        public string alsoRegisterAsPlayerKey = "player";
        public string outputName;
        public string outputFolder = "Assets/locomotion/Prefabs/ActorRagdolls";
    }

    public sealed class RagdollFromScratchResult
    {
        public GameObject clone;
        public string prefabPath;
        public RagdollComponentLeftoverMap leftovers = new RagdollComponentLeftoverMap();
        public RagdollAutoWire.Report wireReport = new RagdollAutoWire.Report();
        public RagdollMobilityValidator.Report mobility;
        public List<string> validationNotes = new List<string>();
        public bool isReadyToSave;
        public bool success;
        public string error;
    }

    /// <summary>
    /// Clones a humanoid ragdoll, strips custom components (leftover map), copies physics,
    /// and wires the System-Drawer actor stack.
    /// </summary>
    public static class RagdollFromScratchReplicator
    {
        public static RagdollFromScratchResult Replicate(GameObject source, RagdollFromScratchOptions options = null)
        {
            var result = new RagdollFromScratchResult();
            options ??= new RagdollFromScratchOptions();

            if (source == null)
            {
                result.error = "Source is null.";
                return result;
            }

            var sourceAnimator = RagdollAutoWire.FindAnimator(source);
            if (!RagdollAutoWire.IsHumanoid(sourceAnimator))
            {
                result.error = "Source requires a humanoid Animator with a valid Avatar.";
                return result;
            }

            if (source.GetComponentsInChildren<Rigidbody>(true).Length == 0)
                result.wireReport.warnings.Add("Source has no Rigidbodies; hybrid AutoWire will create defaults after strip.");

            Undo.SetCurrentGroupName("Ragdoll From-Scratch Replicate");
            int undoGroup = Undo.GetCurrentGroup();

            GameObject clone;
            if (PrefabUtility.IsPartOfPrefabAsset(source))
            {
                clone = (GameObject)PrefabUtility.InstantiatePrefab(source);
                Undo.RegisterCreatedObjectUndo(clone, "Instantiate ragdoll prefab");
            }
            else
            {
                clone = Object.Instantiate(source);
                Undo.RegisterCreatedObjectUndo(clone, "Clone ragdoll");
            }

            string outName = string.IsNullOrEmpty(options.outputName)
                ? source.name + "_SystemDrawerRagdoll"
                : options.outputName;
            clone.name = outName;

            if (PrefabUtility.IsPartOfPrefabInstance(clone))
                PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.UserAction);

            // Strip custom / locomotion components before rewiring; never copy donor MonoBehaviours.
            result.leftovers = RagdollComponentStripper.StripAndCollectLeftovers(clone);

            var cloneAnimator = RagdollAutoWire.FindAnimator(clone);
            if (cloneAnimator != null && options.stripAnimatorController)
            {
                Undo.RecordObject(cloneAnimator, "Strip Animator controller");
                cloneAnimator.runtimeAnimatorController = null;
            }

            CopyHumanoidPhysics(sourceAnimator, cloneAnimator, options, result.wireReport);

            var boneMap = RagdollAutoWire.EnsureBoneMap(clone);
            RagdollAutoWire.AutoFillHumanBoneMap(boneMap, cloneAnimator);
            RagdollAutoWire.EnsureLocomotionCore(clone, result.wireReport);
            RagdollAutoWire.EnsureSensors(clone, boneMap, cloneAnimator, result.wireReport);
            RagdollAutoWire.EnsureRagdollPhysicsHybrid(clone, cloneAnimator, boneMap, result.wireReport);
            // Replicator physics copy can reintroduce Free linear DOFs from the donor — normalize.
            RagdollAutoWire.RepairRagdoll(clone, result.wireReport);

            var ragdollSystem = clone.GetComponent<RagdollSystem>();
            if (ragdollSystem != null)
            {
                ragdollSystem.ragdollRoot = clone.transform;
                FindOrAddBodyParts(ragdollSystem, result.wireReport);
                WirePreservedFingers(ragdollSystem, result.wireReport);
            }

            // Re-place Brain / hair / animation roots once body parts exist (idempotent).
            RagdollAutoWire.EnsureBrainWithDualLstm(clone, result.wireReport);
            RagdollAutoWire.EnsureHairRuntime(clone, result.wireReport);
            RagdollAutoWire.EnsureAnimationRoots(clone, result.wireReport);

            var actor = RagdollAutoWire.EnsureComponent<RagdollActor>(clone);
            RagdollAutoWire.AssignDefaultGetUpPrefab(actor, result.wireReport);
            var wizard = RagdollAutoWire.EnsureComponent<RagdollServiceWizard>(clone);
            wizard.ragdollRoot = clone.transform;
            wizard.alsoRegisterAsPlayerKey = options.alsoRegisterAsPlayerKey ?? "";
            EditorUtility.SetDirty(wizard);
            EditorUtility.SetDirty(actor);

            result.mobility = RagdollMobilityValidator.Validate(clone.transform);
            FillValidation(clone, result);

            result.clone = clone;
            result.success = string.IsNullOrEmpty(result.error);
            Undo.CollapseUndoOperations(undoGroup);
            return result;
        }

        public static RagdollFromScratchResult ReplicateAndSavePrefab(
            GameObject source,
            RagdollFromScratchOptions options = null,
            string prefabPath = null)
        {
            options ??= new RagdollFromScratchOptions();
            var result = Replicate(source, options);
            if (!result.success || result.clone == null)
                return result;

            if (string.IsNullOrEmpty(prefabPath))
            {
                EnsureFolder(options.outputFolder);
                prefabPath = options.outputFolder.TrimEnd('/', '\\') + "/" + result.clone.name + ".prefab";
            }

            string dir = System.IO.Path.GetDirectoryName(prefabPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir))
                EnsureFolder(dir);

            PrefabUtility.SaveAsPrefabAssetAndConnect(result.clone, prefabPath, InteractionMode.UserAction);
            result.prefabPath = prefabPath;
            result.wireReport.info.Add("Saved prefab: " + prefabPath);
            return result;
        }

        static void CopyHumanoidPhysics(
            Animator sourceAnimator,
            Animator destAnimator,
            RagdollFromScratchOptions options,
            RagdollAutoWire.Report report)
        {
            if (sourceAnimator == null || destAnimator == null) return;

            var rootRb = destAnimator.gameObject.GetComponent<Rigidbody>();
            if (rootRb == null)
                rootRb = destAnimator.transform.root.GetComponent<Rigidbody>();
            if (rootRb == null)
                rootRb = Undo.AddComponent<Rigidbody>(destAnimator.gameObject);

            int copied = 0;
            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone) continue;
                Transform src = sourceAnimator.GetBoneTransform(bone);
                Transform dst = destAnimator.GetBoneTransform(bone);
                if (src == null || dst == null) continue;
                if (src.GetComponent<Rigidbody>() == null
                    && src.GetComponent<ConfigurableJoint>() == null
                    && src.GetComponent<Collider>() == null)
                    continue;

                Rigidbody parentRb = RagdollPhysicsCopy.ResolveParentRigidbody(destAnimator, dst, rootRb);
                RagdollPhysicsCopy.CopyBonePhysics(
                    src, dst, parentRb,
                    copyColliders: options.copyColliders,
                    copyJointLimitsAndDrives: options.copyJointLimitsAndDrives);
                copied++;
            }
            report?.info.Add($"Physics copy: {copied} humanoid bones with source physics.");
        }

        static void FindOrAddBodyParts(RagdollSystem system, RagdollAutoWire.Report report)
        {
            if (system == null) return;
            // Prefer the same private ValidateBoneComponents path the Fitting Wizard uses.
            var method = typeof(RagdollSystem).GetMethod(
                "ValidateBoneComponents",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(system, null);
                report?.info.Add("ValidateBoneComponents (FindOrAdd + auto-link) completed.");
                return;
            }

            system.FindOrAddPelvis();
            system.FindOrAddTorso();
            system.FindOrAddNeck();
            system.FindOrAddHead();
            system.FindOrAddJaw();
            system.FindOrAddCollarbones();
            system.FindOrAddShoulders();
            system.FindOrAddUpperarm(BodySide.Left);
            system.FindOrAddUpperarm(BodySide.Right);
            system.FindOrAddElbows();
            system.FindOrAddForearm(BodySide.Left);
            system.FindOrAddForearm(BodySide.Right);
            system.FindOrAddHands();
            system.FindOrAddLeg(BodySide.Left);
            system.FindOrAddLeg(BodySide.Right);
            system.FindOrAddKnee(BodySide.Left);
            system.FindOrAddKnee(BodySide.Right);
            system.FindOrAddShins();
            system.FindOrAddFeet();
            report?.info.Add("FindOrAdd body parts completed.");
        }

        /// <summary>
        /// Finger/digit components are preserved by the stripper; rebind hand.fingers / digit lists
        /// after FindOrAddHands (which may create a fresh RagdollHand without those refs).
        /// </summary>
        static void WirePreservedFingers(RagdollSystem system, RagdollAutoWire.Report report)
        {
            if (system == null) return;
            int wired = 0;
            WireHandFingers(system.leftHandComponent, ref wired);
            WireHandFingers(system.rightHandComponent, ref wired);
            if (wired > 0)
                report?.info.Add($"Rebound fingers on {wired} hand(s) (preserved through strip).");
            else if (system.GetComponentInChildren<RagdollFinger>(true) != null)
                report?.warnings.Add("RagdollFinger components present but no RagdollHand to bind — run FindOrAddHands.");
        }

        static void WireHandFingers(RagdollHand hand, ref int wiredHands)
        {
            if (hand == null) return;
            Undo.RecordObject(hand, "Wire preserved fingers");
            var fingers = new List<RagdollFinger>();
            for (int i = 0; i < hand.transform.childCount; i++)
            {
                var finger = hand.transform.GetChild(i).GetComponent<RagdollFinger>();
                if (finger != null) fingers.Add(finger);
            }
            // Also include nested fingers that aren't direct children.
            if (fingers.Count == 0)
            {
                var all = hand.GetComponentsInChildren<RagdollFinger>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].transform != hand.transform)
                        fingers.Add(all[i]);
                }
            }

            hand.fingers = fingers;
            for (int f = 0; f < fingers.Count; f++)
            {
                var finger = fingers[f];
                if (finger == null) continue;
                Undo.RecordObject(finger, "Wire preserved digits");
                finger.side = hand.side;
                var digits = new List<RagdollDigit>();
                CollectDigitsRecursive(finger.transform, digits);
                for (int d = 0; d < digits.Count; d++)
                {
                    if (digits[d] == null) continue;
                    digits[d].indexInFinger = d;
                    digits[d].isCabooseDigit = d == digits.Count - 1;
                    Undo.RecordObject(digits[d], "Set digit index");
                }
                finger.digits = digits;
            }
            EditorUtility.SetDirty(hand);
            wiredHands++;
        }

        static void CollectDigitsRecursive(Transform root, List<RagdollDigit> list)
        {
            if (root == null || list == null) return;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                var digit = child.GetComponent<RagdollDigit>();
                if (digit != null)
                    list.Add(digit);
                CollectDigitsRecursive(child, list);
            }
        }

        static void FillValidation(GameObject clone, RagdollFromScratchResult result)
        {
            bool hasBoneMap = clone.GetComponent<BoneMap>() != null;
            bool hasRagdoll = clone.GetComponent<RagdollSystem>() != null;
            bool hasNervous = clone.GetComponent<NervousSystem>() != null;
            bool hasBrain = clone.GetComponentInChildren<Brain>() != null;
            bool hasWi = clone.GetComponent<WorldInteraction>() != null;
            bool hasSensor = clone.GetComponentInChildren<Sensor>() != null;
            bool hasEar = clone.GetComponentInChildren<Ears>() != null;

            void Note(bool ok, string label)
            {
                result.validationNotes.Add((ok ? "[OK] " : "[MISSING] ") + label);
            }

            Note(hasBoneMap, "BoneMap");
            Note(hasRagdoll, "RagdollSystem");
            Note(hasNervous, "NervousSystem");
            Note(hasBrain, "Brain");
            Note(hasWi, "WorldInteraction");
            Note(hasSensor, "Sensor");
            Note(hasEar, "Ears");

            result.isReadyToSave = hasBoneMap && hasRagdoll && hasNervous && hasBrain && hasWi && hasSensor && hasEar;
            if (!result.isReadyToSave)
                result.wireReport.warnings.Add("Validation incomplete — prefab save still allowed but wiring may be partial.");
        }

        public static void EnsureFolder(string assetFolder)
        {
            if (string.IsNullOrEmpty(assetFolder)) return;
            assetFolder = assetFolder.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            string[] parts = assetFolder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
