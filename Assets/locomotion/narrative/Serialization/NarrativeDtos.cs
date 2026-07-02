using System;
using System.Collections.Generic;

namespace Locomotion.Narrative.Serialization
{
    [Serializable]
    public class NarrativeCalendarDto
    {
        public int schemaVersion = 1;
        public List<NarrativeCalendarEventDto> events = new List<NarrativeCalendarEventDto>();
    }

    [Serializable]
    public class NarrativeCalendarEventDto
    {
        public string id;
        public string title;
        public string notes;
        public Locomotion.Narrative.NarrativeDateTime startDateTime;
        public int durationSeconds;
        public List<string> tags = new List<string>();

        public string treeAssetGuid;
        public List<NarrativeActionDto> actions = new List<NarrativeActionDto>();
    }

    [Serializable]
    public class NarrativeTreeDto
    {
        public int schemaVersion = 1;
        public string rootAssetGuid;
        public NarrativeNodeDto root;
    }

    [Serializable]
    public class NarrativeNodeDto
    {
        public string type;
        public string id;
        public string title;
        public Locomotion.Narrative.NarrativeContingency contingency;

        public List<NarrativeNodeDto> children;
        public NarrativeActionDto action;
    }

    [Serializable]
    public class NarrativeActionDto
    {
        public string type;
        public Locomotion.Narrative.NarrativeContingency contingency;

        // Common/union payload
        public string targetKey;
        public string componentTypeName;
        public string memberName;
        public string methodName;
        public Locomotion.Narrative.NarrativeValue value;
        public Locomotion.Narrative.NarrativeValue[] args;

        // Prefab / tree links
        public string prefabGuid;

        // Spawn extras
        public string parentKey;
        public UnityEngine.Vector3 localPosition;
        public UnityEngine.Vector3 localEulerAngles;
        public bool worldSpace;

        // BT goal
        public string actorKey;
        public Locomotion.Narrative.BehaviorTreeGoalSpec goal;

        // Lighting action
        public string lightingContextKey;
        public bool fallbackFindAny;
        public bool requireValidity;
        public float minValidityScore;
        public bool preferInferredDirection;
        public bool applyDirectionalLight;
        public float sunAzimuthDeg;
        public float sunElevationDeg;
        public bool sunVisible;
        public float sunDirectionConfidence;
        public string sunDirectionSource;
        public float moonAzimuthDeg;
        public float moonElevationDeg;
        public UnityEngine.Vector3 moonDirectionVectorWorld;
        public float moonDirectionConfidence;
        public string moonDirectionSource;
        public float moonIlluminationFraction;
        public bool moonVisible;
        public UnityEngine.Vector3 inferredSunDirectionVector;
        public float inferredSunDirectionConfidence;
        public float lightingValidityScore;
        public string lightingValidationFlags;
        public string weatherProvider;
        public float cloudCoverPct;
        public float visibilityM;
        public float precipitationMm;
        public float windSpeedMps;
        public int year;
        public int month;
        public int day;
        public int hour;
        public int minute;
        public int second;

        // Galactic night-sky prebake hints
        public bool starVisibility;
        public bool planetoidVisibility;
        public string galacticObserverBodyId;
        public string galacticTargetBodyId;

        // SendThought brain messages (union-style optional fields)
        public string brainSenderKey;
        public string brainReceiverKey;
        public string brainThoughtType;
        public string brainDecisionGoalName;
        public float brainDecisionConviction = 0.5f;
        public string brainSemanticTagsCsv;
        public string brainQueryId;
        public int brainQueryChannels = -1;
    }

    /// <summary>Calendar DTO for LSTM training; includes 4D spatiotemporal volume per event.</summary>
    [Serializable]
    public class NarrativeCalendarTrainingDto
    {
        public int schemaVersion = 1;
        public List<NarrativeCalendarEventTrainingDto> events = new List<NarrativeCalendarEventTrainingDto>();
    }

    [Serializable]
    public class NarrativeCalendarEventTrainingDto
    {
        public string id;
        public string title;
        public string notes;
        public Locomotion.Narrative.NarrativeDateTime startDateTime;
        public int durationSeconds;
        public List<string> tags = new List<string>();
        public string treeAssetGuid;
        public List<NarrativeActionDto> actions = new List<NarrativeActionDto>();
        /// <summary>4D volume for training: center (x,y,z), size (x,y,z), tMin, tMax. Omitted if event has no spatiotemporalVolume.</summary>
        public float? centerX, centerY, centerZ, sizeX, sizeY, sizeZ, tMin, tMax;
    }
}

