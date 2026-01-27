# Networking Architecture for Ragdoll Physics Systems

## Overview

This document explores networking implications for our complex physics-based systems, discusses implementation of Glenn Fiedler's networked physics model, and proposes mathematical factoring strategies to make our systems both communicative (network-transmittable) and associative (locally composable) for networked layer overlays.

## System Architecture Context

Our codebase contains several interconnected systems that must coordinate in a networked environment:

1. **Ragdoll Physics System** - Hierarchical body parts with musculature, joint constraints, and procedural animation
2. **Nervous System** - Impulse routing, temporal graphs, card-based action solving
3. **Narrative System** - Time-based event scheduling and execution
4. **Weather System** - Multi-stage physics simulation with ordered service updates
5. **Spatial Trees** - QuadTree/OctTree for spatial partitioning and object relationships

## Networking Implications

### Key Challenges

#### 1. **Physics Determinism & Authority**

Physics simulations must remain deterministic across clients while handling network latency. Key considerations:

- **State Synchronization**: Ragdoll joint states, muscle activations, and contact points must be synchronized
- **Authority Model**: Who owns what? Each client owns their own character, but must see others accurately
- **Prediction vs Correction**: Client-side prediction must reconcile with server authority

**Implications for Our Systems:**
- `RagdollSystem.GetCurrentState()` returns a complete state snapshot - good for networking
- Muscle activations and joint states are already tracked in dictionaries
- Physics contacts need deterministic serialization

#### 2. **Temporal Consistency**

The Nervous System's temporal graph and Narrative System's time-based events require synchronized time:

- **Server Time Authority**: All narrative events and temporal graph nodes need authoritative time
- **Latency Compensation**: Weather system updates must account for network delay
- **Event Ordering**: Narrative calendar events must execute in the same order on all clients

**Implications:**
- `NarrativeClock` needs network synchronization
- `NarrativeScheduler` requires deterministic event ordering
- Weather system's `ServiceUpdate` order must be preserved across network

#### 3. **Spatial Partitioning & Relevance**

Spatial trees (QuadTree/OctTree) help determine what to network:

- **Interest Management**: Only send updates for entities in relevant spatial regions
- **Level of Detail**: Coarser updates for distant objects
- **Hierarchical State**: Send parent transforms, let clients compute children

**Implications:**
- Our spatial tree structure maps naturally to interest management zones
- Object relationships in spatial trees define entity graph for networking

#### 4. **Action Card System & Bandwidth**

The Physics Card Solver system generates many potential actions that can't all be networked:

- **Card Selection**: Only transmit selected/final cards, not the entire possibility space
- **Intent Transmission**: Send high-level intents, let each client solve locally
- **Validation**: Networked validation of card feasibility before execution

**Implications:**
- `Consider` component generates cards locally - don't network generation process
- Network only the selected `GoodSection` cards for execution
- Each client can generate their own card pool for prediction

## Glenn Fiedler's Networked Physics Model

### Core Principles

Glenn Fiedler's approach (detailed in "Building a Game Network Protocol") emphasizes:

1. **Deterministic Lockstep with Rollback** - All clients simulate the same physics, server corrects deviations
2. **Client-Side Prediction** - Clients immediately respond to input, server reconciles later
3. **Entity Trading** - Client physics interaction cause entities to switch over to interacting client control,
                         other clients receive interpolation position rather than rendering client side.
3. **Entity Interpolation** - Smooth rendering of networked entities between updates
4. **Snapshot Compression** - Efficient encoding of world state

### Implementation Strategy for Our Systems

#### 1. **Deterministic Physics State**

Transform our systems to use deterministic, serializable state:

```csharp
// Deterministic Ragdoll State
public struct NetworkedRagdollState
{
    public uint frame;                    // Frame number for determinism
    public Vector3 rootPosition;          // Quantized position
    public Quaternion rootRotation;       // Compressed rotation
    public Vector3 rootVelocity;          // Quantized velocity
    public Vector3 rootAngularVelocity;   // Quantized angular velocity
    public Dictionary<uint, uint[]> behaviorTreeNodeToCards = new Dictionary<uint, uint[]>();
    public Dictionary<uint, uint[]> cardToBehaviorTreeNodes = new Dictionary<uint, uint[]>();

    // Joint states (compressed)
    public NetworkedJointState[] joints;
    public byte[] muscleActivations;      // Normalized 0-255
    
    // Contact points (limited count for bandwidth)
    public NetworkedContact[] contacts;
}

// Deterministic Weather State
public struct NetworkedWeatherState
{
    public uint frame;
    public float temperature;             // Quantized
    public float pressure;
    public float humidity;
    public Vector3 windField;             // Quantized vector
    public float precipitationRate;
}
```

**Key Design:**
- Use frame numbers for temporal ordering
- Quantize floating-point values (reduce precision for bandwidth)
- Limit contact/event arrays to reduce payload size
- Use delta compression (only send changed state)

#### 2. **Client-Side Prediction**

Extend our systems to support prediction and reconciliation:

```csharp
// Extend RagdollSystem for prediction
public class RagdollSystem : MonoBehaviour
{
    bool useTreeAgreement = true;
    private Queue<RagdollInput> inputQueue = new Queue<RagdollInput>();
    private Queue<NetworkedRagdollState> stateHistory = new Queue<NetworkedRagdollState>();
    
    // Store predicted state
    public void PredictState(RagdollInput input, float deltaTime)
    {
        // Apply input locally
        ApplyInput(input);
        
        // Simulate physics
        UpdatePhysics(deltaTime);
        
        // Store for rollback
        var predictedState = GetCurrentState();
        stateHistory.Enqueue(predictedState);
    }

    public bool CardsOrTreesConflict(
        NetworkedRagdollState authoritativeState, 
        NetworkedRagdollState predictedState) {
        // if any of the cards from the predictedState 
        return authoritativeState.cardToBehaviorTreeNodes.
    }
    
    // Reconcile with authoritative state
    public void ReconcileState(NetworkedRagdollState authoritativeState)
    {
        // Find matching frame
        while (stateHistory.Count > 0)
        {
            var predicted = stateHistory.Peek();
            if (predicted.frame == authoritativeState.frame)
            {
                // Compare and correct if needed
                if (!StatesMatch(predicted, authoritativeState))
                {
                    // allow client side processing of local non-gameplay graph effecting play
                    // provided your tree logic and cards are lockstep
                    // switches cognitive and processing loads, and may not be good for competitive FPS
                    if (useTreeAgreement && !CardsOrTreesConflict(authoritativeState, predicted)) {
                        continue;
                    }

                    // Rollback and resimulate
                    RollbackTo(authoritativeState);
                }
                break;
            }
            stateHistory.Dequeue();
        }
    }
}
```

**Challenges for Our Systems:**
- Nervous System's temporal graph must support rollback
- Card solver decisions must be deterministic
- Weather system must use fixed timestep for determinism

#### 3. **Entity Interpolation**

For smooth rendering of networked entities:

```csharp
// Interpolation component for networked ragdolls
public class NetworkedRagdollInterpolator : MonoBehaviour
{
    private CircularBuffer<NetworkedRagdollState> stateBuffer;
    private float renderTime; // Slightly behind authoritative time
    
    void Update()
    {
        // Interpolate between two states
        var stateA = stateBuffer.GetStateAt(renderTime - interpolationDelay);
        var stateB = stateBuffer.GetStateAt(renderTime - interpolationDelay + 1);
        
        float t = CalculateInterpolationFactor(stateA, stateB);
        
        // Smoothly interpolate position, rotation, etc.
        transform.position = Vector3.Lerp(stateA.rootPosition, stateB.rootPosition, t);
        transform.rotation = Quaternion.Slerp(stateA.rootRotation, stateB.rootRotation, t);
        
        // Interpolate joint states
        InterpolateJoints(stateA.joints, stateB.joints, t);
    }
}
```

#### 4. **Snapshot System**

Implement efficient world state snapshots:

```csharp
// World snapshot for networked physics
public class NetworkedWorldSnapshot
{
    public uint frame;
    public float serverTime;
    
    // Ragdoll entities (interest-filtered)
    public Dictionary<uint, NetworkedRagdollState> ragdolls;
    
    // Weather state (shared, one per world)
    public NetworkedWeatherState weather;
    
    // Narrative events (if relevant to physics)
    public List<NetworkedNarrativeEvent> activeEvents;
    
    // Compressed snapshot data
    public byte[] Serialize()
    {
        // Use delta compression, bit packing, etc.
        // Glenn Fiedler's article suggests delta encoding with Huffman
    }
}
```

**Interest Management Integration:**
- Use our QuadTree/OctTree to determine which ragdolls to include
- Only send ragdolls within relevance distance
- Use hierarchical encoding (parent transforms, then children)

## Mathematical Factoring for Communicative & Associative Systems

### Communicative Operations

A **communicative** operation can be performed in any order and produces the same result. This is crucial for networked systems where messages may arrive out of order.

#### Current Systems Analysis

**Weather System - NOT Fully Communicative:**
```csharp
// Current order matters:
meteorology.ServiceUpdate(deltaTime);  // Must run first
wind.ServiceUpdate(deltaTime);         // Depends on meteorology
precipitation.ServiceUpdate(deltaTime); // Depends on wind
```

**Solution: Dependency Graph with Communicative Sub-operations**

Refactor to identify commutative sub-operations:

```csharp
// Communicative weather state update
public struct WeatherDeltas
{
    // These can be applied in any order
    public float deltaTemperature;    // Communicative: a+b = b+a
    public float deltaPressure;       // Communicative
    public float deltaHumidity;       // Communicative
    public Vector3 deltaWind;         // Communicative: vector addition
    
    // Combine deltas commutatively
    public static WeatherDeltas Combine(WeatherDeltas a, WeatherDeltas b)
    {
        return new WeatherDeltas
        {
            deltaTemperature = a.deltaTemperature + b.deltaTemperature,
            deltaPressure = a.deltaPressure + b.deltaPressure,
            deltaHumidity = a.deltaHumidity + b.deltaHumidity,
            deltaWind = a.deltaWind + b.deltaWind
        };
    }
}
```

**Narrative System - Partially Communicative:**
```csharp
// Events can be processed in different orders if independent
// Use dependency tracking for commutative operations

public class NarrativeEvent
{
    public string id;
    public List<string> dependsOn; // Dependencies for ordering
    public NarrativeDeltas deltas; // Communicative state changes
    
    // Two events are commutative if they don't depend on each other
    public bool IsCommutativeWith(NarrativeEvent other)
    {
        return !dependsOn.Contains(other.id) && 
               !other.dependsOn.Contains(id);
    }
}
```

#### Ragdoll System - Vector Operations Are Communicative

Physics forces and impulses can be combined commutatively:

```csharp
// Muscle activations combine commutatively
public struct MuscleActivation
{
    public string muscleName;
    public float activation; // 0-1
    
    // Combining activations: commutative
    public static MuscleActivation Combine(MuscleActivation a, MuscleActivation b)
    {
        if (a.muscleName != b.muscleName) 
            throw new ArgumentException("Can only combine same muscle");
        
        // Max or additive based on system design
        return new MuscleActivation
        {
            muscleName = a.muscleName,
            activation = Mathf.Clamp01(a.activation + b.activation)
        };
    }
}

// Force vectors combine commutatively
public struct ForceImpulse
{
    public Vector3 force;        // Communicative: vector addition
    public Vector3 torque;       // Communicative: vector addition
    public Vector3 position;     // Reference point
    
    public static ForceImpulse Combine(ForceImpulse a, ForceImpulse b)
    {
        return new ForceImpulse
        {
            force = a.force + b.force,
            torque = a.torque + b.torque,
            position = Vector3.Lerp(a.position, b.position, 0.5f) // Weighted average
        };
    }
}
```

### Associative Operations

An **associative** operation can be grouped differently: `(a ∘ b) ∘ c = a ∘ (b ∘ c)`. This allows efficient local computation and network aggregation.

#### Composition Strategy

**Weather System - Associative Aggregation:**

```csharp
// Weather state is associative under composition
public struct WeatherState
{
    public float temperature;
    public float pressure;
    // ... other fields
    
    // Associative composition: (A ∘ B) ∘ C = A ∘ (B ∘ C)
    public static WeatherState Compose(WeatherState a, WeatherState b)
    {
        // Weighted average or other associative operation
        float weightA = a.validity;
        float weightB = b.validity;
        float totalWeight = weightA + weightB;
        
        return new WeatherState
        {
            temperature = (a.temperature * weightA + b.temperature * weightB) / totalWeight,
            pressure = (a.pressure * weightA + b.pressure * weightB) / totalWeight,
            validity = totalWeight
        };
    }
}

// Local nodes compute partial states, network aggregates
public class WeatherNode
{
    private WeatherState localState;
    
    // Local computation (associative)
    public WeatherState ComputeLocal(WeatherEvent evt)
    {
        // Process event locally
        return ProcessEvent(evt);
    }
    
    // Network aggregation (associative)
    public WeatherState Aggregate(WeatherState[] remoteStates)
    {
        // Can aggregate in any order due to associativity
        WeatherState result = localState;
        foreach (var state in remoteStates)
        {
            result = WeatherState.Compose(result, state);
        }
        return result;
    }
}
```

**Ragdoll System - Associative Force Aggregation:**

```csharp
// Joint constraints are associative under composition
public struct JointConstraintState
{
    public Quaternion targetRotation;
    public float stiffness;
    public float damping;
    
    // Associative: combining constraints
    public static JointConstraintState Compose(JointConstraintState a, JointConstraintState b)
    {
        // Weighted slerp for rotations (associative in practice)
        float weight = a.stiffness / (a.stiffness + b.stiffness);
        
        return new JointConstraintState
        {
            targetRotation = Quaternion.Slerp(a.targetRotation, b.targetRotation, weight),
            stiffness = a.stiffness + b.stiffness, // Associative addition
            damping = Mathf.Max(a.damping, b.damping) // Max is associative
        };
    }
}
```

#### Temporal Graph - Associative Path Composition

The Nervous System's temporal graph can use associative path composition:

```csharp
// Temporal graph paths are associative
public struct TemporalPath
{
    public List<ImpulseNode> nodes;
    public float weight;
    public float timestamp;
    
    // Path composition is associative: (A→B)→C = A→(B→C)
    public static TemporalPath Compose(TemporalPath a, TemporalPath b)
    {
        if (a.nodes.Last() != b.nodes.First())
            throw new ArgumentException("Paths must connect");
        
        return new TemporalPath
        {
            nodes = a.nodes.Concat(b.nodes.Skip(1)).ToList(),
            weight = a.weight * b.weight, // Associative multiplication
            timestamp = Mathf.Max(a.timestamp, b.timestamp)
        };
    }
}

// Networked temporal graph can aggregate paths associatively
public class NetworkedTemporalGraph
{
    // Local graph computation
    public TemporalPath[] ComputeLocalPaths(ImpulseNode start, ImpulseNode end)
    {
        // Find all local paths
        return FindPaths(start, end);
    }
    
    // Aggregate with remote paths (associative)
    public TemporalPath[] AggregatePaths(TemporalPath[] local, TemporalPath[] remote)
    {
        // Combine and deduplicate - order doesn't matter (associative)
        return local.Concat(remote)
            .GroupBy(p => p.nodes)
            .Select(g => g.OrderByDescending(p => p.weight).First())
            .ToArray();
    }
}
```

### Mathematical Formalism

#### Monoid Structure

Both communicative and associative operations form a **monoid**, which is ideal for distributed systems:

```csharp
// Monoid interface for network-compatible operations
public interface IMonoid<T>
{
    T Identity { get; }                    // Identity element
    T Combine(T a, T b);                   // Associative operation
    bool IsCommutative { get; }            // Optional: commutative property
}

// Example: Ragdoll state monoid
public struct RagdollStateMonoid : IMonoid<NetworkedRagdollState>
{
    public NetworkedRagdollState Identity => default;
    
    public NetworkedRagdollState Combine(NetworkedRagdollState a, NetworkedRagdollState b)
    {
        // Combine states associatively
        return new NetworkedRagdollState
        {
            frame = Mathf.Max(a.frame, b.frame), // Latest frame
            rootPosition = Vector3.Lerp(a.rootPosition, b.rootPosition, 0.5f),
            // ... combine other fields associatively
        };
    }
    
    public bool IsCommutative => false; // Order matters for frame numbers
}
```

#### Network Layer Overlay

With monoid operations, we can create networked layers:

```csharp
// Network layer that aggregates local and remote states
public class NetworkedLayer<T> where T : IMonoid<T>
{
    private IMonoid<T> monoid;
    private T localState;
    private Dictionary<uint, T> remoteStates = new Dictionary<uint, T>();
    
    // Update local state
    public void UpdateLocal(T newState)
    {
        localState = newState;
    }
    
    // Receive remote state
    public void ReceiveRemote(uint clientId, T remoteState)
    {
        remoteStates[clientId] = remoteState;
    }
    
    // Get aggregated state (associative composition)
    public T GetAggregatedState()
    {
        T result = localState;
        foreach (var remote in remoteStates.Values)
        {
            result = monoid.Combine(result, remote);
        }
        return result;
    }
    
    // Order doesn't matter due to associativity
    // Can process remote updates in any order
}
```

## Entity Relationships & Object Paths

### Hierarchical Entity Graph

Our systems already have hierarchical structures that map to entity graphs:

```csharp
// Entity relationship structure for networking
public class NetworkedEntity
{
    public uint entityId;
    public uint parentId;          // Hierarchical relationship
    public EntityType type;
    public Transform transform;
    
    // Object path (hierarchical identifier)
    public string GetObjectPath()
    {
        // Build path from root: "ragdoll/pelvis/torso/neck/head"
        List<string> path = new List<string>();
        NetworkedEntity current = this;
        
        while (current != null)
        {
            path.Insert(0, current.type.ToString().ToLower());
            current = GetParent(current);
        }
        
        return string.Join("/", path);
    }
}

// Entity relationship graph
public class EntityGraph
{
    private Dictionary<uint, NetworkedEntity> entities = new Dictionary<uint, NetworkedEntity>();
    private Dictionary<uint, List<uint>> children = new Dictionary<uint, List<uint>>();
    
    // Add entity with parent relationship
    public void AddEntity(NetworkedEntity entity, uint? parentId = null)
    {
        entities[entity.entityId] = entity;
        
        if (parentId.HasValue)
        {
            if (!children.ContainsKey(parentId.Value))
                children[parentId.Value] = new List<uint>();
            children[parentId.Value].Add(entity.entityId);
        }
    }
    
    // Get all descendants (for hierarchical updates)
    public List<uint> GetDescendants(uint entityId)
    {
        List<uint> result = new List<uint>();
        if (children.ContainsKey(entityId))
        {
            foreach (var childId in children[entityId])
            {
                result.Add(childId);
                result.AddRange(GetDescendants(childId));
            }
        }
        return result;
    }
}
```

### Passing Object Paths Between Layers

Object paths enable efficient networking by encoding hierarchical relationships:

```csharp
// Object path protocol for networked layers
public struct ObjectPath
{
    public string[] segments;      // ["ragdoll", "pelvis", "torso"]
    public uint rootEntityId;      // Root entity identifier
    
    // Serialize path efficiently
    public byte[] Serialize()
    {
        // Use string table for common paths
        // Encode as: [rootId:4 bytes][segmentCount:1 byte][segmentIds...]
        // Segments reference shared string table
    }
    
    // Deserialize and resolve to entity
    public NetworkedEntity Resolve(EntityGraph graph)
    {
        NetworkedEntity current = graph.GetEntity(rootEntityId);
        
        foreach (var segment in segments)
        {
            current = current.GetChild(segment);
            if (current == null) return null;
        }
        
        return current;
    }
}

// Network layer that passes object paths
public class ObjectPathLayer
{
    private EntityGraph entityGraph;
    private Dictionary<string, uint> pathToEntityId = new Dictionary<string, uint>();
    
    // Send state update via object path
    public void SendStateUpdate(ObjectPath path, NetworkedRagdollState state)
    {
        // Resolve path to entity
        var entity = path.Resolve(entityGraph);
        if (entity == null) return;
        
        // Serialize and send
        var packet = new NetworkPacket
        {
            entityPath = path,
            state = state
        };
        
        Network.Send(packet.Serialize());
    }
    
    // Receive and apply state update
    public void ReceiveStateUpdate(NetworkPacket packet)
    {
        // Resolve path
        var entity = packet.entityPath.Resolve(entityGraph);
        if (entity == null) return;
        
        // Apply state
        entity.ApplyState(packet.state);
    }
}
```

### Spatial Tree Integration

Our QuadTree/OctTree structure maps directly to entity spatial relationships:

```csharp
// Spatial tree node as networked entity container
public class SpatialTreeNode : NetworkedEntityContainer
{
    public Bounds bounds;
    public List<uint> entityIds;          // Entities in this region
    public SpatialTreeNode[] children;     // Sub-regions
    
    // Get object paths for all entities in region
    public List<ObjectPath> GetEntityPaths(EntityGraph graph)
    {
        List<ObjectPath> paths = new List<ObjectPath>();
        
        foreach (var entityId in entityIds)
        {
            var entity = graph.GetEntity(entityId);
            if (entity != null)
            {
                paths.Add(entity.GetObjectPath());
            }
        }
        
        return paths;
    }
    
    // Interest management: only send entities in relevant regions
    public List<ObjectPath> GetRelevantPaths(Vector3 observerPosition, float maxDistance)
    {
        // Only include entities within distance
        var relevant = entityIds
            .Where(id => {
                var entity = graph.GetEntity(id);
                if (entity == null) return false;
                float distance = Vector3.Distance(observerPosition, entity.transform.position);
                return distance <= maxDistance;
            })
            .Select(id => graph.GetEntity(id).GetObjectPath())
            .ToList();
        
        return relevant;
    }
}
```

## Implementation Roadmap

### Phase 1: Deterministic State Serialization
1. Create `NetworkedRagdollState` struct with quantization
2. Implement `NetworkedWeatherState` serialization
3. Add frame numbers to all state structures
4. Create delta compression utilities

### Phase 2: Client-Side Prediction
1. Extend `RagdollSystem` with prediction support
2. Implement state history and rollback
3. Add input queue for buffering
4. Create reconciliation system

### Phase 3: Monoid Factoring
1. Refactor weather system to use `WeatherDeltas` (commutative)
2. Create `IMonoid<T>` interface
3. Implement associative operations for ragdoll forces
4. Refactor temporal graph for associative path composition

### Phase 4: Network Layer Overlay
1. Create `NetworkedLayer<T>` class
2. Implement entity graph system
3. Add object path serialization
4. Integrate with spatial trees for interest management

### Phase 5: Integration
1. Connect all systems through networked layers
2. Implement snapshot system
3. Add interpolation for smooth rendering
4. Performance optimization and bandwidth reduction

## Conclusion

By factoring our systems mathematically as communicative and associative operations (monoids), we enable:

1. **Efficient Network Aggregation** - States can be combined in any order
2. **Parallel Processing** - Local computation can happen independently
3. **Graceful Degradation** - Missing remote updates don't break the system
4. **Bandwidth Optimization** - Only deltas need to be transmitted
5. **Deterministic Reconciliation** - Server corrections can be applied associatively

The hierarchical nature of our ragdoll system and spatial trees naturally maps to entity relationship graphs, enabling efficient object path-based networking. Glenn Fiedler's techniques provide the foundation for deterministic physics networking, which we extend with mathematical factoring for distributed system compatibility.

### Questions

Photon integration, or another asset to try?