#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Locomotion.Drink;
using UnityEditor;
using UnityEngine;

/// <summary>Editor for lemma-level localization property bags (syncs with Continuum API).</summary>
public sealed class VocabularyLemmaPropertyEditorWindow : EditorWindow
{
    const string PlaybackPolicyContextTypeName = "AnimationPlaybackPolicyContext";
    const string LocomotionRuntimeAssembly = "Locomotion.Runtime";

    LocalizationPropertySpecCatalog _catalog;
    string _entryId = "";
    string _clauseDraftId = "";
    int _clauseCharStart;
    int _clauseCharEnd;
    string _clauseSelection = "";
    ThesaurusEntryPropertyRecord[] _properties = Array.Empty<ThesaurusEntryPropertyRecord>();
    LemmaCompositionChildDto[] _compositionChildren = Array.Empty<LemmaCompositionChildDto>();
    string _compositionAddQuery = "";
    bool _nonIkAnimation;
    DrinkAnimationReference _drinkAnimationRef;
    DrinkLemmaProperties _drinkProps = DrinkLemmaProperties.Defaults;
    OpenCloseLemmaProperties _openCloseProps = OpenCloseLemmaProperties.Defaults;
    Vector2 _scroll;
    GameObject _pushTarget;

    [MenuItem("Window/Continuum/Lemma Properties")]
    public static void Open()
    {
        var w = GetWindow<VocabularyLemmaPropertyEditorWindow>("Lemma Properties");
        w.minSize = new Vector2(420, 320);
    }

    public static void OpenWithEntryId(string entryId)
    {
        var w = GetWindow<VocabularyLemmaPropertyEditorWindow>("Lemma Properties");
        w.minSize = new Vector2(420, 320);
        if (!string.IsNullOrEmpty(entryId))
        {
            w._entryId = entryId;
            _ = w.LoadPropertiesAsync();
            _ = w.LoadCompositionAsync();
        }
    }

    void OnEnable()
    {
        _catalog = Resources.Load<LocalizationPropertySpecCatalog>("LocalizationPropertySpecCatalog");
        if (_catalog == null)
            _catalog = LocalizationPropertySpecCatalog.CreateDefaultAsset();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Localization Property Specs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Lemma-level property bags sync with Continuum thesaurus_entry_properties.", MessageType.Info);

        _entryId = EditorGUILayout.TextField("Entry ID", _entryId);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load properties"))
        {
            _ = LoadPropertiesAsync();
            _ = LoadCompositionAsync();
        }
        if (GUILayout.Button("Save"))
            SaveProperties();
        GUI.enabled = !string.IsNullOrEmpty(_entryId);
        if (GUILayout.Button("Scan prefab components"))
            _ = ScanPrefabComponentsAsync();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Clause context (optional)", EditorStyles.boldLabel);
        _clauseDraftId = EditorGUILayout.TextField("Draft episode ID", _clauseDraftId);
        EditorGUILayout.BeginHorizontal();
        _clauseCharStart = EditorGUILayout.IntField("Char start", _clauseCharStart);
        _clauseCharEnd = EditorGUILayout.IntField("Char end", _clauseCharEnd);
        EditorGUILayout.EndHorizontal();
        _clauseSelection = EditorGUILayout.TextField("Selection text", _clauseSelection);
        GUI.enabled = !string.IsNullOrEmpty(_entryId) && !string.IsNullOrEmpty(_clauseDraftId) && _clauseCharEnd > _clauseCharStart;
        if (GUILayout.Button("Attach entry to clause span"))
            _ = AttachEntryToClauseAsync();
        GUI.enabled = !string.IsNullOrEmpty(_entryId) && _clauseCharEnd > _clauseCharStart;
        if (GUILayout.Button("Mark Mayor Dog mod slot"))
            _ = MarkMayorDogModSlotAsync();
        GUI.enabled = true;

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_catalog?.specs != null)
        {
            foreach (var spec in _catalog.specs)
            {
                if (spec == null) continue;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(spec.key, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Type", spec.valueType);
                EditorGUILayout.LabelField("Default", spec.defaultValue);
                EditorGUILayout.LabelField("Description", spec.description ?? "", EditorStyles.wordWrappedLabel);
                if (spec.key == LocalizationPropertyKeys.NonIkAnimation)
                    _nonIkAnimation = EditorGUILayout.Toggle("Value", _nonIkAnimation);
                EditorGUILayout.EndVertical();
            }
        }

        DrawDrinkPropertiesPanel();
        DrawOpenClosePropertiesPanel();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        _pushTarget = (GameObject)EditorGUILayout.ObjectField("Push to Travel Agent", _pushTarget, typeof(GameObject), true);
        if (GUILayout.Button("Push to runtime cache") && _pushTarget != null)
            PushToRuntime();

        if (GUILayout.Button("Sync specs from API"))
            SyncSpecs();

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Open in Lemma Library") && !string.IsNullOrEmpty(_entryId))
        {
            var baseUrl = ContinuumEditorSession.ApiBaseUrl.TrimEnd('/');
            Application.OpenURL($"{baseUrl}/lemma-library#entry/{Uri.EscapeDataString(_entryId)}");
        }

        DrawCompositionPanel();
    }

    void DrawDrinkPropertiesPanel()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Drink lemma properties", EditorStyles.boldLabel);

        _drinkAnimationRef = (DrinkAnimationReference)EditorGUILayout.ObjectField(
            "Animation reference", _drinkAnimationRef, typeof(DrinkAnimationReference), false);

        _drinkProps.autoMiddleMouthJaw = EditorGUILayout.Toggle("Auto middle mouth / jaw", _drinkProps.autoMiddleMouthJaw);
        _drinkProps.jawTiltAnimationAuditInsert = EditorGUILayout.Toggle(
            "Jaw tilt audit / insert keys", _drinkProps.jawTiltAnimationAuditInsert);
        _drinkProps.holdWithoutReturn = EditorGUILayout.Toggle("Hold without return", _drinkProps.holdWithoutReturn);
        _drinkProps.putWithoutRelease = EditorGUILayout.Toggle("Put without release", _drinkProps.putWithoutRelease);
        _drinkProps.nozzleLoopEnabled = EditorGUILayout.Toggle("Nozzle loop enabled", _drinkProps.nozzleLoopEnabled);
        _drinkProps.liquidSimulationEnabled = EditorGUILayout.Toggle("Liquid simulation", _drinkProps.liquidSimulationEnabled);
        _drinkProps.placeNozzleOnMouth = EditorGUILayout.Toggle("Place nozzle on mouth", _drinkProps.placeNozzleOnMouth);
        _drinkProps.drinkEfficacy = EditorGUILayout.Slider("Drink efficacy", _drinkProps.drinkEfficacy, 0f, 1f);
        _drinkProps.sipCount = EditorGUILayout.IntField("Sips to imbibe over", _drinkProps.sipCount);
        _drinkProps.sipCount = Mathf.Max(1, _drinkProps.sipCount);
        _drinkProps.totalVolumeLiters = EditorGUILayout.FloatField("Total volume (L)", _drinkProps.totalVolumeLiters);
        _drinkProps.totalVolumeLiters = Mathf.Max(0f, _drinkProps.totalVolumeLiters);
        if (_drinkProps.totalVolumeLiters > 0f && _drinkProps.sipCount > 0)
        {
            float oz = _drinkProps.totalVolumeLiters * DrinkLemmaPropertyKeys.LitersToUsFlOz;
            float perSipL = _drinkProps.VolumePerSipLiters;
            EditorGUILayout.LabelField(
                $"≈ {oz:F1} US fl oz total · {perSipL:F3} L per sip",
                EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Comedy / closure", EditorStyles.boldLabel);
        _drinkProps.partiallyRaiseAmount = EditorGUILayout.Slider("Partially raise amount", _drinkProps.partiallyRaiseAmount, 0f, 1f);
        _drinkProps.partialRaiseDefaultWhenStalled = EditorGUILayout.Slider(
            "Partial raise when stalled", _drinkProps.partialRaiseDefaultWhenStalled, 0f, 1f);
        _drinkProps.trainForPerfectDrink = EditorGUILayout.Toggle("Train for perfect drink", _drinkProps.trainForPerfectDrink);
        _drinkProps.maxSpillLitersTolerance = EditorGUILayout.FloatField("Max spill tolerance (L)", _drinkProps.maxSpillLitersTolerance);
        _drinkProps.maxSpillLitersTolerance = Mathf.Max(0f, _drinkProps.maxSpillLitersTolerance);
        _drinkProps.closureMode = (DrinkClosureMode)EditorGUILayout.EnumPopup("Closure mode", _drinkProps.closureMode);
        _drinkProps.mouthVolumeLitersTarget = EditorGUILayout.FloatField("Mouth volume target (L)", _drinkProps.mouthVolumeLitersTarget);
        _drinkProps.mouthVolumeLitersTarget = Mathf.Max(0f, _drinkProps.mouthVolumeLitersTarget);
        _drinkProps.infiniteDrain = EditorGUILayout.Toggle("Infinite drain (Fantasia)", _drinkProps.infiniteDrain);
        _drinkProps.infiniteDrainClosureSeconds = EditorGUILayout.FloatField("Infinite drain closure (s)", _drinkProps.infiniteDrainClosureSeconds);
        _drinkProps.infiniteDrainClosureSeconds = Mathf.Max(0f, _drinkProps.infiniteDrainClosureSeconds);

        EditorGUILayout.EndVertical();
    }

    void DrawOpenClosePropertiesPanel()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Open/close lemma properties", EditorStyles.boldLabel);
        _openCloseProps.openAngleDeg = EditorGUILayout.FloatField("Open angle (deg)", _openCloseProps.openAngleDeg);
        _openCloseProps.arrivalBlendCoefficient = EditorGUILayout.Slider("Arrival blend", _openCloseProps.arrivalBlendCoefficient, 0f, 1f);
        _openCloseProps.reachRadiusMeters = EditorGUILayout.FloatField("Reach radius (m)", _openCloseProps.reachRadiusMeters);
        _openCloseProps.requireFacingTarget = EditorGUILayout.Toggle("Require facing", _openCloseProps.requireFacingTarget);
        _openCloseProps.autoCloseBt = (OpenCloseLemmaAutoCloseBtMode)EditorGUILayout.EnumPopup("Auto close BT", _openCloseProps.autoCloseBt);
        _openCloseProps.autoCloseOnExit = EditorGUILayout.Toggle("Auto close on exit", _openCloseProps.autoCloseOnExit);
        _openCloseProps.compileCloseAmbulation = EditorGUILayout.Toggle("Compile close ambulation", _openCloseProps.compileCloseAmbulation);
        _openCloseProps.linearOnly = EditorGUILayout.Toggle("Linear only", _openCloseProps.linearOnly);
        _openCloseProps.questHintKind = (OpenCloseLemmaQuestHintKind)EditorGUILayout.EnumPopup("Quest hint", _openCloseProps.questHintKind);
        _openCloseProps.questObjectiveId = EditorGUILayout.TextField("Quest objective id", _openCloseProps.questObjectiveId ?? "");
        _openCloseProps.openAnimationRef = EditorGUILayout.TextField("Open animation ref", _openCloseProps.openAnimationRef ?? "");
        _openCloseProps.closeAnimationRef = EditorGUILayout.TextField("Close animation ref", _openCloseProps.closeAnimationRef ?? "");
        EditorGUILayout.EndVertical();
    }

    void DrawCompositionPanel()
    {
        EditorGUILayout.Space(4);
        var sepRect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(sepRect, new Color(0.4f, 0.4f, 0.4f, 1f));
        EditorGUILayout.Space(8);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Composed lemmas", EditorStyles.boldLabel);

        if (_compositionChildren == null || _compositionChildren.Length == 0)
            EditorGUILayout.LabelField("No child lemmas.", EditorStyles.miniLabel);
        else
        {
            foreach (var child in _compositionChildren)
            {
                if (child == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(child.term ?? child.entryId ?? "?", GUILayout.Width(160));
                EditorGUILayout.SelectableLabel(child.entryId ?? "", EditorStyles.textField, GUILayout.Height(18));
                if (GUILayout.Button("Remove", GUILayout.Width(64)))
                    _ = RemoveCompositionChildAsync(child.entryId);
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.BeginHorizontal();
        _compositionAddQuery = EditorGUILayout.TextField("Add child lemma", _compositionAddQuery);
        if (GUILayout.Button("Search & add", GUILayout.Width(100)))
            _ = SearchAndAddCompositionChildAsync();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reload composition"))
            _ = LoadCompositionAsync();
        if (GUILayout.Button("Open in Lemma Library") && !string.IsNullOrEmpty(_entryId))
        {
            var baseUrl = ContinuumEditorSession.ApiBaseUrl.TrimEnd('/');
            Application.OpenURL($"{baseUrl}/lemma-library#entry/{Uri.EscapeDataString(_entryId)}");
        }
        if (GUILayout.Button("Recombobulate spatial graph"))
            _ = RecombobulateSpatialAsync();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    async Task LoadCompositionAsync()
    {
        if (string.IsNullOrEmpty(_entryId)) return;
        var data = await ContinuumEditorLocalizationClient.Instance.GetCompositionAsync(_entryId);
        _compositionChildren = data?.children ?? Array.Empty<LemmaCompositionChildDto>();
        Repaint();
    }

    async Task SearchAndAddCompositionChildAsync()
    {
        if (string.IsNullOrEmpty(_entryId) || string.IsNullOrWhiteSpace(_compositionAddQuery))
            return;
        var q = Uri.EscapeDataString(_compositionAddQuery.Trim());
        var r = await ContinuumEditorLocalizationClient.Instance.CallRawAsync("GET", $"/api/thesaurus/entries?q={q}&limit=1", null);
        if (!r.success || string.IsNullOrEmpty(r.json))
        {
            EditorUtility.DisplayDialog("Composed lemmas", "No matching lemma found.", "OK");
            return;
        }
        var wrapper = JsonUtility.FromJson<ThesaurusEntryListWrapper>(r.json);
        if (wrapper?.items == null || wrapper.items.Length == 0)
        {
            EditorUtility.DisplayDialog("Composed lemmas", "No matching lemma found.", "OK");
            return;
        }
        var hit = wrapper.items[0];
        if (hit == null || string.IsNullOrEmpty(hit.id))
            return;
        var list = (_compositionChildren ?? Array.Empty<LemmaCompositionChildDto>()).ToList();
        if (list.Any(c => c != null && c.entryId == hit.id))
        {
            EditorUtility.DisplayDialog("Composed lemmas", "Lemma already in composition.", "OK");
            return;
        }
        list.Add(new LemmaCompositionChildDto { entryId = hit.id, term = hit.term, sortOrder = list.Count });
        var put = list.Select((c, i) => new LemmaCompositionChildPutDto { entryId = c.entryId, sortOrder = i }).ToArray();
        var saved = await ContinuumEditorLocalizationClient.Instance.PutCompositionAsync(_entryId, put);
        _compositionChildren = saved?.children ?? list.ToArray();
        _compositionAddQuery = "";
        Repaint();
    }

    async Task RemoveCompositionChildAsync(string childEntryId)
    {
        if (string.IsNullOrEmpty(_entryId) || string.IsNullOrEmpty(childEntryId))
            return;
        var list = (_compositionChildren ?? Array.Empty<LemmaCompositionChildDto>())
            .Where(c => c != null && c.entryId != childEntryId)
            .Select((c, i) => new LemmaCompositionChildPutDto { entryId = c.entryId, sortOrder = i })
            .ToArray();
        var saved = await ContinuumEditorLocalizationClient.Instance.PutCompositionAsync(_entryId, list);
        _compositionChildren = saved?.children ?? Array.Empty<LemmaCompositionChildDto>();
        Repaint();
    }

    async Task RecombobulateSpatialAsync()
    {
        if (string.IsNullOrEmpty(_entryId))
            return;
        string scriptText = "";
        if (!string.IsNullOrEmpty(_clauseDraftId))
        {
            var draft = await ContinuumEditorLocalizationClient.Instance.GetDraftScriptAsync(_clauseDraftId);
            scriptText = draft?.scriptText ?? "";
        }
        var audit = await ContinuumEditorLocalizationClient.Instance.RecombobulateSpatialAsync(
            _entryId,
            new LemmaRecombobulateRequestDto { scriptText = scriptText, draftEpisodeId = _clauseDraftId });
        if (audit?.issues == null || audit.issues.Length == 0)
        {
            EditorUtility.DisplayDialog("Recombobulate spatial graph", "No issues found.", "OK");
            await LoadCompositionAsync();
            return;
        }
        var ackIds = new List<string>();
        foreach (var issue in audit.issues)
        {
            if (issue == null) continue;
            var msg = $"{issue.code}: {issue.message}";
            if (!string.IsNullOrEmpty(issue.storedText) || !string.IsNullOrEmpty(issue.currentText))
                msg += $"\nStored: {issue.storedText}\nCurrent: {issue.currentText}";
            if (issue.requiresAck)
            {
                if (!EditorUtility.DisplayDialog("Recombobulate spatial graph", msg + "\n\nAcknowledge this fix?", "Acknowledge", "Skip"))
                    continue;
                ackIds.Add(issue.id);
            }
            else
                ackIds.Add(issue.id);
        }
        await ContinuumEditorLocalizationClient.Instance.RecombobulateSpatialAsync(
            _entryId,
            new LemmaRecombobulateRequestDto
            {
                scriptText = scriptText,
                draftEpisodeId = _clauseDraftId,
                apply = true,
                acknowledgedIssueIds = ackIds.ToArray(),
            });
        await LoadCompositionAsync();
        EditorUtility.DisplayDialog("Recombobulate spatial graph", "Repair pass completed.", "OK");
    }

    [Serializable]
    class ThesaurusEntryListWrapper
    {
        public ThesaurusEntrySummaryDto[] items;
    }

    [Serializable]
    class ThesaurusEntrySummaryDto
    {
        public string id;
        public string term;
    }

    async Task ScanPrefabComponentsAsync()
    {
        if (string.IsNullOrEmpty(_entryId))
            return;
        var ok = await LemmaComponentBlueprintScanner.ScanAndPostEntryAsync(_entryId);
        EditorUtility.DisplayDialog(
            "Lemma Properties",
            ok ? "Prefab component blueprint uploaded." : "Scan failed — check prefab-id property and Console.",
            "OK");
    }

    async Task LoadPropertiesAsync()
    {
        if (string.IsNullOrEmpty(_entryId)) return;
        var client = ContinuumEditorLocalizationClient.Instance;
        _properties = await client.GetEntryPropertiesAsync(_entryId);
        _nonIkAnimation = false;
        _drinkProps = DrinkLemmaProperties.Defaults;
        _openCloseProps = OpenCloseLemmaProperties.Defaults;
        _drinkAnimationRef = null;
        foreach (var p in _properties)
        {
            if (p == null) continue;
            if (p.propertyKey == LocalizationPropertyKeys.NonIkAnimation &&
                TryParseBool(p.propertyValue, out bool v))
                _nonIkAnimation = v;
            ApplyDrinkPropertyFromRecord(p);
            ApplyOpenClosePropertyFromRecord(p);
        }
        if (!string.IsNullOrEmpty(_drinkProps.drinkAnimationRef))
        {
            _drinkAnimationRef = AssetDatabase.LoadAssetAtPath<DrinkAnimationReference>(_drinkProps.drinkAnimationRef);
            if (_drinkAnimationRef == null)
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(DrinkAnimationReference)}");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<DrinkAnimationReference>(path);
                    if (asset != null && (asset.name == _drinkProps.drinkAnimationRef || path == _drinkProps.drinkAnimationRef))
                    {
                        _drinkAnimationRef = asset;
                        break;
                    }
                }
            }
        }
        Repaint();
    }

    void ApplyOpenClosePropertyFromRecord(ThesaurusEntryPropertyRecord p)
    {
        switch (p.propertyKey)
        {
            case OpenCloseLemmaPropertyKeys.OpenAngleDeg:
                if (float.TryParse(p.propertyValue, out float oa)) _openCloseProps.openAngleDeg = oa;
                break;
            case OpenCloseLemmaPropertyKeys.ArrivalBlendCoefficient:
                if (float.TryParse(p.propertyValue, out float ab)) _openCloseProps.arrivalBlendCoefficient = Mathf.Clamp01(ab);
                break;
            case OpenCloseLemmaPropertyKeys.ReachRadiusMeters:
                if (float.TryParse(p.propertyValue, out float rr)) _openCloseProps.reachRadiusMeters = Mathf.Max(0.1f, rr);
                break;
            case OpenCloseLemmaPropertyKeys.RequireFacingTarget:
                if (TryParseBool(p.propertyValue, out bool rf)) _openCloseProps.requireFacingTarget = rf;
                break;
            case OpenCloseLemmaPropertyKeys.AutoCloseOnExit:
                if (TryParseBool(p.propertyValue, out bool ace)) _openCloseProps.autoCloseOnExit = ace;
                break;
            case OpenCloseLemmaPropertyKeys.CompileCloseAmbulation:
                if (TryParseBool(p.propertyValue, out bool cca)) _openCloseProps.compileCloseAmbulation = cca;
                break;
            case OpenCloseLemmaPropertyKeys.LinearOnly:
                if (TryParseBool(p.propertyValue, out bool lo)) _openCloseProps.linearOnly = lo;
                break;
            case OpenCloseLemmaPropertyKeys.AutoCloseBt:
                _openCloseProps.autoCloseBt = ParseOpenCloseAutoCloseBt(p.propertyValue);
                break;
            case OpenCloseLemmaPropertyKeys.QuestHintKind:
                if (Enum.TryParse(p.propertyValue, true, out OpenCloseLemmaQuestHintKind qh))
                    _openCloseProps.questHintKind = qh;
                break;
            case OpenCloseLemmaPropertyKeys.QuestObjectiveId:
                _openCloseProps.questObjectiveId = p.propertyValue ?? "";
                break;
            case OpenCloseLemmaPropertyKeys.OpenAnimationRef:
                _openCloseProps.openAnimationRef = p.propertyValue ?? "";
                break;
            case OpenCloseLemmaPropertyKeys.CloseAnimationRef:
                _openCloseProps.closeAnimationRef = p.propertyValue ?? "";
                break;
        }
    }

    static OpenCloseLemmaAutoCloseBtMode ParseOpenCloseAutoCloseBt(string raw)
    {
        raw = (raw ?? "on-stop-exit").Trim().ToLowerInvariant().Replace("_", "-");
        return raw switch
        {
            "none" => OpenCloseLemmaAutoCloseBtMode.None,
            "after-children" => OpenCloseLemmaAutoCloseBtMode.AfterChildren,
            "on-sequence-end" => OpenCloseLemmaAutoCloseBtMode.OnSequenceEnd,
            "manual" => OpenCloseLemmaAutoCloseBtMode.Manual,
            _ => OpenCloseLemmaAutoCloseBtMode.OnStopExit,
        };
    }

    static string AutoCloseBtToString(OpenCloseLemmaAutoCloseBtMode mode) => mode switch
    {
        OpenCloseLemmaAutoCloseBtMode.None => "none",
        OpenCloseLemmaAutoCloseBtMode.AfterChildren => "after-children",
        OpenCloseLemmaAutoCloseBtMode.OnSequenceEnd => "on-sequence-end",
        OpenCloseLemmaAutoCloseBtMode.Manual => "manual",
        _ => "on-stop-exit",
    };

    void ApplyDrinkPropertyFromRecord(ThesaurusEntryPropertyRecord p)
    {
        switch (p.propertyKey)
        {
            case DrinkLemmaPropertyKeys.DrinkAnimationRef:
                _drinkProps.drinkAnimationRef = p.propertyValue ?? "";
                break;
            case DrinkLemmaPropertyKeys.AutoMiddleMouthJaw:
                if (TryParseBool(p.propertyValue, out bool am)) _drinkProps.autoMiddleMouthJaw = am;
                break;
            case DrinkLemmaPropertyKeys.JawTiltAnimationAuditInsert:
                if (TryParseBool(p.propertyValue, out bool jt)) _drinkProps.jawTiltAnimationAuditInsert = jt;
                break;
            case DrinkLemmaPropertyKeys.HoldWithoutReturn:
                if (TryParseBool(p.propertyValue, out bool hr)) _drinkProps.holdWithoutReturn = hr;
                break;
            case DrinkLemmaPropertyKeys.PutWithoutRelease:
                if (TryParseBool(p.propertyValue, out bool pr)) _drinkProps.putWithoutRelease = pr;
                break;
            case DrinkLemmaPropertyKeys.NozzleLoopEnabled:
                if (TryParseBool(p.propertyValue, out bool nl)) _drinkProps.nozzleLoopEnabled = nl;
                break;
            case DrinkLemmaPropertyKeys.LiquidSimulationEnabled:
                if (TryParseBool(p.propertyValue, out bool ls)) _drinkProps.liquidSimulationEnabled = ls;
                break;
            case DrinkLemmaPropertyKeys.PlaceNozzleOnMouth:
                if (TryParseBool(p.propertyValue, out bool pn)) _drinkProps.placeNozzleOnMouth = pn;
                break;
            case DrinkLemmaPropertyKeys.DrinkEfficacy:
                if (float.TryParse(p.propertyValue, out float de)) _drinkProps.drinkEfficacy = Mathf.Clamp01(de);
                break;
            case DrinkLemmaPropertyKeys.SipCount:
                if (int.TryParse(p.propertyValue, out int sc)) _drinkProps.sipCount = Mathf.Max(1, sc);
                break;
            case DrinkLemmaPropertyKeys.TotalVolumeLiters:
                if (float.TryParse(p.propertyValue, out float tv)) _drinkProps.totalVolumeLiters = Mathf.Max(0f, tv);
                break;
            case DrinkLemmaPropertyKeys.PartiallyRaiseAmount:
                if (float.TryParse(p.propertyValue, out float pra)) _drinkProps.partiallyRaiseAmount = Mathf.Clamp01(pra);
                break;
            case DrinkLemmaPropertyKeys.PartialRaiseDefaultWhenStalled:
                if (float.TryParse(p.propertyValue, out float prs)) _drinkProps.partialRaiseDefaultWhenStalled = Mathf.Clamp01(prs);
                break;
            case DrinkLemmaPropertyKeys.TrainForPerfectDrink:
                if (TryParseBool(p.propertyValue, out bool tfp)) _drinkProps.trainForPerfectDrink = tfp;
                break;
            case DrinkLemmaPropertyKeys.MaxSpillLitersTolerance:
                if (float.TryParse(p.propertyValue, out float mst)) _drinkProps.maxSpillLitersTolerance = Mathf.Max(0f, mst);
                break;
            case DrinkLemmaPropertyKeys.ClosureMode:
                if (Enum.TryParse(p.propertyValue?.Replace("-", ""), true, out DrinkClosureMode cm))
                    _drinkProps.closureMode = cm;
                else if (p.propertyValue == "spill-beat") _drinkProps.closureMode = DrinkClosureMode.SpillBeat;
                else if (p.propertyValue == "empty-vessel") _drinkProps.closureMode = DrinkClosureMode.EmptyVessel;
                else if (p.propertyValue == "infinite-drain-beat") _drinkProps.closureMode = DrinkClosureMode.InfiniteDrainBeat;
                break;
            case DrinkLemmaPropertyKeys.MouthVolumeLitersTarget:
                if (float.TryParse(p.propertyValue, out float mvt)) _drinkProps.mouthVolumeLitersTarget = Mathf.Max(0f, mvt);
                break;
            case DrinkLemmaPropertyKeys.InfiniteDrain:
                if (TryParseBool(p.propertyValue, out bool id)) _drinkProps.infiniteDrain = id;
                break;
            case DrinkLemmaPropertyKeys.InfiniteDrainClosureSeconds:
                if (float.TryParse(p.propertyValue, out float idc)) _drinkProps.infiniteDrainClosureSeconds = Mathf.Max(0f, idc);
                break;
        }
    }

    async void SaveProperties()
    {
        if (string.IsNullOrEmpty(_entryId)) return;
        var client = ContinuumEditorLocalizationClient.Instance;
        await client.PutEntryPropertyAsync(_entryId, LocalizationPropertyKeys.NonIkAnimation, _nonIkAnimation ? "true" : "false");
        string animRef = _drinkAnimationRef != null
            ? AssetDatabase.GetAssetPath(_drinkAnimationRef)
            : (_drinkProps.drinkAnimationRef ?? "");
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.DrinkAnimationRef, animRef ?? "");
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.AutoMiddleMouthJaw, _drinkProps.autoMiddleMouthJaw ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.JawTiltAnimationAuditInsert, _drinkProps.jawTiltAnimationAuditInsert ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.HoldWithoutReturn, _drinkProps.holdWithoutReturn ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.PutWithoutRelease, _drinkProps.putWithoutRelease ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.NozzleLoopEnabled, _drinkProps.nozzleLoopEnabled ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.LiquidSimulationEnabled, _drinkProps.liquidSimulationEnabled ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.PlaceNozzleOnMouth, _drinkProps.placeNozzleOnMouth ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.DrinkEfficacy, _drinkProps.drinkEfficacy.ToString("G"));
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.SipCount, _drinkProps.sipCount.ToString());
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.TotalVolumeLiters, _drinkProps.totalVolumeLiters.ToString("G"));
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.PartiallyRaiseAmount, _drinkProps.partiallyRaiseAmount.ToString("G"));
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.PartialRaiseDefaultWhenStalled, _drinkProps.partialRaiseDefaultWhenStalled.ToString("G"));
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.TrainForPerfectDrink, _drinkProps.trainForPerfectDrink ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.MaxSpillLitersTolerance, _drinkProps.maxSpillLitersTolerance.ToString("G"));
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.ClosureMode, ClosureModeToString(_drinkProps.closureMode));
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.MouthVolumeLitersTarget, _drinkProps.mouthVolumeLitersTarget.ToString("G"));
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.InfiniteDrain, _drinkProps.infiniteDrain ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, DrinkLemmaPropertyKeys.InfiniteDrainClosureSeconds, _drinkProps.infiniteDrainClosureSeconds.ToString("G"));
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.OpenAngleDeg, _openCloseProps.openAngleDeg.ToString("G"));
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.ArrivalBlendCoefficient, _openCloseProps.arrivalBlendCoefficient.ToString("G"));
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.ReachRadiusMeters, _openCloseProps.reachRadiusMeters.ToString("G"));
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.RequireFacingTarget, _openCloseProps.requireFacingTarget ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.AutoCloseBt, AutoCloseBtToString(_openCloseProps.autoCloseBt));
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.AutoCloseOnExit, _openCloseProps.autoCloseOnExit ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.CompileCloseAmbulation, _openCloseProps.compileCloseAmbulation ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.LinearOnly, _openCloseProps.linearOnly ? "true" : "false");
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.QuestHintKind, _openCloseProps.questHintKind.ToString().ToLowerInvariant());
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.QuestObjectiveId, _openCloseProps.questObjectiveId ?? "");
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.OpenAnimationRef, _openCloseProps.openAnimationRef ?? "");
        await client.PutEntryPropertyAsync(_entryId, OpenCloseLemmaPropertyKeys.CloseAnimationRef, _openCloseProps.closeAnimationRef ?? "");
        await LoadPropertiesAsync();
    }

    async Task AttachEntryToClauseAsync()
    {
        var draft = await ContinuumEditorLocalizationClient.Instance.GetDraftScriptAsync(_clauseDraftId);
        string scriptText = draft?.scriptText ?? "";
        var farey = FareySpanUtility.CharRangeToFareySpan(scriptText, _clauseCharStart, _clauseCharEnd);
        var clauseRef = new ClauseRefRecord
        {
            charStart = _clauseCharStart,
            charEnd = _clauseCharEnd,
            selectionText = _clauseSelection,
            entryId = _entryId,
            draftEpisodeId = _clauseDraftId,
            draftScriptId = draft?.id ?? "",
            fareyLeftNum = farey.ln,
            fareyLeftDen = farey.ld,
            fareyRightNum = farey.rn,
            fareyRightDen = farey.rd,
        };
        await ContinuumEditorLocalizationClient.Instance.PostClauseBindingAsync(
            clauseRef,
            LocalizationBindingKinds.Lemma,
            "entry-id",
            _entryId,
            scriptText);
        EditorUtility.DisplayDialog("Lemma Properties", "Lemma attached to clause span.", "OK");
    }

    async Task MarkMayorDogModSlotAsync()
    {
        if (string.IsNullOrEmpty(_entryId) || _clauseCharEnd <= _clauseCharStart)
            return;
        var label = string.IsNullOrEmpty(_clauseSelection)
            ? _entryId
            : _clauseSelection.Trim();
        if (label.Length > 48)
            label = label.Substring(0, 48);
        var slotKey = System.Text.RegularExpressions.Regex.Replace(label.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrEmpty(slotKey))
            slotKey = "lemma-slot";
        slotKey = $"{slotKey}-{System.Guid.NewGuid().ToString("N").Substring(0, 4)}";
        var draft = await ContinuumEditorLocalizationClient.Instance.GetDraftScriptAsync(_clauseDraftId);
        var scriptText = draft?.scriptText ?? "";
        var body = $@"{{""targetKind"":""lemma_prompt"",""entryId"":""{_entryId}"",""charStart"":{_clauseCharStart},""charEnd"":{_clauseCharEnd},""slotKey"":""{slotKey}"",""label"":""{label.Replace("\"", "\\\"")}"",""sourceText"":{JsonEscape(scriptText)}}}";
        var resp = await ContinuumEditorLocalizationClient.Instance.CallRawAsync("POST", "/api/mods/moddable-targets", body);
        if (!resp.success)
        {
            EditorUtility.DisplayDialog("Mayor Dog Mods", resp.error ?? "Failed to create mod slot.", "OK");
            return;
        }
        EditorUtility.DisplayDialog("Mayor Dog Mods", $"Mod slot created: {slotKey}\nInsert {{M:{slotKey}}} in lemma prompt.", "OK");
    }

    static string JsonEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "\"\"";
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
    }

    void PushToRuntime()
    {
        var ctxType = FindLocomotionRuntimeType(PlaybackPolicyContextTypeName);
        if (ctxType == null)
        {
            EditorUtility.DisplayDialog("Lemma Properties", "Locomotion.Runtime is not loaded.", "OK");
            return;
        }

        var ctx = _pushTarget.GetComponent(ctxType) ?? _pushTarget.GetComponentInChildren(ctxType);
        if (ctx == null)
        {
            EditorUtility.DisplayDialog("Lemma Properties", "Add AnimationPlaybackPolicyContext to Travel Agent hierarchy.", "OK");
            return;
        }

        var method = ctxType.GetMethod("SetLemmaProperties", BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
        {
            EditorUtility.DisplayDialog("Lemma Properties", "AnimationPlaybackPolicyContext.SetLemmaProperties not found.", "OK");
            return;
        }

        var list = _properties.ToList();
        if (!string.IsNullOrEmpty(_entryId))
        {
            list.RemoveAll(p => p != null && p.entryId == _entryId && (
                p.propertyKey == LocalizationPropertyKeys.NonIkAnimation ||
                DrinkLemmaPropertyKeys.AllKeys.Contains(p.propertyKey)));
            list.Add(new ThesaurusEntryPropertyRecord
            {
                entryId = _entryId,
                propertyKey = LocalizationPropertyKeys.NonIkAnimation,
                propertyValue = _nonIkAnimation ? "true" : "false"
            });
            void AddDrink(string key, string value)
            {
                list.Add(new ThesaurusEntryPropertyRecord { entryId = _entryId, propertyKey = key, propertyValue = value });
            }
            string animRef = _drinkAnimationRef != null
                ? AssetDatabase.GetAssetPath(_drinkAnimationRef)
                : (_drinkProps.drinkAnimationRef ?? "");
            AddDrink(DrinkLemmaPropertyKeys.DrinkAnimationRef, animRef ?? "");
            AddDrink(DrinkLemmaPropertyKeys.AutoMiddleMouthJaw, _drinkProps.autoMiddleMouthJaw ? "true" : "false");
            AddDrink(DrinkLemmaPropertyKeys.JawTiltAnimationAuditInsert, _drinkProps.jawTiltAnimationAuditInsert ? "true" : "false");
            AddDrink(DrinkLemmaPropertyKeys.HoldWithoutReturn, _drinkProps.holdWithoutReturn ? "true" : "false");
            AddDrink(DrinkLemmaPropertyKeys.PutWithoutRelease, _drinkProps.putWithoutRelease ? "true" : "false");
            AddDrink(DrinkLemmaPropertyKeys.NozzleLoopEnabled, _drinkProps.nozzleLoopEnabled ? "true" : "false");
            AddDrink(DrinkLemmaPropertyKeys.LiquidSimulationEnabled, _drinkProps.liquidSimulationEnabled ? "true" : "false");
            AddDrink(DrinkLemmaPropertyKeys.PlaceNozzleOnMouth, _drinkProps.placeNozzleOnMouth ? "true" : "false");
            AddDrink(DrinkLemmaPropertyKeys.DrinkEfficacy, _drinkProps.drinkEfficacy.ToString("G"));
            AddDrink(DrinkLemmaPropertyKeys.SipCount, _drinkProps.sipCount.ToString());
            AddDrink(DrinkLemmaPropertyKeys.TotalVolumeLiters, _drinkProps.totalVolumeLiters.ToString("G"));
            AddDrink(DrinkLemmaPropertyKeys.PartiallyRaiseAmount, _drinkProps.partiallyRaiseAmount.ToString("G"));
            AddDrink(DrinkLemmaPropertyKeys.PartialRaiseDefaultWhenStalled, _drinkProps.partialRaiseDefaultWhenStalled.ToString("G"));
            AddDrink(DrinkLemmaPropertyKeys.TrainForPerfectDrink, _drinkProps.trainForPerfectDrink ? "true" : "false");
            AddDrink(DrinkLemmaPropertyKeys.MaxSpillLitersTolerance, _drinkProps.maxSpillLitersTolerance.ToString("G"));
            AddDrink(DrinkLemmaPropertyKeys.ClosureMode, ClosureModeToString(_drinkProps.closureMode));
            AddDrink(DrinkLemmaPropertyKeys.MouthVolumeLitersTarget, _drinkProps.mouthVolumeLitersTarget.ToString("G"));
            AddDrink(DrinkLemmaPropertyKeys.InfiniteDrain, _drinkProps.infiniteDrain ? "true" : "false");
            AddDrink(DrinkLemmaPropertyKeys.InfiniteDrainClosureSeconds, _drinkProps.infiniteDrainClosureSeconds.ToString("G"));
        }

        method.Invoke(ctx, new object[] { list });
    }

    async void SyncSpecs()
    {
        var specs = await ContinuumEditorLocalizationClient.Instance.GetPropertySpecsAsync();
        if (specs != null && specs.Length > 0)
            Debug.Log($"[Lemma Properties] Loaded {specs.Length} spec(s) from API.");
    }

    static Type FindLocomotionRuntimeType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!string.Equals(assembly.GetName().Name, LocomotionRuntimeAssembly, StringComparison.Ordinal))
                continue;
            var type = assembly.GetType(typeName, throwOnError: false);
            if (type != null)
                return type;
        }
        return null;
    }

    static string ClosureModeToString(DrinkClosureMode mode) => mode switch
    {
        DrinkClosureMode.Mouth => "mouth",
        DrinkClosureMode.EmptyVessel => "empty-vessel",
        DrinkClosureMode.Stalled => "stalled",
        DrinkClosureMode.SpillBeat => "spill-beat",
        DrinkClosureMode.InfiniteDrainBeat => "infinite-drain-beat",
        _ => "auto",
    };

    static bool TryParseBool(string value, out bool result)
    {
        result = false;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (bool.TryParse(value.Trim(), out result))
            return true;
        switch (value.Trim().ToLowerInvariant())
        {
            case "1":
            case "yes":
            case "on":
                result = true;
                return true;
            case "0":
            case "no":
            case "off":
                result = false;
                return true;
            default:
                return false;
        }
    }
}
#endif
