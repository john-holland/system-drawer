using System;
using Locomotion.Narrative;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Locomotion.Narrative.Serialization
{
    public static class NarrativeImportUtility
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static NarrativeCalendarDto ImportCalendarFromJson(string json)
        {
            return JsonConvert.DeserializeObject<NarrativeCalendarDto>(json, JsonSettings);
        }

        public static NarrativeTreeDto ImportTreeFromJson(string json)
        {
            return JsonConvert.DeserializeObject<NarrativeTreeDto>(json, JsonSettings);
        }

        public static NarrativeCalendarDto ImportCalendarFromYaml(string yaml)
        {
            return BuildYamlDeserializer().Deserialize<NarrativeCalendarDto>(yaml);
        }

        public static NarrativeTreeDto ImportTreeFromYaml(string yaml)
        {
            return BuildYamlDeserializer().Deserialize<NarrativeTreeDto>(yaml);
        }

#if UNITY_EDITOR
        public static NarrativeCalendarAsset CreateCalendarAssetFromDto(NarrativeCalendarDto dto, string assetPath)
        {
            var asset = ScriptableObject.CreateInstance<NarrativeCalendarAsset>();
            ApplyDto(asset, dto);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        public static NarrativeTreeAsset CreateTreeAssetFromDto(NarrativeTreeDto dto, string assetPath)
        {
            var asset = ScriptableObject.CreateInstance<NarrativeTreeAsset>();
            ApplyDto(asset, dto);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }
#endif

        public static void ApplyDto(NarrativeCalendarAsset calendar, NarrativeCalendarDto dto)
        {
            if (calendar == null || dto == null)
                return;

            calendar.schemaVersion = dto.schemaVersion;
            calendar.events.Clear();

            if (dto.events == null)
                return;

            for (int i = 0; i < dto.events.Count; i++)
            {
                var e = dto.events[i];
                if (e == null) continue;

                var evt = new NarrativeCalendarEvent
                {
                    id = e.id,
                    title = e.title,
                    notes = e.notes,
                    startDateTime = e.startDateTime,
                    durationSeconds = e.durationSeconds,
                    tags = e.tags ?? new System.Collections.Generic.List<string>(),
                    tree = ResolveTreeByGuid(e.treeAssetGuid)
                };

                if (e.actions != null)
                {
                    for (int a = 0; a < e.actions.Count; a++)
                    {
                        var act = FromDto(e.actions[a]);
                        if (act != null) evt.actions.Add(act);
                    }
                }

                calendar.events.Add(evt);
            }
        }

        public static void ApplyDto(NarrativeTreeAsset tree, NarrativeTreeDto dto)
        {
            if (tree == null || dto == null)
                return;

            tree.schemaVersion = dto.schemaVersion;
            tree.root = FromDto(dto.root) ?? new NarrativeSequenceNode { title = "Root" };
        }

        private static NarrativeNode FromDto(NarrativeNodeDto dto)
        {
            if (dto == null) return null;

            NarrativeNode n;
            switch (dto.type)
            {
                case nameof(NarrativeSequenceNode):
                    var seq = new NarrativeSequenceNode();
                    if (dto.children != null)
                    {
                        for (int i = 0; i < dto.children.Count; i++)
                            seq.children.Add(FromDto(dto.children[i]));
                    }
                    n = seq;
                    break;

                case nameof(NarrativeSelectorNode):
                    var sel = new NarrativeSelectorNode();
                    if (dto.children != null)
                    {
                        for (int i = 0; i < dto.children.Count; i++)
                            sel.children.Add(FromDto(dto.children[i]));
                    }
                    n = sel;
                    break;

                case nameof(NarrativeActionNode):
                default:
                    var an = new NarrativeActionNode { action = FromDto(dto.action) };
                    n = an;
                    break;
            }

            n.id = string.IsNullOrWhiteSpace(dto.id) ? Guid.NewGuid().ToString("N") : dto.id;
            n.title = string.IsNullOrWhiteSpace(dto.title) ? "Node" : dto.title;
            n.contingency = dto.contingency ?? new NarrativeContingency();
            return n;
        }

        private static NarrativeActionSpec FromDto(NarrativeActionDto dto)
        {
            if (dto == null) return null;

            NarrativeActionSpec a;
            switch (dto.type)
            {
                case nameof(SpawnPrefabAction):
                    a = new SpawnPrefabAction
                    {
                        prefab = ResolvePrefabByGuid(dto.prefabGuid),
                        parentKey = dto.parentKey,
                        localPosition = dto.localPosition,
                        localEulerAngles = dto.localEulerAngles,
                        worldSpace = dto.worldSpace
                    };
                    break;

                case nameof(SetPropertyAction):
                    a = new SetPropertyAction
                    {
                        targetKey = dto.targetKey,
                        componentTypeName = dto.componentTypeName,
                        memberName = dto.memberName,
                        value = dto.value
                    };
                    break;

                case nameof(CallMethodAction):
                    a = new CallMethodAction
                    {
                        targetKey = dto.targetKey,
                        componentTypeName = dto.componentTypeName,
                        methodName = dto.methodName,
                        args = dto.args ?? Array.Empty<NarrativeValue>()
                    };
                    break;

                case nameof(RunBehaviorTreeAction):
                    a = new RunBehaviorTreeAction
                    {
                        actorKey = dto.actorKey,
                        goal = dto.goal ?? new BehaviorTreeGoalSpec()
                    };
                    break;

                default:
                    // Unknown future action: skip.
                    return null;
            }

            a.contingency = dto.contingency ?? new NarrativeContingency();
            return a;
        }

        private static IDeserializer BuildYamlDeserializer()
        {
            return new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        private static NarrativeTreeAsset ResolveTreeByGuid(string guid)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<NarrativeTreeAsset>(path);
#else
            return null;
#endif
        }

        private static UnityEngine.GameObject ResolvePrefabByGuid(string guid)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
#else
            return null;
#endif
        }
    }
}

