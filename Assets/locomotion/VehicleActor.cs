using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a vehicle root using <see cref="VehicleAmbulationSolver"/> and vehicle hierarchy parts.
/// 
/// todo: let's make a plan to add explorable vehicle fusilages (spelling):
///   making the assumption that interiors have vertexes that majority face toward eachother (dotproduct lattice)
///     (interior could be composed of mulitple models, extend search to all objects under selected with mesh including selected
///       and treat this as one pool of vertexes)
///   also optionally accept an anchor object from authors
///   create a cache for the hierarchical pathing solver by finding the exterior loop closest to the anchor object or the average shift-
///    away from the dotproduct as a loop, then closing by closest vertex between averaging loop
///   using the interior found, create containers for octtree hierarchical pathing and cache paths to instruments / animation objects
///   track actor changes that effect paths to instruments and animation objects like doors
///    on init and change use describe component on interior objects - possibly add a "explore interior" for the inside \
///      then use describe on the outside for any changes to the tree beyond suspension
///      ^ could potentially be costly, so maybe debounce
/// 
///  experiment: using introspection and decompiling, scan interior object tree for components that effect physics,
///              then when found, serialize the ship object parent, then perform various tests to see what does what
///              if we find via decompiling the ship has keyboard controls, we should try those via messages
/// </summary>
public sealed class VehicleActor : BaseAmbulatingActor
{
    [Tooltip("Optional drivetrain / steering solver on this actor.")]
    public VehicleAmbulationSolver ambulationSolver;

    [Tooltip("Optional instrument-only card solver when character is driving.")]
    public DrivingPhysicsCardSolver drivingPhysicsCardSolver;
}