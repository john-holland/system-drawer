using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal root contract for travel discovery shared by ragdolls and vehicles (<see cref="BaseAmbulatingActor"/>).
/// </summary>
public interface ITravelActorRoot
{
    Transform RootTransform { get; }
}
