using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using Newtonsoft.Json;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Locomotion.Narrative.Serialization
{
    public static class NarrativeExportUtility
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string ExportCalendarToJson(NarrativeCalendarAsset calendar)
        {
            var dto = ToDto(calendar);
            return JsonConvert.SerializeObject(dto, JsonSettings);
        }

        public static string ExportTreeToJson(NarrativeTreeAsset tree)
        {
            var dto = ToDto(tree);
            return JsonConvert.SerializeObject(dto, JsonSettings);
        }

        public static string ExportCalendarToYaml(NarrativeCalendarAsset calendar)
        {
            var dto = ToDto(calendar);
            return BuildYamlSerializer().Serialize(dto);
        }

        public static string ExportTreeToYaml(NarrativeTreeAsset tree)
        {
            var dto = ToDto(tree);
            return BuildYamlSerializer().Serialize(dto);
        }

#if UNITY_EDITOR
        public static void ExportCalendarToJsonFile(NarrativeCalendarAsset calendar, string path)
        {
            System.IO.File.WriteAllText(path, ExportCalendarToJson(calendar));
            AssetDatabase.Refresh();
        }

        public static void ExportCalendarToYamlFile(NarrativeCalendarAsset calendar, string path)
        {
            System.IO.File.WriteAllText(path, ExportCalendarToYaml(calendar));
            AssetDatabase.Refresh();
        }

        public static void ExportTreeToJsonFile(NarrativeTreeAsset tree, string path)
        {
            System.IO.File.WriteAllText(path, ExportTreeToJson(tree));
            AssetDatabase.Refresh();
        }

        public static void ExportTreeToYamlFile(NarrativeTreeAsset tree, string path)
        {
            System.IO.File.WriteAllText(path, ExportTreeToYaml(tree));
            AssetDatabase.Refresh();
        }
#endif

        private static ISerializer BuildYamlSerializer()
        {
            return new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
                .Build();
        }

        private static NarrativeCalendarDto ToDto(NarrativeCalendarAsset calendar)
        {
            var dto = new NarrativeCalendarDto
            {
                schemaVersion = calendar != null ? calendar.schemaVersion : 1
            };

            if (calendar == null || calendar.events == null)
                return dto;

            for (int i = 0; i < calendar.events.Count; i++)
            {
                var e = calendar.events[i];
                if (e == null) continue;

                var ed = new NarrativeCalendarEventDto
                {
                    id = e.id,
                    title = e.title,
                    notes = e.notes,
                    startDateTime = e.startDateTime,
                    durationSeconds = e.durationSeconds,
                    tags = e.tags ?? new List<string>(),
                    treeAssetGuid = AssetGuid(e.tree)
                };

                if (e.actions != null)
                {
                    for (int a = 0; a < e.actions.Count; a++)
                    {
                        var act = e.actions[a];
                        var ad = ToDto(act);
                        if (ad != null) ed.actions.Add(ad);
                    }
                }

                dto.events.Add(ed);
            }

            return dto;
        }

        private static NarrativeTreeDto ToDto(NarrativeTreeAsset tree)
        {
            return new NarrativeTreeDto
            {
                schemaVersion = tree != null ? tree.schemaVersion : 1,
                rootAssetGuid = AssetGuid(tree),
                root = tree != null ? ToDto(tree.root) : null
            };
        }

        private static NarrativeNodeDto ToDto(NarrativeNode node)
        {
            if (node == null) return null;

            var dto = new NarrativeNodeDto
            {
                id = node.id,
                title = node.title,
                contingency = node.contingency
            };

            switch (node.NodeType)
            {
                case NarrativeNodeType.Sequence:
                    dto.type = nameof(NarrativeSequenceNode);
                    dto.children = new List<NarrativeNodeDto>();
                    foreach (var c in ((NarrativeSequenceNode)node).children)
                        dto.children.Add(ToDto(c));
                    break;

                case NarrativeNodeType.Selector:
                    dto.type = nameof(NarrativeSelectorNode);
                    dto.children = new List<NarrativeNodeDto>();
                    foreach (var c in ((NarrativeSelectorNode)node).children)
                        dto.children.Add(ToDto(c));
                    break;

                case NarrativeNodeType.Action:
                    dto.type = nameof(NarrativeActionNode);
                    dto.action = ToDto(((NarrativeActionNode)node).action);
                    break;
            }

            return dto;
        }

        private static NarrativeActionDto ToDto(NarrativeActionSpec action)
        {
            if (action == null) return null;

            if (action is SpawnPrefabAction sp)
            {
                return new NarrativeActionDto
                {
                    type = nameof(SpawnPrefabAction),
                    contingency = sp.contingency,
                    prefabGuid = AssetGuid(sp.prefab),
                    parentKey = sp.parentKey,
                    localPosition = sp.localPosition,
                    localEulerAngles = sp.localEulerAngles,
                    worldSpace = sp.worldSpace
                };
            }

            if (action is SetPropertyAction set)
            {
                return new NarrativeActionDto
                {
                    type = nameof(SetPropertyAction),
                    contingency = set.contingency,
                    targetKey = set.targetKey,
                    componentTypeName = set.componentTypeName,
                    memberName = set.memberName,
                    value = set.value
                };
            }

            if (action is CallMethodAction call)
            {
                return new NarrativeActionDto
                {
                    type = nameof(CallMethodAction),
                    contingency = call.contingency,
                    targetKey = call.targetKey,
                    componentTypeName = call.componentTypeName,
                    methodName = call.methodName,
                    args = call.args
                };
            }

            if (action is RunBehaviorTreeAction run)
            {
                return new NarrativeActionDto
                {
                    type = nameof(RunBehaviorTreeAction),
                    contingency = run.contingency,
                    actorKey = run.actorKey,
                    goal = run.goal
                };
            }

            // Unknown action type (future-proof): write only its type name.
            return new NarrativeActionDto
            {
                type = action.GetType().Name,
                contingency = action.contingency
            };
        }

        private static string AssetGuid(UnityEngine.Object asset)
        {
#if UNITY_EDITOR
            if (asset == null) return null;
            
            // For MonoBehaviour components, get the prefab path
            if (asset is MonoBehaviour mb)
            {
                // Check if it's a prefab asset
                if (PrefabUtility.IsPartOfPrefabAsset(mb))
                {
                    string path = AssetDatabase.GetAssetPath(mb);
                    if (!string.IsNullOrEmpty(path))
                        return AssetDatabase.AssetPathToGUID(path);
                }
                // Check if it's a prefab instance
                else if (PrefabUtility.IsPartOfPrefabInstance(mb))
                {
                    GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(mb.gameObject);
                    if (prefab != null)
                    {
                        string path = AssetDatabase.GetAssetPath(prefab);
                        if (!string.IsNullOrEmpty(path))
                            return AssetDatabase.AssetPathToGUID(path);
                    }
                }
                // Scene object - get the scene path
                else
                {
                    string scenePath = mb.gameObject.scene.path;
                    if (!string.IsNullOrEmpty(scenePath))
                        return AssetDatabase.AssetPathToGUID(scenePath);
                }
            }
            else
            {
                // ScriptableObject or other asset
                string path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path))
                    return AssetDatabase.AssetPathToGUID(path);
            }
            
            return null;
#else
            return null;
#endif
        }
    }
}

