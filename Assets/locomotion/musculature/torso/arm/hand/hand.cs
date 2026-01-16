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

