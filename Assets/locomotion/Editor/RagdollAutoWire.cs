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
            if (Object.FindAnyObjectByType<AudioPathingSolver>() == null)
            {
                var go = new GameObject("AudioPathingSolver");
                Undo.RegisterCreatedObjectUndo(go, "Create AudioPathingSolver");
                go.AddComponent<AudioPathingSolver>();
                report?.info.Add("Created global AudioPathingSolver");
            }

            if (Object.FindAnyObjectByType<HierarchicalPathingSolver>() == null)
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

            EnsureBrainWithDualLstm(actor, report);

            var ragdollActor = EnsureComponent<RagdollActor>(actor);
            AssignDefaultGetUpPrefab(ragdollActor, report);
        }

        /// <summary>
        /// Ensures a Brain GameObject at the head centroid with Left/Right <see cref="LSTMPredictor"/> children,
        /// wired for dual LSTM. Idempotent.
        /// </summary>
        public static Brain EnsureBrainWithDualLstm(GameObject actor, Report report = null)
        {
            if (actor == null)
                return null;

            Transform head = ResolveHeadTransform(actor);
            Brain brain = PlaceOrMigrateBrainUnderHead(actor, head, report);
            if (brain == null)
                return null;

            Undo.RecordObject(brain, "Wire dual LSTM Brain");
            brain.attachedBodyPart = actor;

            GameObject leftGo = EnsureChild(brain.gameObject, "LeftLSTM");
            GameObject rightGo = EnsureChild(brain.gameObject, "RightLSTM");
            var left = EnsureComponent<LSTMPredictor>(leftGo);
            var right = EnsureComponent<LSTMPredictor>(rightGo);

            brain.leftLSTM = left;
            brain.rightLSTM = right;
            brain.enableDualLSTM = true;
            brain.mirrorDimension = MirrorDimension.X;

            EditorUtility.SetDirty(brain);
            EditorUtility.SetDirty(left);
            EditorUtility.SetDirty(right);
            report?.info.Add($"Brain dual LSTM ready under '{head.name}' (LeftLSTM / RightLSTM).");
            return brain;
        }

        static Transform ResolveHeadTransform(GameObject actor)
        {
            var animator = FindAnimator(actor);
            if (IsHumanoid(animator))
            {
                Transform bone = GetHumanBone(animator, HumanBodyBones.Head);
                if (bone != null)
                    return bone;
            }

            var ragdoll = actor.GetComponent<RagdollSystem>();
            if (ragdoll != null && ragdoll.headComponent != null)
            {
                Transform t = ragdoll.headComponent.PrimaryBoneTransform;
                if (t != null)
                    return t;
                return ragdoll.headComponent.transform;
            }

            return actor.transform;
        }

        static Brain PlaceOrMigrateBrainUnderHead(GameObject actor, Transform head, Report report)
        {
            GameObject brainGo = null;
            BehaviorTree migratedTree = null;
            List<Brain> migratedConnections = null;
            int migratedPriority = 0;

            // Legacy: Brain component on actor root — migrate fields, then remove.
            Brain rootBrain = actor.GetComponent<Brain>();
            if (rootBrain != null && rootBrain.gameObject == actor)
            {
                migratedTree = rootBrain.behaviorTree;
                migratedConnections = rootBrain.connectedBrains != null
                    ? new List<Brain>(rootBrain.connectedBrains)
                    : null;
                migratedPriority = rootBrain.priority;
                Undo.DestroyObjectImmediate(rootBrain);
                report?.info.Add("Removed legacy Brain from actor root (migrating under head).");
            }

            // Prefer an existing Brain child (not on actor root).
            Brain[] brains = actor.GetComponentsInChildren<Brain>(true);
            Brain existing = null;
            for (int i = 0; i < brains.Length; i++)
            {
                if (brains[i] != null && brains[i].gameObject != actor)
                {
                    existing = brains[i];
                    break;
                }
            }

            if (existing != null)
            {
                brainGo = existing.gameObject;
                if (brainGo.transform.parent != head)
                {
                    Undo.SetTransformParent(brainGo.transform, head, "Parent Brain under head");
                    report?.info.Add($"Reparented Brain under '{head.name}'.");
                }
            }
            else
            {
                brainGo = EnsureChild(head.gameObject, "Brain");
                report?.info.Add($"Created Brain under '{head.name}'.");
            }

            Undo.RecordObject(brainGo.transform, "Position Brain at head centroid");
            brainGo.transform.localPosition = Vector3.zero;
            brainGo.transform.localRotation = Quaternion.identity;
            brainGo.transform.localScale = Vector3.one;
            if (brainGo.name != "Brain")
                brainGo.name = "Brain";

            Brain brain = EnsureComponent<Brain>(brainGo);
            if (migratedTree != null && brain.behaviorTree == null)
                brain.behaviorTree = migratedTree;
            if (migratedConnections != null && (brain.connectedBrains == null || brain.connectedBrains.Count == 0))
                brain.connectedBrains = migratedConnections;
            if (migratedPriority != 0 && brain.priority == 0)
                brain.priority = migratedPriority;

            // Destroy any extra Brain components elsewhere under the actor (keep the head one).
            brains = actor.GetComponentsInChildren<Brain>(true);
            for (int i = 0; i < brains.Length; i++)
            {
                Brain other = brains[i];
                if (other == null || other == brain)
                    continue;
                if (other.gameObject == actor)
                {
                    Undo.DestroyObjectImmediate(other);
                    continue;
                }
                // Extra Brain GOs: remove component only if duplicate; prefer single Brain.
                Undo.DestroyObjectImmediate(other);
                report?.info.Add("Removed duplicate Brain under actor.");
            }

            return brain;
        }

        /// <summary>
        /// Floor/joint/collider repair plus Brain/LSTM, hair runtime, and animation roots.
        /// </summary>
        public static void RepairRagdoll(GameObject actor, Report report = null)
        {
            RepairFloorPenetration(actor, report);
            EnsureBrainWithDualLstm(actor, report);
            EnsureHairRuntime(actor, report);
            EnsureAnimationRoots(actor, report);
        }

        const string HairConfigFolder = "Assets/locomotion/hair/Baked";

        /// <summary>
        /// Ensures <see cref="HairPlumePhysicsDriver"/> + <see cref="HairBodyCapsuleBinder"/> under a Hair child
        /// (or rewires an existing driver), with scalp/head binding and optional default config.
        /// </summary>
        public static HairPlumePhysicsDriver EnsureHairRuntime(GameObject actor, Report report = null)
        {
            if (actor == null)
                return null;

            var ragdoll = actor.GetComponent<RagdollSystem>()
                          ?? actor.GetComponentInChildren<RagdollSystem>(true);
            var animator = FindAnimator(actor);
            Transform scalp = ResolveHeadTransform(actor);

            var driver = actor.GetComponentInChildren<HairPlumePhysicsDriver>(true);
            if (driver == null)
            {
                GameObject hairGo = EnsureChild(actor, "Hair");
                driver = EnsureComponent<HairPlumePhysicsDriver>(hairGo);
                report?.info.Add("Created Hair/HairPlumePhysicsDriver");
            }

            Undo.RecordObject(driver, "Wire hair runtime");
            var binder = driver.GetComponent<HairBodyCapsuleBinder>();
            if (binder == null)
                binder = EnsureComponent<HairBodyCapsuleBinder>(driver.gameObject);

            Undo.RecordObject(binder, "Wire hair body binder");
            binder.ragdoll = ragdoll;
            binder.animator = animator;
            binder.scalpRoot = scalp;
            binder.AutoSetOptionalOverrides();
            if (binder.head != null)
                scalp = binder.head;

            driver.scalpRoot = scalp;
            driver.bodyBinder = binder;

            if (driver.config == null)
            {
                HairPlumeConfig cfg = LoadOrCreateHairConfig(actor.name, report);
                driver.config = cfg;
                binder.config = cfg;
            }
            else
            {
                binder.config = driver.config;
            }

            driver.EnsurePartGizmo();
            EditorUtility.SetDirty(driver);
            EditorUtility.SetDirty(binder);
            report?.info.Add($"Hair runtime ready (scalp='{scalp?.name ?? "(null)"}').");
            return driver;
        }

        static HairPlumeConfig LoadOrCreateHairConfig(string actorName, Report report)
        {
            if (!AssetDatabase.IsValidFolder("Assets/locomotion/hair"))
            {
                if (AssetDatabase.IsValidFolder("Assets/locomotion"))
                    AssetDatabase.CreateFolder("Assets/locomotion", "hair");
            }
            if (!AssetDatabase.IsValidFolder(HairConfigFolder))
            {
                if (AssetDatabase.IsValidFolder("Assets/locomotion/hair"))
                    AssetDatabase.CreateFolder("Assets/locomotion/hair", "Baked");
            }

            string safe = string.IsNullOrEmpty(actorName) ? "Actor" : actorName;
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');
            string path = $"{HairConfigFolder}/HairPlumeConfig_{safe}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<HairPlumeConfig>(path);
            if (existing != null)
                return existing;

            var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
            cfg.name = $"HairPlumeConfig_{safe}";
            cfg.ApplyLatticeBakeDefaults();
            AssetDatabase.CreateAsset(cfg, path);
            AssetDatabase.SaveAssets();
            report?.info.Add($"Created hair config at {path}");
            return cfg;
        }

        /// <summary>
        /// Ensures animation set / IK managers and at least one AnimationBehaviorTree with an AnimationRoot child.
        /// Syncs selected IK sets into <c>{displayName}_animation_tree</c> children when present.
        /// </summary>
        public static void EnsureAnimationRoots(GameObject actor, Report report = null)
        {
            if (actor == null)
                return;

            var ragdoll = EnsureComponent<RagdollSystem>(actor);
            if (ragdoll.ragdollRoot == null)
                ragdoll.ragdollRoot = actor.transform;

            var setManager = actor.GetComponentInChildren<RagdollAnimationSetManager>(true);
            if (setManager == null)
            {
                setManager = EnsureComponent<RagdollAnimationSetManager>(actor);
                report?.info.Add("Created RagdollAnimationSetManager");
            }

            var ikManager = actor.GetComponentInChildren<RagdollIKAnimationManager>(true);
            if (ikManager == null)
            {
                ikManager = EnsureComponent<RagdollIKAnimationManager>(actor);
                report?.info.Add("Created RagdollIKAnimationManager");
            }

            Undo.RecordObject(setManager, "Wire animation set manager");
            Undo.RecordObject(ikManager, "Wire IK animation manager");
            setManager.ragdollSystem = ragdoll;
            ikManager.ragdollSystem = ragdoll;
            ikManager.animationSetManager = setManager;

            if (ragdoll.animationContainer == null)
            {
                GameObject container = EnsureChild(actor, "AnimationContainer");
                Undo.RecordObject(ragdoll, "Assign animationContainer");
                ragdoll.animationContainer = container.transform;
                report?.info.Add("Created AnimationContainer");
            }

            AnimationBehaviorTree abt = ragdoll.animationTree;
            if (abt == null)
                abt = actor.GetComponentInChildren<AnimationBehaviorTree>(true);

            if (abt == null)
            {
                Transform parent = ragdoll.animationContainer != null
                    ? ragdoll.animationContainer
                    : actor.transform;
                GameObject treeGo = EnsureChild(parent.gameObject, "Default_animation_tree");
                abt = EnsureComponent<AnimationBehaviorTree>(treeGo);
                var bt = EnsureComponent<BehaviorTree>(treeGo);
                abt.generatedTree = bt;
                report?.info.Add("Created Default_animation_tree (AnimationBehaviorTree)");
            }

            EnsureAnimationRootNode(abt, report);

            Undo.RecordObject(ragdoll, "Wire ragdoll animationTree");
            ragdoll.animationTree = abt;
            if (setManager.animationBehaviorTree == null && abt.generatedTree != null)
                setManager.animationBehaviorTree = abt.generatedTree;
            else if (setManager.animationBehaviorTree == null)
                setManager.animationBehaviorTree = abt.GetComponent<BehaviorTree>();

            // Re-ensure AnimationRoot on any existing *_animation_tree children, then sync selection.
            Transform treeParent = ikManager.GetRagdollActorTransform();
            if (treeParent != null)
            {
                for (int i = 0; i < treeParent.childCount; i++)
                {
                    Transform child = treeParent.GetChild(i);
                    if (child == null || !child.name.EndsWith("_animation_tree"))
                        continue;
                    var childAbt = child.GetComponent<AnimationBehaviorTree>();
                    if (childAbt == null)
                        childAbt = EnsureComponent<AnimationBehaviorTree>(child.gameObject);
                    EnsureComponent<BehaviorTree>(child.gameObject);
                    EnsureAnimationRootNode(childAbt, report);
                }
            }

            ikManager.SyncSelectionToSetManagerAndHierarchy();
            EditorUtility.SetDirty(setManager);
            EditorUtility.SetDirty(ikManager);
            EditorUtility.SetDirty(ragdoll);
            report?.info.Add("Animation roots / managers ready.");
        }

        static void EnsureAnimationRootNode(AnimationBehaviorTree abt, Report report)
        {
            if (abt == null)
                return;

            Transform existingRoot = abt.transform.Find("AnimationRoot");
            if (existingRoot != null && existingRoot.GetComponent<AnimationBehaviorTreeNode>() != null)
            {
                if (abt.rootNode == null)
                {
                    Undo.RecordObject(abt, "Assign AnimationRoot");
                    abt.rootNode = existingRoot.GetComponent<AnimationBehaviorTreeNode>();
                }
                var bt = abt.generatedTree != null ? abt.generatedTree : EnsureComponent<BehaviorTree>(abt.gameObject);
                abt.generatedTree = bt;
                if (bt.rootNode == null)
                    bt.rootNode = abt.rootNode;
                return;
            }

            if (abt.rootNode != null && abt.rootNode.gameObject != null)
                return;

            var node = abt.CreateRoot("AnimationRoot");
            var behaviorTree = abt.generatedTree != null
                ? abt.generatedTree
                : EnsureComponent<BehaviorTree>(abt.gameObject);
            abt.generatedTree = behaviorTree;
            if (behaviorTree.rootNode == null)
                behaviorTree.rootNode = node;
            EditorUtility.SetDirty(abt);
            EditorUtility.SetDirty(behaviorTree);
            report?.info.Add($"Created AnimationRoot under '{abt.name}'.");
        }

        /// <summary>
        /// Assigns the default get-up BT prefab onto <see cref="RagdollActor.getUpBehaviorTreePrefab"/> when null.
        /// </summary>
        public static void AssignDefaultGetUpPrefab(RagdollActor ragdollActor, Report report = null)
        {
            if (ragdollActor == null || ragdollActor.getUpBehaviorTreePrefab != null)
                return;

            var bt = AssetDatabase.LoadAssetAtPath<BehaviorTree>(RagdollGetUpTreeFactory.PrefabAssetPath);
            if (bt == null)
                return;

            Undo.RecordObject(ragdollActor, "Assign default get-up BehaviorTree prefab");
            ragdollActor.getUpBehaviorTreePrefab = bt;
            EditorUtility.SetDirty(ragdollActor);
            report?.info.Add("Assigned default RagdollGetUpBehaviorTree prefab");
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
            Undo.RecordObject(rootRb, "Configure hybrid root Rigidbody");
            rootRb.isKinematic = false;
            rootRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rootRb.interpolation = RigidbodyInterpolation.Interpolate;

            // Bone list (MVP) — Head group includes Head + Jaw (merged body part group)
            HumanBodyBones[] required =
            {
                HumanBodyBones.Hips,
                HumanBodyBones.Spine,
                HumanBodyBones.Chest,
                HumanBodyBones.Neck,
                HumanBodyBones.Head,
                HumanBodyBones.Jaw,
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
                Undo.RecordObject(rb, "Configure hybrid bone Rigidbody");
                rb.mass = Mathf.Max(0.1f, rb.mass);
                // ContinuousDynamic resists floor tunneling better than Continuous on free-falling limbs.
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                // Connect to parent rigidbody when possible
                Rigidbody parentRb = null;
                if (t.parent != null) parentRb = t.parent.GetComponentInParent<Rigidbody>();
                if (parentRb == null) parentRb = rootRb;

                ConfigurableJoint cj = go.GetComponent<ConfigurableJoint>();
                if (cj == null) cj = Undo.AddComponent<ConfigurableJoint>(go);
                Undo.RecordObject(cj, "Configure hybrid ConfigurableJoint");
                cj.connectedBody = parentRb;
                cj.autoConfigureConnectedAnchor = true;
                cj.rotationDriveMode = RotationDriveMode.Slerp;
                // Free linear DOFs let limbs separate and fall through the floor; joints then
                // slowly drag the head after the sunk chain ("sucked through").
                cj.xMotion = ConfigurableJointMotion.Locked;
                cj.yMotion = ConfigurableJointMotion.Locked;
                cj.zMotion = ConfigurableJointMotion.Locked;
                cj.projectionMode = JointProjectionMode.PositionAndRotation;
                cj.projectionDistance = 0.08f;
                cj.projectionAngle = 20f;
                cj.enableCollision = false;

                // Conservative defaults
                JointDrive drive = cj.slerpDrive;
                drive.positionSpring = 100f;
                drive.positionDamper = 10f;
                drive.maximumForce = 1000f;
                cj.slerpDrive = drive;

                EnsureLimbCollider(animator, bone, t, report);

                // Ensure Muscle
                Muscle m = go.GetComponent<Muscle>();
                if (m == null) m = Undo.AddComponent<Muscle>(go);

                // Group assignment — Head = merged head+jaw body part group
                if (bone == HumanBodyBones.Head || bone == HumanBodyBones.Neck || bone == HumanBodyBones.Jaw)
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

            report?.info.Add($"Ragdoll hybrid build: configured {groupComponents.Count} muscle groups (Locked linear joints + limb colliders).");
        }

        /// <summary>
        /// Ensures a <see cref="RagdollLimbCapsuleFit"/> + proxy CapsuleCollider on the bone.
        /// Skips when a fit already exists (preserves user rotation/translation offsets).
        /// Hands aim toward middle finger when present (avoids default local-Y "wristwatch" capsules).
        /// </summary>
        public static void EnsureLimbCollider(Animator animator, HumanBodyBones bone, Transform t, Report report)
        {
            if (t == null) return;

            var existingFit = t.GetComponent<RagdollLimbCapsuleFit>();
            if (existingFit != null)
            {
                existingFit.Apply();
                return;
            }

            // Already has a non-proxy collider (authored) — leave it unless it's a bare capsule we can migrate.
            var bareCapsule = t.GetComponent<CapsuleCollider>();
            bool migrateBare = bareCapsule != null;
            if (t.GetComponent<Collider>() != null && !migrateBare)
                return;

            ResolveLimbExtent(animator, bone, t, out float length, out Vector3 localDir);

            float radius = LimbRadius(bone, length);
            bool prevSuppress = RagdollLimbCapsuleFit.SuppressValidateApply;
            RagdollLimbCapsuleFit.SuppressValidateApply = true;
            RagdollLimbCapsuleFit fit;
            try
            {
                fit = Undo.AddComponent<RagdollLimbCapsuleFit>(t.gameObject);
                Undo.RecordObject(fit, "Configure limb capsule fit");
                fit.height = Mathf.Max(length, radius * 2.2f);
                fit.radius = radius;
                fit.direction = 1; // proxy Y after we rotate proxy to localDir
                fit.centerOffsetLocal = localDir * (length * 0.5f);
                fit.eulerOffsetDegrees = Quaternion.FromToRotation(Vector3.up, localDir).eulerAngles;
                fit.Apply();
            }
            finally
            {
                RagdollLimbCapsuleFit.SuppressValidateApply = prevSuppress;
            }
            report?.info.Add($"Added RagdollLimbCapsuleFit on {bone} ({t.name}).");
        }

        static void ResolveLimbExtent(
            Animator animator,
            HumanBodyBones bone,
            Transform t,
            out float length,
            out Vector3 localDir)
        {
            length = 0.2f;
            localDir = Vector3.up;

            Transform child = ResolveLimbChild(animator, bone);
            if (child != null)
            {
                Vector3 worldDelta = child.position - t.position;
                length = Mathf.Max(0.08f, worldDelta.magnitude);
                Vector3 local = t.InverseTransformDirection(worldDelta.normalized);
                if (local.sqrMagnitude > 1e-6f)
                    localDir = local.normalized;
                return;
            }

            if (bone == HumanBodyBones.Head || bone == HumanBodyBones.Jaw)
            {
                length = 0.22f;
                return;
            }

            if (bone == HumanBodyBones.LeftHand || bone == HumanBodyBones.RightHand)
            {
                if (TryHandPalmDirection(animator, bone, t, out length, out localDir))
                    return;
                length = 0.12f;
                return;
            }

            if (bone == HumanBodyBones.LeftFoot || bone == HumanBodyBones.RightFoot)
                length = 0.12f;
        }

        /// <summary>Aim hand capsule along middle (or average) finger bones when humanoid fingers exist.</summary>
        static bool TryHandPalmDirection(
            Animator animator,
            HumanBodyBones handBone,
            Transform hand,
            out float length,
            out Vector3 localDir)
        {
            length = 0.12f;
            localDir = Vector3.up;
            if (animator == null || hand == null) return false;

            HumanBodyBones[] tips =
                handBone == HumanBodyBones.LeftHand
                    ? new[]
                    {
                        HumanBodyBones.LeftMiddleDistal,
                        HumanBodyBones.LeftMiddleIntermediate,
                        HumanBodyBones.LeftMiddleProximal,
                        HumanBodyBones.LeftIndexDistal,
                        HumanBodyBones.LeftRingDistal
                    }
                    : new[]
                    {
                        HumanBodyBones.RightMiddleDistal,
                        HumanBodyBones.RightMiddleIntermediate,
                        HumanBodyBones.RightMiddleProximal,
                        HumanBodyBones.RightIndexDistal,
                        HumanBodyBones.RightRingDistal
                    };

            Vector3 sum = Vector3.zero;
            int n = 0;
            float maxLen = 0f;
            for (int i = 0; i < tips.Length; i++)
            {
                Transform tip = GetHumanBone(animator, tips[i]);
                if (tip == null) continue;
                Vector3 delta = tip.position - hand.position;
                float mag = delta.magnitude;
                if (mag < 0.01f) continue;
                sum += delta.normalized;
                maxLen = Mathf.Max(maxLen, mag);
                n++;
            }

            if (n == 0) return false;
            Vector3 worldDir = (sum / n).normalized;
            Vector3 local = hand.InverseTransformDirection(worldDir);
            if (local.sqrMagnitude < 1e-6f) return false;
            localDir = local.normalized;
            length = Mathf.Clamp(maxLen * 0.85f, 0.08f, 0.22f);
            return true;
        }

        static Transform ResolveLimbChild(Animator animator, HumanBodyBones bone)
        {
            HumanBodyBones childBone = HumanBodyBones.LastBone;
            switch (bone)
            {
                case HumanBodyBones.Hips: childBone = HumanBodyBones.Spine; break;
                case HumanBodyBones.Spine: childBone = HumanBodyBones.Chest; break;
                case HumanBodyBones.Chest: childBone = HumanBodyBones.Neck; break;
                case HumanBodyBones.Neck: childBone = HumanBodyBones.Head; break;
                case HumanBodyBones.LeftUpperArm: childBone = HumanBodyBones.LeftLowerArm; break;
                case HumanBodyBones.LeftLowerArm: childBone = HumanBodyBones.LeftHand; break;
                case HumanBodyBones.RightUpperArm: childBone = HumanBodyBones.RightLowerArm; break;
                case HumanBodyBones.RightLowerArm: childBone = HumanBodyBones.RightHand; break;
                case HumanBodyBones.LeftUpperLeg: childBone = HumanBodyBones.LeftLowerLeg; break;
                case HumanBodyBones.LeftLowerLeg: childBone = HumanBodyBones.LeftFoot; break;
                case HumanBodyBones.RightUpperLeg: childBone = HumanBodyBones.RightLowerLeg; break;
                case HumanBodyBones.RightLowerLeg: childBone = HumanBodyBones.RightFoot; break;
                default: return null;
            }
            return GetHumanBone(animator, childBone);
        }

        static float LimbRadius(HumanBodyBones bone, float length)
        {
            switch (bone)
            {
                case HumanBodyBones.Hips:
                case HumanBodyBones.Spine:
                case HumanBodyBones.Chest:
                    return Mathf.Clamp(length * 0.35f, 0.08f, 0.18f);
                case HumanBodyBones.Head:
                case HumanBodyBones.Jaw:
                    return Mathf.Clamp(length * 0.45f, 0.08f, 0.12f);
                case HumanBodyBones.LeftHand:
                case HumanBodyBones.RightHand:
                case HumanBodyBones.LeftFoot:
                case HumanBodyBones.RightFoot:
                    return Mathf.Clamp(length * 0.35f, 0.03f, 0.06f);
                default:
                    return Mathf.Clamp(length * 0.22f, 0.04f, 0.09f);
            }
        }

        static int DominantAxisIndex(Vector3 localDir)
        {
            float ax = Mathf.Abs(localDir.x);
            float ay = Mathf.Abs(localDir.y);
            float az = Mathf.Abs(localDir.z);
            if (ax >= ay && ax >= az) return 0;
            if (ay >= az) return 1;
            return 2;
        }

        /// <summary>
        /// Repair existing ragdolls: lock Free linear joints + add missing limb colliders.
        /// Call from Fitting Wizard / Replicator on already-wired actors.
        /// </summary>
        public static void RepairFloorPenetration(GameObject actor, Report report = null)
        {
            if (actor == null) return;
            var animator = FindAnimator(actor);
            if (!IsHumanoid(animator))
            {
                report?.warnings.Add("RepairFloorPenetration: animator is not humanoid.");
                return;
            }

            int locked = 0;
            int caps = 0;
            var joints = actor.GetComponentsInChildren<ConfigurableJoint>(true);
            for (int i = 0; i < joints.Length; i++)
            {
                var cj = joints[i];
                if (cj == null) continue;
                bool needs = cj.xMotion != ConfigurableJointMotion.Locked
                             || cj.yMotion != ConfigurableJointMotion.Locked
                             || cj.zMotion != ConfigurableJointMotion.Locked
                             || cj.projectionMode == JointProjectionMode.None;
                if (!needs) continue;
                Undo.RecordObject(cj, "Repair joint linear locks");
                cj.xMotion = ConfigurableJointMotion.Locked;
                cj.yMotion = ConfigurableJointMotion.Locked;
                cj.zMotion = ConfigurableJointMotion.Locked;
                cj.projectionMode = JointProjectionMode.PositionAndRotation;
                cj.projectionDistance = 0.08f;
                cj.projectionAngle = 20f;
                locked++;
            }

            HumanBodyBones[] bones =
            {
                HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest,
                HumanBodyBones.Neck, HumanBodyBones.Head, HumanBodyBones.Jaw,
                HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
                HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot
            };
            int beforeCols = actor.GetComponentsInChildren<Collider>(true).Length;
            for (int i = 0; i < bones.Length; i++)
            {
                Transform t = GetHumanBone(animator, bones[i]);
                if (t == null) continue;
                var rb = t.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Undo.RecordObject(rb, "Repair RB collision mode");
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
                bool isHand = bones[i] == HumanBodyBones.LeftHand || bones[i] == HumanBodyBones.RightHand;
                var fit = t.GetComponent<RagdollLimbCapsuleFit>();
                var bareCap = t.GetComponent<CapsuleCollider>();
                // Hands with a bare capsule (no fit) often point local-Y like a wristwatch — migrate to Fit.
                if (fit == null && (t.GetComponent<Collider>() == null || (isHand && bareCap != null)))
                {
                    if (isHand && bareCap != null)
                        Undo.DestroyObjectImmediate(bareCap);
                    EnsureLimbCollider(animator, bones[i], t, report);
                }
                else if (fit != null)
                {
                    fit.Apply();
                }
            }
            caps = actor.GetComponentsInChildren<Collider>(true).Length - beforeCols;
            report?.info.Add($"RepairFloorPenetration: locked {locked} joints, added {Mathf.Max(0, caps)} colliders.");
        }
    }
}
#endif

