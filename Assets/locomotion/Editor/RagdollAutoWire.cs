#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Locomotion.Audio;
using Locomotion.Senses;
using Locomotion.Rig;

namespace Locomotion.EditorTools
{
    public static class RagdollAutoWire
    {
        public class Report
        {
            public List<string> info = new List<string>();
            public List<string> warnings = new List<string>();
            public List<string> errors = new List<string>();
        }

        public static Animator FindAnimator(GameObject actor)
        {
            return actor != null ? actor.GetComponentInChildren<Animator>() : null;
        }

        public static bool IsHumanoid(Animator animator)
        {
            return animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid;
        }

        public static Transform GetHumanBone(Animator animator, HumanBodyBones bone)
        {
            if (animator == null) return null;
            return animator.GetBoneTransform(bone);
        }

        public static BoneMap EnsureBoneMap(GameObject actor)
        {
            var bm = actor.GetComponent<BoneMap>();
            if (bm == null) bm = Undo.AddComponent<BoneMap>(actor);
            return bm;
        }

        public static void AutoFillHumanBoneMap(BoneMap bm, Animator animator)
        {
            if (bm == null || animator == null) return;

            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone) continue;
                Transform t = animator.GetBoneTransform(bone);
                if (t != null)
                {
                    bm.Set($"Human:{bone}", t);
                }
            }
        }

        public static GameObject EnsureChild(GameObject root, string name)
        {
            Transform existing = root.transform.Find(name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(root.transform, worldPositionStays: false);
            return go;
        }

        public static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(go);
            return c;
        }

        public static void EnsureGlobalSolvers(Report report)
        {
            if (Object.FindObjectOfType<AudioPathingSolver>() == null)
            {
                var go = new GameObject("AudioPathingSolver");
                Undo.RegisterCreatedObjectUndo(go, "Create AudioPathingSolver");
                go.AddComponent<AudioPathingSolver>();
                report?.info.Add("Created global AudioPathingSolver");
            }

            if (Object.FindObjectOfType<HierarchicalPathingSolver>() == null)
            {
                var go = new GameObject("HierarchicalPathingSolver");
                Undo.RegisterCreatedObjectUndo(go, "Create HierarchicalPathingSolver");
                go.AddComponent<HierarchicalPathingSolver>();
                report?.info.Add("Created global HierarchicalPathingSolver");
            }
        }

        public static void EnsureLocomotionCore(GameObject actor, Report report)
        {
            EnsureComponent<RagdollSystem>(actor);
            EnsureComponent<NervousSystem>(actor);
            EnsureComponent<PhysicsCardSolver>(actor);
            EnsureComponent<WorldInteraction>(actor);

            // At least one Brain
            if (actor.GetComponentInChildren<Brain>() == null)
            {
                var brain = EnsureComponent<Brain>(actor);
                brain.attachedBodyPart = actor;
                report?.info.Add("Created Brain on actor root");
            }
        }

        public static void EnsureSensors(GameObject actor, BoneMap bm, Animator animator, Report report)
        {
            var sensesRoot = EnsureChild(actor, "Senses");

            // Eyes
            var eyesRoot = EnsureChild(sensesRoot, "Eyes");
            var leftEyeGo = EnsureChild(eyesRoot, "LeftEye");
            var rightEyeGo = EnsureChild(eyesRoot, "RightEye");

            var leftEyeSensor = EnsureComponent<Sensor>(leftEyeGo);
            leftEyeSensor.sensorType = SensorType.Visual;
            var rightEyeSensor = EnsureComponent<Sensor>(rightEyeGo);
            rightEyeSensor.sensorType = SensorType.Visual;

            var eyes = EnsureComponent<Eyes>(eyesRoot);
            eyes.leftEye = leftEyeSensor;
            eyes.rightEye = rightEyeSensor;

            // Smell
            var smellGo = EnsureChild(sensesRoot, "Nose");
            var smellSensor = EnsureComponent<Sensor>(smellGo);
            smellSensor.sensorType = SensorType.Smell;
            EnsureComponent<SmellSensor>(smellGo);

            // Ears
            var earsRoot = EnsureChild(sensesRoot, "Ears");
            var leftEarGo = EnsureChild(earsRoot, "LeftEar");
            var rightEarGo = EnsureChild(earsRoot, "RightEar");

            EnsureComponent<Locomotion.Audio.Ears>(leftEarGo);
            EnsureComponent<Locomotion.Audio.Ears>(rightEarGo);

            // Wire WorldInteraction sensor list by leaving it empty (it auto-finds children sensors),
            // but ensure Sensor components exist.
            var wi = actor.GetComponent<WorldInteraction>();
            if (wi != null && (wi.sensors == null || wi.sensors.Count == 0))
            {
                report?.info.Add("WorldInteraction will auto-discover sensors in children.");
            }
        }

        public static void EnsureRagdollPhysicsHybrid(GameObject actor, Animator animator, BoneMap bm, Report report)
        {
            if (animator == null || !IsHumanoid(animator))
            {
                report?.warnings.Add("Animator is not humanoid; skipping auto ragdoll joint creation (wizard can still wire systems).");
                return;
            }

            // Ensure ragdoll root RB exists
            var ragdollSystem = EnsureComponent<RagdollSystem>(actor);
            if (ragdollSystem.ragdollRoot == null)
                ragdollSystem.ragdollRoot = actor.transform;

            var rootRb = actor.GetComponent<Rigidbody>();
            if (rootRb == null) rootRb = Undo.AddComponent<Rigidbody>(actor);
            rootRb.isKinematic = false;

            // Bone list (MVP)
            HumanBodyBones[] required =
            {
                HumanBodyBones.Hips,
                HumanBodyBones.Spine,
                HumanBodyBones.Chest,
                HumanBodyBones.Neck,
                HumanBodyBones.Head,
                HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand,
                HumanBodyBones.RightUpperArm,
                HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightHand,
                HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot,
                HumanBodyBones.RightUpperLeg,
                HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot
            };

            var musclesByRegion = new Dictionary<string, List<Muscle>>
            {
                { "Spine", new List<Muscle>() },
                { "LeftArm", new List<Muscle>() },
                { "RightArm", new List<Muscle>() },
                { "LeftLeg", new List<Muscle>() },
                { "RightLeg", new List<Muscle>() },
                { "Head", new List<Muscle>() },
            };

            // Ensure joints/muscles exist (hybrid)
            for (int i = 0; i < required.Length; i++)
            {
                HumanBodyBones bone = required[i];
                Transform t = animator.GetBoneTransform(bone);
                if (t == null) continue;

                bm?.Set($"Human:{bone}", t);

                GameObject go = t.gameObject;
                Rigidbody rb = go.GetComponent<Rigidbody>();
                if (rb == null) rb = Undo.AddComponent<Rigidbody>(go);
                rb.mass = Mathf.Max(0.1f, rb.mass);

                // Connect to parent rigidbody when possible
                Rigidbody parentRb = null;
                if (t.parent != null) parentRb = t.parent.GetComponentInParent<Rigidbody>();
                if (parentRb == null) parentRb = rootRb;

                ConfigurableJoint cj = go.GetComponent<ConfigurableJoint>();
                if (cj == null) cj = Undo.AddComponent<ConfigurableJoint>(go);
                cj.connectedBody = parentRb;
                cj.autoConfigureConnectedAnchor = true;
                cj.rotationDriveMode = RotationDriveMode.Slerp;

                // Conservative defaults
                JointDrive drive = cj.slerpDrive;
                drive.positionSpring = 100f;
                drive.positionDamper = 10f;
                drive.maximumForce = 1000f;
                cj.slerpDrive = drive;

                // Ensure Muscle
                Muscle m = go.GetComponent<Muscle>();
                if (m == null) m = Undo.AddComponent<Muscle>(go);

                // Group assignment
                if (bone == HumanBodyBones.Head || bone == HumanBodyBones.Neck)
                    musclesByRegion["Head"].Add(m);
                else if (bone.ToString().StartsWith("LeftUpperArm") || bone.ToString().StartsWith("LeftLowerArm") || bone == HumanBodyBones.LeftHand)
                    musclesByRegion["LeftArm"].Add(m);
                else if (bone.ToString().StartsWith("RightUpperArm") || bone.ToString().StartsWith("RightLowerArm") || bone == HumanBodyBones.RightHand)
                    musclesByRegion["RightArm"].Add(m);
                else if (bone.ToString().StartsWith("LeftUpperLeg") || bone.ToString().StartsWith("LeftLowerLeg") || bone == HumanBodyBones.LeftFoot)
                    musclesByRegion["LeftLeg"].Add(m);
                else if (bone.ToString().StartsWith("RightUpperLeg") || bone.ToString().StartsWith("RightLowerLeg") || bone == HumanBodyBones.RightFoot)
                    musclesByRegion["RightLeg"].Add(m);
                else
                    musclesByRegion["Spine"].Add(m);
            }

            // Create MuscleGroups container + groups
            var groupsRoot = EnsureChild(actor, "MuscleGroups");
            var groupComponents = new List<MuscleGroup>();

            foreach (var kvp in musclesByRegion)
            {
                if (kvp.Value.Count == 0) continue;

                var gObj = EnsureChild(groupsRoot, kvp.Key);
                var mg = EnsureComponent<MuscleGroup>(gObj);
                mg.groupName = kvp.Key;
                mg.muscles = kvp.Value;
                groupComponents.Add(mg);
            }

            ragdollSystem.muscleGroups = groupComponents;
            EditorUtility.SetDirty(ragdollSystem);

            report?.info.Add($"Ragdoll hybrid build: configured {groupComponents.Count} muscle groups.");
        }
    }
}
#endif

