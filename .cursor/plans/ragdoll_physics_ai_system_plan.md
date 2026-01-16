# Ragdoll Physics Animation AI System Plan

## Overview

Implement a sophisticated ragdoll physics animation AI system that combines:
- Ragdoll physics with muscle-based actuation
- Brain components with impulse processing
- Arbitrary behavior trees for decision making
- Physics card "good section" solvers for motion planning
- LSTM prediction layers for learning and anticipation
- World interaction and sensing

Tooling
- ragdoll skeletal bone name association service (use chatgpt possibly to fit ragdolls in the editor more easily)

## Architecture

### Core Concepts

**Physics Cards / "Good Sections"**:
- Stacks of impulse actions that transition from one physical state to another
- Define limits, connected sections, and feasibility criteria
- Ordered by applicability (degrees difference, torque, force, velocity change)
- Can be merged with procedural or keyframe animations

**Brain Components**:
- Process impulses (muscle activations, physics contacts, events)
- Filter and interpret physics card impulses
- Communicate with other brains via thoughts/messages
- Priority-based execution when multiple brains attached

**Behavior Trees**:
- Arbitrary decision trees that work with physics card solvers
- Can be pruned based on available cards
- Integrate with physics card selection

**LSTM Prediction**:
- Predict next card in sequence
- Estimate required muscle strength/activation
- Learn from successful motion sequences

## System Components

### 1. Ragdoll System

**File**: `Assets/locomotion/RagdollSystem.cs`

**Responsibilities**:
- Manage ragdoll physics structure
- Coordinate muscle activations
- Handle partial/temporary animation breaks
- Merge procedural and keyframe animations

**Key Properties**:
- `ragdollRoot` (Transform) - Root of ragdoll hierarchy
- `muscleGroups` (List<MuscleGroup>) - Organized muscle groups
- `animationBlendMode` (enum: FullRagdoll, PartialRagdoll, KeyframeOnly)
- `breakableSections` (List<RagdollSection>) - Sections that can break out of animation

**Key Methods**:
- `ActivateMuscleGroup(string groupName, float activation)`
- `BreakSection(RagdollSection section)` - Temporarily break section out of animation
- `MergeAnimation(AnimationClip clip, float blend)` - Merge with procedural
- `GetCurrentState()` - Get current ragdoll state for card matching

### 2. Muscle System

**File**: `Assets/locomotion/Muscle.cs`

**Responsibilities**:
- Individual muscle actuation
- Force/torque application to joints
- Muscle strength limits
- Activation curves

**Key Properties**:
- `attachedJoint` (ConfigurableJoint) - Joint this muscle affects
- `maxForce` (float) - Maximum force muscle can apply
- `maxTorque` (float) - Maximum torque
- `activation` (float, 0-1) - Current activation level
- `activationCurve` (AnimationCurve) - Force curve over activation

**Key Methods**:
- `Activate(float strength)` - Activate muscle with strength (0-1)
- `ApplyForce()` - Apply force to joint based on activation
- `GetCurrentForce()` - Get current force being applied

### 3. Nervous System / Impulse System

**File**: `Assets/locomotion/NervousSystem.cs`

**Responsibilities**:
- Route impulses up and down the body
- Process physics contacts
- Handle arbitrary events (heat, custom game events)
- Maintain temporal graph of good sections

**Key Properties**:
- `impulseChannels` (Dictionary<string, ImpulseChannel>) - Named impulse channels
- `goodSections` (List<GoodSection>) - Available good sections
- `temporalGraph` (TemporalGraph) - Graph of connected good sections

**Key Methods**:
- `SendImpulseDown(string channel, ImpulseData data)` - Send activation impulse
- `SendImpulseUp(string channel, ImpulseData data)` - Send sensor impulse
- `GetAvailableGoodSections(GameObject target)` - Query available sections
- `GetGoodSectionSequence(GameObject target, GoodSection goal)` - Get sequence to goal
- `RegisterGoodSection(GoodSection section)` - Register new good section

### 4. Physics Card / Good Section

**File**: `Assets/locomotion/GoodSection.cs`

**Responsibilities**:
- Define a transition between physical states
- Store impulse action stack
- Define limits and feasibility criteria
- Connect to other good sections

**Key Properties**:
- `sectionName` (string) - Name/identifier
- `impulseStack` (List<ImpulseAction>) - Stack of actions to execute
- `requiredState` (RagdollState) - Required starting state
- `targetState` (RagdollState) - Target ending state
- `limits` (SectionLimits) - Physical limits (degrees, torque, force)
- `connectedSections` (List<GoodSection>) - Sections reachable from this one
- `behaviorTree` (BehaviorTree) - Associated behavior tree (optional)

**Key Methods**:
- `IsFeasible(RagdollState currentState)` - Check if section is feasible
- `CalculateFeasibilityScore(RagdollState currentState)` - Score for ordering
- `Execute()` - Execute the impulse stack
- `GetRequiredMuscleActivations()` - Get muscle activations needed

### 5. Physics Card Solver

**File**: `Assets/locomotion/PhysicsCardSolver.cs`

**Responsibilities**:
- Find applicable good sections from current state
- Order sections by feasibility
- Perform topological sorts for physical space searches
- Match cards to behavior tree goals

**Key Methods**:
- `FindApplicableCards(RagdollState state, GameObject target)` - Find available cards
- `OrderCardsByFeasibility(List<GoodSection> cards, RagdollState state)` - Order cards
- `SolveForGoal(BehaviorTreeGoal goal, RagdollState state)` - Find card sequence for goal
- `TopologicalSearch(GoodSection start, GoodSection goal)` - Find path through graph

**Feasibility Criteria**:
- Degrees difference required
- Torque requirements
- Force requirements
- Velocity change needed
- Likelihood given force/torque ranges

### 6. Brain Component

**File**: `Assets/locomotion/Brain.cs`

**Responsibilities**:
- Process impulses and interpret physics cards
- Execute behavior trees
- Communicate with other brains
- Filter and prioritize actions

**Key Properties**:
- `priority` (int) - Execution priority (higher = more important)
- `attachedBodyPart` (GameObject) - Body part this brain is attached to
- `behaviorTree` (BehaviorTree) - Main behavior tree
- `connectedBrains` (List<Brain>) - Other brains to communicate with
- `impulseFilters` (List<ImpulseFilter>) - Filters for processing impulses

**Key Methods**:
- `ProcessImpulse(ImpulseData impulse)` - Process incoming impulse
- `SendThought(Brain target, ThoughtData thought)` - Send thought to another brain
- `ExecuteBehaviorTree()` - Run behavior tree
- `InterpretPhysicsCard(GoodSection card)` - Interpret card for behavior tree

### 7. Behavior Tree System

**File**: `Assets/locomotion/BehaviorTree.cs`

**Responsibilities**:
- Arbitrary decision trees
- Integration with physics card solvers
- Pruning based on available cards
- Goal-oriented planning

**Key Properties**:
- `rootNode` (BehaviorTreeNode) - Root of tree
- `currentGoal` (BehaviorTreeGoal) - Current active goal
- `availableCards` (List<GoodSection>) - Cards available from solver

**Key Methods**:
- `Execute()` - Execute behavior tree
- `SetGoal(BehaviorTreeGoal goal)` - Set new goal
- `PruneForCards(List<GoodSection> cards)` - Prune tree based on available cards
- `GetRequiredCards()` - Get cards needed for current goal

### 8. LSTM Prediction System

**File**: `Assets/locomotion/LSTMPredictor.cs`

**Responsibilities**:
- Predict next card in sequence
- Estimate muscle activation strength
- Learn from successful motion sequences
- Provide confidence scores

**Key Properties**:
- `model` (LSTMModel) - Trained LSTM model
- `sequenceHistory` (Queue<MotionSequence>) - Recent motion sequences
- `trainingData` (List<MotionSequence>) - Training data

**Key Methods**:
- `PredictNextCard(GoodSection currentCard, RagdollState state)` - Predict next card
- `EstimateMuscleStrength(GoodSection card, RagdollState state)` - Estimate activation
- `TrainOnSequence(MotionSequence sequence, bool success)` - Learn from sequence
- `GetConfidence()` - Get prediction confidence

**LSTM Architecture**:
- Input: Current state, current card, muscle activations, physics contacts
- Hidden layers: 2-3 LSTM layers
- Output: Next card probability distribution, muscle strength estimates

### 9. World Interaction System

**File**: `Assets/locomotion/WorldInteraction.cs`

**Responsibilities**:
- Sense world state (contacts, objects, events)
- Generate interaction impulses
- Handle arbitrary game events
- Provide context for behavior trees

**Key Properties**:
- `sensors` (List<Sensor>) - Active sensors
- `interactionRange` (float) - Range for interactions
- `eventHandlers` (Dictionary<string, EventHandler>) - Custom event handlers

**Key Methods**:
- `SenseWorld()` - Collect sensor data
- `GenerateImpulse(SensorData data)` - Generate impulse from sensor
- `HandleGameEvent(GameEvent event)` - Handle custom game event
- `GetInteractionTargets()` - Get available interaction targets

## Implementation Phases

### Phase 1: Core Ragdoll & Muscle System
1. Implement RagdollSystem with basic structure
2. Implement Muscle component with force application
3. Implement MuscleGroup organization
4. Test basic muscle activations

### Phase 2: Nervous System & Impulses
1. Implement NervousSystem with impulse routing
2. Implement ImpulseData and ImpulseChannel
3. Implement basic good section structure
4. Test impulse routing up/down

### Phase 3: Physics Cards & Good Sections
1. Implement GoodSection with impulse stacks
2. Implement RagdollState representation
3. Implement SectionLimits and feasibility checking
4. Create initial good section library (walk, jump, grab, etc.)

### Phase 4: Physics Card Solver
1. Implement PhysicsCardSolver
2. Implement feasibility scoring
3. Implement topological search
4. Test card selection and ordering

### Phase 5: Behavior Tree Integration
1. Implement BehaviorTree system
2. Integrate with PhysicsCardSolver
3. Implement goal-oriented planning
4. Test behavior tree + card solver integration

### Phase 6: Brain Component
1. Implement Brain component
2. Implement impulse processing
3. Implement brain-to-brain communication
4. Test multi-brain coordination

### Phase 7: LSTM Prediction
1. Implement LSTM model structure
2. Implement training system
3. Implement prediction interface
4. Train initial model on motion data

### Phase 8: World Interaction
1. Implement sensor system
2. Implement world sensing
3. Implement event handling
4. Test full interaction loop

### Phase 9: Animation Integration
1. Implement animation blending
2. Implement partial ragdoll breaks
3. Implement procedural + keyframe merging
4. Test animation system

### Phase 10: Advanced Features
1. Adoption timeouts for stuck states
2. Card game mechanics (if desired)
3. Vehicle/object interaction (cars, bicycles)
4. Structure deformation integration

## Technical Details

### RagdollState Representation

```csharp
public struct RagdollState
{
    public Dictionary<string, JointState> jointStates; // Joint angles, velocities
    public Dictionary<string, float> muscleActivations; // Current muscle activations
    public Vector3 rootPosition;
    public Quaternion rootRotation;
    public Vector3 rootVelocity;
    public Vector3 rootAngularVelocity;
    public List<ContactPoint> contacts; // Physics contacts
}
```

### ImpulseAction Structure

```csharp
public struct ImpulseAction
{
    public string muscleGroup; // Which muscle group to activate
    public float activation; // Activation strength (0-1)
    public float duration; // How long to maintain activation
    public AnimationCurve curve; // Activation curve over duration
    public ImpulseCondition[] conditions; // Conditions to check
}
```

### Good Section Feasibility Scoring

```csharp
public float CalculateFeasibilityScore(RagdollState currentState)
{
    float score = 0f;
    
    // Degrees difference (lower is better)
    float degreesDiff = CalculateDegreesDifference(currentState, requiredState);
    score += (1f - Mathf.Clamp01(degreesDiff / 180f)) * 0.3f;
    
    // Torque feasibility (within limits)
    float torqueFeasibility = CheckTorqueFeasibility(currentState);
    score += torqueFeasibility * 0.3f;
    
    // Force feasibility
    float forceFeasibility = CheckForceFeasibility(currentState);
    score += forceFeasibility * 0.2f;
    
    // Velocity change likelihood
    float velocityLikelihood = EstimateVelocityChangeLikelihood(currentState);
    score += velocityLikelihood * 0.2f;
    
    return score;
}
```

### LSTM Input/Output

**Input Features**:
- Current ragdoll state (normalized)
- Current good section ID
- Recent muscle activations (last N frames)
- Physics contacts
- Goal state

**Output**:
- Next card probability distribution
- Muscle strength estimates per muscle group
- Confidence scores

## Integration Points

### With Weather System
- Weather events can trigger impulses (wind forces, temperature)
- Behavior trees can react to weather conditions
- Physics cards can include weather-affected motions

### With BedogaGenerator
- Use spatial generation for world layout
- Behavior trees can navigate generated spaces
- Good sections can adapt to generated geometry

## Testing & Validation

### Unit Tests
- Muscle activation force application
- Impulse routing correctness
- Good section feasibility scoring
- Card solver ordering

### Integration Tests
- Full motion sequence execution
- Behavior tree + card solver integration
- Multi-brain coordination
- LSTM prediction accuracy

### Performance Tests
- Ragdoll physics performance
- Card solver search performance
- LSTM inference speed
- Multi-character scenarios

## Future Enhancements

- Card game mechanics (poker-style with physics cards)
- Vehicle systems (cars, bicycles with individual components)
- Structure deformation (barns, buildings reacting to weather)
- Multi-character coordination
- Advanced facial animation (from head.cs notes)
- Hand articulation system (from hand.cs notes)
