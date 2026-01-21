// a hand is an appendage that grabs and can be "usually" articulated in front of the individual
// a finger is a part of the hand that bends to pick up things alone or with other fingers
// a thumb is a specific finger evolved to rotate
//
// the human hand has 4 fingers and 1 thumb
//  each finger has 3 digits, a bone that counts as a length of finger
//   each digit has a knuckle, the connecting joint, and 2 elbows connecting 
//   each digit reduces in length and width starting from the hand
//   the last digit on the finger has a has a finger nail
//    finger nails have cuticles that control growth of nail
//  the thumb has 2 digits and is bigger than most fingers
// the thumb and the 4 fingers are connected to the wrist by bones that span the hand
// the bones in the wrist are wrapped in a band, joined with a bundle of sinew, and shaped
//   like partially deflated footballs and fruity pebbles, to allow the wrist to bend in 6 DOF 
//     (although roll is limited and delegates to the forearm, which has 2 bones that rotate at one end)
// the muscle of the hands has # modes: wrist-pull, finger-pull
//   the thumb and pinky both have muscle on the hand, below connected to the wrist and the bones that support them
//    the has muscle to pull itself to the hand, but uses the arm muscle to pull back
//    the pinky muscle uses its own muscle to close and pull in, and the arm muscles to pull back
//   the other fingers have sinew that extends through the wrist and \
//      is connected to a web of muscles that culminate at the of the forearm near the elbow
//   
// let's write a hand unity component that features fingers, thumb, digits, nails, bones, and wrist
// the wrist bones should be a topology and be visually represented rather than physically simulated
// digits should use prefabs
// thumb nail should use prefab
// nail should be mesh contructed from slow linear progression toward a conque shell shaped design
// the hand shape should be a trapazoidal prism that changes slightly to encapsulate bone position
// the muscles should be elipsoid spheres

// let's write a slim nervous system that sends impulses up and down,
//  impulses down include "activate muscle"
//  impulses up include "physics information - what's on me etc", "contacts"
// 
// nervous system goals:
//   walk
//   jump
//   run
//   grab
//   poke
//   climb
//   prone
//   crawl
//   slink
//  
// nervous system goals should be represented as a temporal graph
//  with known good sections traversed via a topology
// "good sections" should be stacks of impulse actions, or cards that lead from one state to another
//  they should indicate their limits, and any connected "good sections"
// we should be able to query the nervous system with a GameObject, then have the nervous system give us available
// "good sections"
//  or we should be able to query the nervous system with a GameObject, and a desired goal or "good section" and have
//    the nervous system respond with a "video routine" of good sections in order that satisfy the desired topology.
//    we should be able to insert the "video routine" and have the musculature respond desireably to our goal
//  impulses should also include things like physics contacts (for feet and hands common animations),
//    as well as arbitrary things, like heat or custom game events like an animal licking them, or
//    someone pointing something out, with or without a laser pointer
//
//  standing up:
//    pull in knees, feet flat, hands behind you, push torso forward, support with arms while legs open and torso extends
//    pull in hands and knees to ball, extend one leg to fall on side then close leg, on ground side pull arm back and press open lifting torso
//       pull ground side back muscle to bend spine, and rotate hips toward opening arm
//       pull inner thigh and upper leg and ground side groin muscles to situate foot curl leg, and calf to bring foot on floor
//       support with arm by stiffening, opening and closing the same amount providing extra where necessary
//       open ground leg slowly while opening other leg (keeping shin muscle pulled to pull foot), 
//       and supporting opening leg one planted on the floor
//       pull back muscles and butt muscles to stand
//   pull in arms and legs, prone ball, release one leg, then the other leg, straight out, both legs taught,
//       put both arms out, now on your back, relax all limbs, pull knees up, letting feet flat on floor
//       pull one arm up at the shoulder, hand next to rib cage
//       put a lot of power into front body core muscles, while pushing with butt and backleg muscles
//       push hard with tricept and trap muscles, extending push through fingers
//       once launched above 45, bring butt back and push then stand


// nice to have:
//  we should be able to match this setup with a ragdoll for a humanoid
//  given a set of "good sections" and motion graph sections, we should be able to
//  use a set of animations and a AABB capsule collider spoof a real walk cycle,
// and temporarily break sections of the ragdoll out of animation to perform tasks,
// and since we have the "good sections" as pieces of required communication,
// we can procedurally disable the tree, and topologically handle the animation of the
// non-hand-animated (ironically lol) ragdoll tree sections!

// for the first time around, let's make our character super man, and let the individual muscles be as strong as they
// have to be to reach a desired card position to continue our procedural animation
// we should develop an LSTM or more generic RNN that can tell the next card's with accuracy, or guess the amount
//   of strength necessary
// we should add an arbitrary tree fit component solver for game object trees
//

// todo: cars, with chassis, engines, tanks, drivetrains, steering, brakes, etc
//       bicycles, with peddles, shifters, gears, i-n-d-i-v-i-d-u-a-l chain links (or approximation), handle bars
//       structures with deformation, it'd be fun to run this with the weather system on a barn and make a scene from
//        twister, including the acting procedurally, since the pressure change, and sound of the twister could be
//        counted as cards that offload to a behavior tree for AI, that send the characters to gather the animals
//        close the barn, and head into the cellar
//        
//      if we get really good at this, we should make a stanley steamer simulator
// 
// todo: make a card game out of these
//    when we resume we should all be able to walk our characters around
//    so it would be like poker alan wake 2 fortnite takashis castle (cards 
//       in hand when taking damage, otherwise attacks do nothing, first one back wins)


// if the physics card, or "good section" is available from the current selection (applied from animation, or around procedurally)
//   the card should be looked at for range, and ordered for applicability based on:
//      - degrees difference required
//      - torque, force
//      - velocity change, likely hood given force / torque ranges
// 
// e.x. 
//  the idea that a screw could be turned by hand, would occur to the solver with available cards
//    finding 2 good sections, we get 2 cards
//     - fingers grab and turn (includes behavior tree)
//     - pick up screw driver, turn (includes behavior tree with pruning)
// these 2 cards should order fairly simply, finding little good in finger grab and turn
// but if the screw were entirely loose, the finger grab and turn might be a better option

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Musculature
{
    public enum FingerKind
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky,
        Extra
    }

    /// <summary>
    /// Marker + container for a hand on a rig.
    /// </summary>
    public sealed class RagdollHand : RagdollSidedBodyPart
    {
        [Tooltip("Resolved finger roots for this hand (thumb/index/middle/ring/pinky + extras).")]
        public List<RagdollFinger> fingers = new List<RagdollFinger>(8);

        [Header("Finger Utilities")]
        [Tooltip("If true, LessFingers removes all fingers at/after the start index (truncate).")]
        public bool lessFingersRemoveAllAfter = false;

        public enum CabooseMode
        {
            None,
            Append
        }

        public void LessFingers(int startIndex, int removeCount, bool removeAfter = false, CabooseMode caboose = CabooseMode.None, RagdollFinger cabooseTemplate = null)
        {
            if (fingers == null) fingers = new List<RagdollFinger>();
            int count = fingers.Count;

            if (startIndex < 0 || startIndex > count)
            {
                Debug.LogWarning($"[RagdollHand] LessFingers startIndex {startIndex} out of range (0..{count}).");
                return;
            }

            int effectiveRemove = Mathf.Max(0, removeCount);
            if (removeAfter || lessFingersRemoveAllAfter)
            {
                int removeAll = count - startIndex;
                if (effectiveRemove != removeAll)
                {
                    Debug.LogWarning($"[RagdollHand] LessFingers(removeAfter=true) requested removeCount={effectiveRemove} but will remove {removeAll}.");
                }
                effectiveRemove = removeAll;
            }

            int available = count - startIndex;
            if (effectiveRemove > available)
            {
                Debug.LogWarning($"[RagdollHand] LessFingers removing beyond list: requested {effectiveRemove} but only {available} available. Treating as partial removal.");
            }

            int toRemove = Mathf.Min(effectiveRemove, available);
            if (toRemove > 0)
                fingers.RemoveRange(startIndex, toRemove);

            // Caboose append: if we removed beyond available (conceptual partial finger), allow appending a caboose finger.
            if (caboose == CabooseMode.Append && effectiveRemove > available)
            {
                if (cabooseTemplate == null)
                {
                    Debug.LogWarning("[RagdollHand] Caboose requested, but no cabooseTemplate provided to append.");
                    return;
                }

                var go = new GameObject($"{side}_CabooseFinger");
                go.transform.SetParent(transform, worldPositionStays: false);
                var f = go.AddComponent<RagdollFinger>();
                f.side = side;
                f.kind = FingerKind.Extra;
                f.isCaboose = true;
                fingers.Add(f);
            }
        }

        public void ExtraFingers(int insertIndex, int addCount, RagdollFinger template = null)
        {
            if (fingers == null) fingers = new List<RagdollFinger>();
            int count = fingers.Count;
            insertIndex = Mathf.Clamp(insertIndex, 0, count);

            int n = Mathf.Max(0, addCount);
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject($"{side}_ExtraFinger_{insertIndex + i}");
                go.transform.SetParent(transform, worldPositionStays: false);
                var f = go.AddComponent<RagdollFinger>();
                f.side = side;
                f.kind = FingerKind.Extra;
                fingers.Insert(insertIndex + i, f);
            }
        }
    }

    /// <summary>
    /// Marker for a finger root. Digits are ordered proximal->distal.
    /// </summary>
    public sealed class RagdollFinger : MonoBehaviour
    {
        public BodySide side;
        public FingerKind kind;

        [Tooltip("Digit components from proximal->distal.")]
        public List<RagdollDigit> digits = new List<RagdollDigit>(4);

        [Tooltip("If true, this finger is a 'caboose' partial finger placeholder.")]
        public bool isCaboose = false;
    }

    /// <summary>
    /// Marker for a digit bone in a finger chain.
    /// </summary>
    public sealed class RagdollDigit : MonoBehaviour
    {
        public int indexInFinger = 0;

        [Tooltip("If true, this is the last digit in the chain (caboose digit).")]
        public bool isCabooseDigit = false;

        [Tooltip("Optional nailbed mesh generated on caboose digit.")]
        public RagdollNailbed nailbed;
    }

    /// <summary>
    /// Optional curve-based nailbed mesh for the caboose digit.
    /// Generates a simple ribbon mesh along local +Z.
    /// </summary>
    [ExecuteAlways]
    public sealed class RagdollNailbed : MonoBehaviour
    {
        [Tooltip("Width profile along the nail (0..1 t). Output is multiplied by baseWidth.")]
        public AnimationCurve widthProfile = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);

        public float length = 0.03f;
        public float baseWidth = 0.01f;
        public float thickness = 0.0015f;
        [Range(2, 64)] public int segments = 12;

        public Material material;

        private MeshFilter mf;
        private MeshRenderer mr;
        private Mesh mesh;

        private void OnEnable()
        {
            EnsureComponents();
            RebuildMesh();
        }

        private void OnValidate()
        {
            length = Mathf.Max(0.0001f, length);
            baseWidth = Mathf.Max(0.0001f, baseWidth);
            thickness = Mathf.Max(0f, thickness);
            segments = Mathf.Clamp(segments, 2, 128);
            EnsureComponents();
            RebuildMesh();
        }

        private void EnsureComponents()
        {
            mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
            if (material != null) mr.sharedMaterial = material;
        }

        public void RebuildMesh()
        {
            if (mf == null) EnsureComponents();
            if (mesh == null)
            {
                mesh = new Mesh { name = "NailbedMesh" };
                mesh.MarkDynamic();
            }

            int vCount = (segments + 1) * 4; // top+bottom, left+right
            var verts = new Vector3[vCount];
            var uvs = new Vector2[vCount];

            int triCount = segments * 6 * 2; // top and bottom ribbons (no sides)
            var tris = new int[triCount];

            int vi = 0;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float w = baseWidth * Mathf.Max(0f, widthProfile.Evaluate(t));
                float z = t * length;

                // top surface
                verts[vi + 0] = new Vector3(-w * 0.5f, thickness * 0.5f, z);
                verts[vi + 1] = new Vector3(+w * 0.5f, thickness * 0.5f, z);
                // bottom surface
                verts[vi + 2] = new Vector3(-w * 0.5f, -thickness * 0.5f, z);
                verts[vi + 3] = new Vector3(+w * 0.5f, -thickness * 0.5f, z);

                uvs[vi + 0] = new Vector2(0f, t);
                uvs[vi + 1] = new Vector2(1f, t);
                uvs[vi + 2] = new Vector2(0f, t);
                uvs[vi + 3] = new Vector2(1f, t);

                vi += 4;
            }

            int ti = 0;
            for (int i = 0; i < segments; i++)
            {
                int baseV = i * 4;
                int nextV = (i + 1) * 4;

                // top quad (baseV+0, baseV+1, nextV+0, nextV+1)
                tris[ti++] = baseV + 0;
                tris[ti++] = nextV + 0;
                tris[ti++] = nextV + 1;
                tris[ti++] = baseV + 0;
                tris[ti++] = nextV + 1;
                tris[ti++] = baseV + 1;

                // bottom quad (flip winding)
                tris[ti++] = baseV + 2;
                tris[ti++] = nextV + 3;
                tris[ti++] = nextV + 2;
                tris[ti++] = baseV + 2;
                tris[ti++] = baseV + 3;
                tris[ti++] = nextV + 3;
            }

            mesh.Clear();
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
        }
    }
}

