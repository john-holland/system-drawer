// the head has a bunch of plates that are one piece
//  and a jaw that flaps around
// the head also has a bunch of muscle on top for articulating the face and some hair expressions
// the nose has a piece of cartilage supporting it
//  we have the eyebrows  representing each with planes or cloth, like a curtain sinched at two corners, and
//    with two delicate groups of muscles wrapped like a venetian (van-ee-shan (sp?)) blind
//    while a delicate job, the muscles extend up into the skull under the hair and above the bone
// the nose is conncted to the eye muscles and the cheek muscles, individually articulateable with assist from cheeks or brows
// the cheeks are connected to muscle that extends up the side of the head but doesn't go back much
//    and along the jaw are muscles to help animate the cheeks and lips
//    primarily: chin muscle (lower lip up down),
//               jaw muscle (frown / dimple smile), 
//               cheek muscle (smile, grimace), 
//               nose muscle (scrunchy smile, eye squeezies)
// the ears are connected slightly through skin but not sinew to the muscles on top of head
//   the ears can be articulated by pulling the muscles that scrunch the forhead, and in the prosterior of the head
//      there may be specific ear muscles also used by the brow, evolved to pull the earway clear temporarily of wax (just a guess)

// the head should have a number of expressions and a set of topologies that reach that expression
// the head should have mouth limits
// additionally the head should have a number of degrees to open the mouth

// let's make a brain component that can be attached to the head or anywhere a nervous system dispatcher is needed
// the brain should contain the solvers cited in hand.cs and be given a priority so that multiple brains can be attached to the same body part
// brains will effectively act as filters, and can be considered appended behavior trees, who's main function is to interpret physics card impulses
// in this manner, brains should also be able to send thoughts to one another
// this could be a result of discrete or procedural, e.x. dialogue or arbitrary event sequence
// physics card solver <-> arbitrary behavior tree
// physics card solvers adjust component physics pieces and allow for topological sorts to perform possible physical space searches
// 


// if the physics card, or "good section" is available from the current selection (applied from animation, or around procedurally)
//   the card should be looked at for range, and ordered for applicability based on:
//      - degrees difference required
//      - torque, force
//      - velocity change, likely hood given force / torque ranges
//      - any other comparison properties specified
//
// when a "good section" is selected based on feasability / closeness to the current behavior tree
// it paints the stack of cards on the section of tree, such that each limb and chord receive what to do
// note: we may want to include 'adoption timeouts' to debounce the actor being stuck in loops during weak sections
//   of a behavior tree
//
// e.x. 
//  the idea that a screw could be turned by hand, would occur to the solver with available cards
//    finding 2 good sections, we get 2 cards
//     - fingers grab and turn (includes behavior tree)
//     - pick up screw driver, turn (includes behavior tree with pruning)
// these 2 cards should order fairly simply, finding little good in finger grab and turn
// but if the screw were entirely loose, the finger grab and turn might be a better option

using UnityEngine;

namespace Locomotion.Musculature
{
    public sealed class RagdollHead : RagdollBodyPart
    {
    }
}


