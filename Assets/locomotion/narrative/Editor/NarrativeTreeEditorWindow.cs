#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Locomotion.Narrative;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Locomotion.Narrative.EditorTools
{
    public class NarrativeTreeEditorWindow : EditorWindow
    {
        private NarrativeTreeAsset tree;
        private NarrativeGraphView graph;
        private VisualElement inspectorRoot;

        private NarrativeNode selectedNode;

        [MenuItem("Window/System Drawer/Narrative/Tree Editor", false, 200)]
        public static void ShowWindow()
        {
            var w = GetWindow<NarrativeTreeEditorWindow>("Narrative Tree");
            w.minSize = new Vector2(980, 620);
            w.Show();
        }

        public static void ShowWindow(NarrativeTreeAsset tree)
        {
            var w = GetWindow<NarrativeTreeEditorWindow>("Narrative Tree");
            w.minSize = new Vector2(980, 620);
            w.tree = tree;
            w.Rebuild();
            w.Show();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            // Note: GraphView also defines an internal Toolbar type; use the public UIElements toolbar explicitly.
            var toolbar = new UnityEditor.UIElements.Toolbar();
            
            // Use IMGUIContainer for ObjectField to ensure scene objects work properly
            var treeFieldContainer = new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                var newTree = EditorGUILayout.ObjectField("Tree", tree, typeof(NarrativeTreeAsset), true) as NarrativeTreeAsset;
                if (EditorGUI.EndChangeCheck())
            {
                    tree = newTree;
                Rebuild();
                }
            });
            treeFieldContainer.style.flexGrow = 1f;

            toolbar.Add(treeFieldContainer);
            toolbar.Add(new UnityEditor.UIElements.ToolbarButton(() => AddChildToSelected(NarrativeNodeType.Sequence)) { text = "Add Sequence" });
            toolbar.Add(new UnityEditor.UIElements.ToolbarButton(() => AddChildToSelected(NarrativeNodeType.Selector)) { text = "Add Selector" });
            toolbar.Add(new UnityEditor.UIElements.ToolbarButton(() => AddChildToSelected(NarrativeNodeType.Action)) { text = "Add Action" });
            toolbar.Add(new UnityEditor.UIElements.ToolbarButton(() => Rebuild()) { text = "Refresh" });
            root.Add(toolbar);

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;

            graph = new NarrativeGraphView();
            graph.style.flexGrow = 1.8f;
            graph.OnNodeSelected = node =>
            {
                selectedNode = node;
                RebuildInspector();
            };

            inspectorRoot = new ScrollView();
            inspectorRoot.style.flexGrow = 1f;
            inspectorRoot.style.paddingLeft = 8;
            inspectorRoot.style.paddingRight = 8;
            inspectorRoot.style.paddingTop = 6;
            inspectorRoot.style.paddingBottom = 6;

            body.Add(graph);
            body.Add(inspectorRoot);
            root.Add(body);

            Rebuild();
        }

        private void Rebuild()
        {
            selectedNode = null;
            if (graph != null)
            {
                graph.Populate(tree);
            }
            RebuildInspector();
        }

        private void AddChildToSelected(NarrativeNodeType type)
        {
            if (tree == null || tree.root == null)
                return;

            var parent = selectedNode ?? tree.root;

            // Only nodes with children can accept children.
            if (parent is NarrativeActionNode)
                parent = tree.root;

            Undo.RecordObject(tree, "Add Narrative Node");

            NarrativeNode child = type switch
            {
                NarrativeNodeType.Sequence => new NarrativeSequenceNode { title = "Sequence" },
                NarrativeNodeType.Selector => new NarrativeSelectorNode { title = "Selector" },
                NarrativeNodeType.Action => new NarrativeActionNode { title = "Action", action = new CallMethodAction() },
                _ => new NarrativeActionNode { title = "Action", action = new CallMethodAction() }
            };

            switch (parent)
            {
                case NarrativeSequenceNode seq:
                    seq.children.Add(child);
                    break;
                case NarrativeSelectorNode sel:
                    sel.children.Add(child);
                    break;
                default:
                    // fallback: root
                    if (tree.root is NarrativeSequenceNode rootSeq)
                        rootSeq.children.Add(child);
                    break;
            }

            EditorUtility.SetDirty(tree);
            graph?.Populate(tree);
            selectedNode = child;
            RebuildInspector();
        }

        private void RebuildInspector()
        {
            inspectorRoot.Clear();

            if (tree == null)
            {
                inspectorRoot.Add(new Label("Assign a NarrativeTreeAsset to edit."));
                return;
            }

            var node = selectedNode ?? tree.root;
            if (node == null)
            {
                inspectorRoot.Add(new Label("Tree has no root node."));
                return;
            }

            inspectorRoot.Add(new Label("Node") { style = { unityFontStyleAndWeight = FontStyle.Bold } });

            var titleField = new TextField("Title") { value = node.title };
            titleField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(tree, "Edit Narrative Node Title");
                node.title = evt.newValue;
                EditorUtility.SetDirty(tree);
                graph?.Populate(tree);
            });
            inspectorRoot.Add(titleField);

            inspectorRoot.Add(new Label($"Type: {node.NodeType}"));
            inspectorRoot.Add(new Label($"Id: {node.id}") { style = { color = new Color(0, 0, 0, 0.6f), fontSize = 10 } });

            inspectorRoot.Add(new Label("Contingency") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10 } });
            inspectorRoot.Add(new IMGUIContainer(() => DrawContingency(node.contingency)));

            if (node is NarrativeActionNode an)
            {
                inspectorRoot.Add(new Label("Action") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10 } });
                inspectorRoot.Add(new IMGUIContainer(() => DrawAction(an)));
            }
        }

        private void DrawContingency(NarrativeContingency c)
        {
            if (tree == null) return;

            EditorGUI.BeginChangeCheck();
            c.enabled = EditorGUILayout.Toggle("Enabled", c.enabled);
            c.op = (NarrativeLogicalOperator)EditorGUILayout.EnumPopup("Operator", c.op);

            if (GUILayout.Button("Add Condition: Component Member"))
            {
                Undo.RecordObject(tree, "Add Narrative Condition");
                c.conditions.Add(new ComponentMemberCondition());
                EditorUtility.SetDirty(tree);
            }

            for (int i = 0; i < c.conditions.Count; i++)
            {
                var cond = c.conditions[i];
                if (cond == null) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(cond.GetType().Name, EditorStyles.boldLabel);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    Undo.RecordObject(tree, "Remove Narrative Condition");
                    c.conditions.RemoveAt(i);
                    EditorUtility.SetDirty(tree);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (cond is ComponentMemberCondition cm)
                {
                    cm.targetKey = EditorGUILayout.TextField("Target Key", cm.targetKey);
                    cm.componentTypeName = EditorGUILayout.TextField("Component Type", cm.componentTypeName);
                    cm.memberName = EditorGUILayout.TextField("Member", cm.memberName);
                    cm.comparison = (ComparisonOperator)EditorGUILayout.EnumPopup("Compare", cm.comparison);
                    DrawNarrativeValue("Value", ref cm.compareTo);
                }

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(tree, "Edit Narrative Contingency");
                EditorUtility.SetDirty(tree);
            }
        }

        private void DrawAction(NarrativeActionNode node)
        {
            if (tree == null) return;

            EditorGUI.BeginChangeCheck();

            NarrativeActionSpec a = node.action;
            string typeName = a != null ? a.GetType().Name : "(none)";
            EditorGUILayout.LabelField("Current", typeName);

            // Action type switcher
            var newType = (NarrativeActionKind)EditorGUILayout.EnumPopup("Action Type", GetKind(a));
            if (a == null || newType != GetKind(a))
            {
                Undo.RecordObject(tree, "Change Narrative Action Type");
                node.action = newType switch
                {
                    NarrativeActionKind.SpawnPrefab => new SpawnPrefabAction(),
                    NarrativeActionKind.SetProperty => new SetPropertyAction(),
                    NarrativeActionKind.CallMethod => new CallMethodAction(),
                    NarrativeActionKind.RunBehaviorTree => new RunBehaviorTreeAction(),
                    NarrativeActionKind.SendThought => new SendThoughtAction(),
                    NarrativeActionKind.EnterSlowTimeGambit => new NarrativeEnterSlowTimeGambitAction(),
                    NarrativeActionKind.ChooseGambitAperture => new NarrativeChooseGambitApertureAction(),
                    NarrativeActionKind.CommitGambitPath => new NarrativeCommitGambitPathAction(),
                    NarrativeActionKind.EnterSlowTimeWrestling => new NarrativeEnterSlowTimeWrestlingAction(),
                    NarrativeActionKind.ChooseWrestlingCard => new NarrativeChooseWrestlingCardAction(),
                    NarrativeActionKind.CommitWrestlingCard => new NarrativeCommitWrestlingCardAction(),
                    NarrativeActionKind.WrestlingBioRhythm => new NarrativeWrestlingBioRhythmAction(),
                    NarrativeActionKind.EnterSlowTimeLoveMaking => new NarrativeEnterSlowTimeLoveMakingAction(),
                    NarrativeActionKind.ChooseLoveMakingCard => new NarrativeChooseLoveMakingCardAction(),
                    NarrativeActionKind.CommitLoveMakingCard => new NarrativeCommitLoveMakingCardAction(),
                    NarrativeActionKind.LoveMakingBioRhythm => new NarrativeLoveMakingBioRhythmAction(),
                    NarrativeActionKind.EnterSlowTimeCombat => new NarrativeEnterSlowTimeCombatAction(),
                    NarrativeActionKind.ChooseCombatCard => new NarrativeChooseCombatCardAction(),
                    NarrativeActionKind.CommitCombatCard => new NarrativeCommitCombatCardAction(),
                    NarrativeActionKind.CombatCommunique => new CombatCommuniqueNarrativeAction(),
                    NarrativeActionKind.Trade => new NarrativeTradeAction(),
                    NarrativeActionKind.Bite => new BiteNarrativeAction(),
                    NarrativeActionKind.Chew => new ChewNarrativeAction(),
                    NarrativeActionKind.Swallow => new SwallowNarrativeAction(),
                    NarrativeActionKind.AnimationChew => new AnimationChewNarrativeAction(),
                    _ => new CallMethodAction()
                };
                EditorUtility.SetDirty(tree);
                a = node.action;
            }

            if (a == null)
            {
                EditorGUI.EndChangeCheck();
                return;
            }

            // Action-specific fields
            if (a is SpawnPrefabAction sp)
            {
                sp.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", sp.prefab, typeof(GameObject), false);
                sp.parentKey = EditorGUILayout.TextField("Parent Key", sp.parentKey);
                sp.worldSpace = EditorGUILayout.Toggle("World Space", sp.worldSpace);
                sp.localPosition = EditorGUILayout.Vector3Field("Position", sp.localPosition);
                sp.localEulerAngles = EditorGUILayout.Vector3Field("Euler", sp.localEulerAngles);
            }
            else if (a is SetPropertyAction set)
            {
                set.targetKey = EditorGUILayout.TextField("Target Key", set.targetKey);
                set.componentTypeName = EditorGUILayout.TextField("Component Type", set.componentTypeName);
                set.memberName = EditorGUILayout.TextField("Member", set.memberName);
                DrawNarrativeValue("Value", ref set.value);
            }
            else if (a is CallMethodAction call)
            {
                call.targetKey = EditorGUILayout.TextField("Target Key", call.targetKey);
                call.componentTypeName = EditorGUILayout.TextField("Component Type", call.componentTypeName);
                call.methodName = EditorGUILayout.TextField("Method", call.methodName);

                int n = Mathf.Max(0, EditorGUILayout.IntField("Arg Count", call.args != null ? call.args.Length : 0));
                if (call.args == null || call.args.Length != n)
                {
                    Array.Resize(ref call.args, n);
                }

                for (int i = 0; i < n; i++)
                {
                    var v = call.args[i];
                    DrawNarrativeValue($"Arg {i}", ref v);
                    call.args[i] = v;
                }
            }
            else if (a is RunBehaviorTreeAction run)
            {
                run.actorKey = EditorGUILayout.TextField("Actor Key", run.actorKey);
                run.goal.goalName = EditorGUILayout.TextField("Goal Name", run.goal.goalName);
                run.goal.type = (GoalType)EditorGUILayout.EnumPopup("Goal Type", run.goal.type);
                run.goal.targetKey = EditorGUILayout.TextField("Target Key", run.goal.targetKey);
                run.goal.targetPosition = EditorGUILayout.Vector3Field("Target Position", run.goal.targetPosition);
            }
            else if (a is SendThoughtAction st)
            {
                st.senderKey = EditorGUILayout.TextField("Sender Key", st.senderKey);
                st.receiverKey = EditorGUILayout.TextField("Receiver Key", st.receiverKey);
                st.thoughtType = (NarrativeThoughtType)EditorGUILayout.EnumPopup("Thought Type", st.thoughtType);
                if (st.decisionPayload == null)
                    st.decisionPayload = new NarrativeDecisionThoughtPayload();
                st.decisionPayload.proposedGoalName = EditorGUILayout.TextField("Decision Goal Name", st.decisionPayload.proposedGoalName);
                st.decisionPayload.conviction = EditorGUILayout.Slider("Decision Conviction", st.decisionPayload.conviction, 0f, 1f);
                st.decisionPayload.optionalTargetPosition = EditorGUILayout.Vector3Field("Decision Target Pos", st.decisionPayload.optionalTargetPosition);
                if (st.queryPayload == null)
                    st.queryPayload = new NarrativeQueryThoughtPayload();
                st.queryPayload.queryId = EditorGUILayout.TextField("Query Id", st.queryPayload.queryId);
                st.queryPayload.channels = (NarrativeQueryChannel)EditorGUILayout.EnumFlagsField("Query Channels", st.queryPayload.channels);
            }
            else if (a is NarrativeEnterSlowTimeGambitAction enterGambit)
            {
                enterGambit.sessionKey = EditorGUILayout.TextField("Session Key", enterGambit.sessionKey);
                enterGambit.modeFilter = (PathingApertureMode)EditorGUILayout.EnumPopup("Mode Filter", enterGambit.modeFilter);
                enterGambit.tagFilter = EditorGUILayout.TextField("Tag Filter", enterGambit.tagFilter);
                enterGambit.timeScaleCoefficient = EditorGUILayout.Slider("Time Scale", enterGambit.timeScaleCoefficient, 0f, 1f);
                enterGambit.enforcement01 = EditorGUILayout.Slider("Enforcement", enterGambit.enforcement01, 0f, 1f);
            }
            else if (a is NarrativeChooseGambitApertureAction chooseGambit)
            {
                chooseGambit.sessionKey = EditorGUILayout.TextField("Session Key", chooseGambit.sessionKey);
                chooseGambit.selectedApertureKey = EditorGUILayout.TextField("Selected Aperture Key", chooseGambit.selectedApertureKey);
                chooseGambit.apertureRegistryKey = EditorGUILayout.TextField("Registry Key", chooseGambit.apertureRegistryKey);
                chooseGambit.enforcement01 = EditorGUILayout.Slider("Enforcement", chooseGambit.enforcement01, 0f, 1f);
                chooseGambit.requirePlayerConfirm = EditorGUILayout.Toggle("Require Player Confirm", chooseGambit.requirePlayerConfirm);
                chooseGambit.timeoutUnscaledSeconds = EditorGUILayout.FloatField("Timeout (unscaled)", chooseGambit.timeoutUnscaledSeconds);
            }
            else if (a is NarrativeCommitGambitPathAction commitGambit)
            {
                commitGambit.sessionKey = EditorGUILayout.TextField("Session Key", commitGambit.sessionKey);
            }
            else if (a is NarrativeEnterSlowTimeWrestlingAction enterWrestle)
            {
                enterWrestle.sessionKey = EditorGUILayout.TextField("Session Key", enterWrestle.sessionKey);
                enterWrestle.considerKey = EditorGUILayout.TextField("Consider Key", enterWrestle.considerKey);
                enterWrestle.opponentKey = EditorGUILayout.TextField("Opponent Key", enterWrestle.opponentKey);
                enterWrestle.mode = (WrestlingMode)EditorGUILayout.EnumPopup("Mode", enterWrestle.mode);
                enterWrestle.timeScaleCoefficient = EditorGUILayout.Slider("Time Scale", enterWrestle.timeScaleCoefficient, 0f, 1f);
            }
            else if (a is NarrativeChooseWrestlingCardAction chooseWrestle)
            {
                chooseWrestle.sessionKey = EditorGUILayout.TextField("Session Key", chooseWrestle.sessionKey);
                chooseWrestle.requirePlayerConfirm = EditorGUILayout.Toggle("Require Player Confirm", chooseWrestle.requirePlayerConfirm);
                chooseWrestle.timeoutUnscaledSeconds = EditorGUILayout.FloatField("Timeout (unscaled)", chooseWrestle.timeoutUnscaledSeconds);
            }
            else if (a is NarrativeCommitWrestlingCardAction commitWrestle)
            {
                commitWrestle.sessionKey = EditorGUILayout.TextField("Session Key", commitWrestle.sessionKey);
            }
            else if (a is NarrativeWrestlingBioRhythmAction bioWrestle)
            {
                bioWrestle.actorKey = EditorGUILayout.TextField("Actor Key", bioWrestle.actorKey);
                bioWrestle.opponentKey = EditorGUILayout.TextField("Opponent Key", bioWrestle.opponentKey);
                bioWrestle.mode = (WrestlingMode)EditorGUILayout.EnumPopup("Mode", bioWrestle.mode);
                bioWrestle.bioRhythmAmplitudeDelta = EditorGUILayout.FloatField("Bio Rhythm Δ", bioWrestle.bioRhythmAmplitudeDelta);
                bioWrestle.adrenalineChannelDelta = EditorGUILayout.FloatField("Adrenaline Δ", bioWrestle.adrenalineChannelDelta);
                bioWrestle.durationSeconds = EditorGUILayout.FloatField("Duration", bioWrestle.durationSeconds);
                bioWrestle.queueWrestlingGoal = EditorGUILayout.Toggle("Queue Wrestling Goal", bioWrestle.queueWrestlingGoal);
            }
            else if (a is NarrativeEnterSlowTimeLoveMakingAction enterLove)
            {
                enterLove.sessionKey = EditorGUILayout.TextField("Session Key", enterLove.sessionKey);
                enterLove.considerKey = EditorGUILayout.TextField("Consider Key", enterLove.considerKey);
                enterLove.partnerKey = EditorGUILayout.TextField("Partner Key", enterLove.partnerKey);
                enterLove.mode = (LoveMakingMode)EditorGUILayout.EnumPopup("Mode", enterLove.mode);
                enterLove.timeScaleCoefficient = EditorGUILayout.Slider("Time Scale", enterLove.timeScaleCoefficient, 0f, 1f);
            }
            else if (a is NarrativeChooseLoveMakingCardAction chooseLove)
            {
                chooseLove.sessionKey = EditorGUILayout.TextField("Session Key", chooseLove.sessionKey);
                chooseLove.requirePlayerConfirm = EditorGUILayout.Toggle("Require Player Confirm", chooseLove.requirePlayerConfirm);
                chooseLove.timeoutUnscaledSeconds = EditorGUILayout.FloatField("Timeout (unscaled)", chooseLove.timeoutUnscaledSeconds);
            }
            else if (a is NarrativeCommitLoveMakingCardAction commitLove)
            {
                commitLove.sessionKey = EditorGUILayout.TextField("Session Key", commitLove.sessionKey);
            }
            else if (a is NarrativeLoveMakingBioRhythmAction bioLove)
            {
                bioLove.actorKey = EditorGUILayout.TextField("Actor Key", bioLove.actorKey);
                bioLove.partnerKey = EditorGUILayout.TextField("Partner Key", bioLove.partnerKey);
                bioLove.mode = (LoveMakingMode)EditorGUILayout.EnumPopup("Mode", bioLove.mode);
                bioLove.queueLoveMakingGoal = EditorGUILayout.Toggle("Queue LoveMaking Goal", bioLove.queueLoveMakingGoal);
            }
            else if (a is NarrativeEnterSlowTimeCombatAction enterCombat)
            {
                enterCombat.sessionKey = EditorGUILayout.TextField("Session Key", enterCombat.sessionKey);
                enterCombat.considerKey = EditorGUILayout.TextField("Consider Key", enterCombat.considerKey);
                enterCombat.targetKey = EditorGUILayout.TextField("Target Key", enterCombat.targetKey);
                enterCombat.mode = (CombatMode)EditorGUILayout.EnumPopup("Mode", enterCombat.mode);
                enterCombat.timeScaleCoefficient = EditorGUILayout.Slider("Time Scale", enterCombat.timeScaleCoefficient, 0f, 1f);
            }
            else if (a is NarrativeChooseCombatCardAction chooseCombat)
            {
                chooseCombat.sessionKey = EditorGUILayout.TextField("Session Key", chooseCombat.sessionKey);
                chooseCombat.requirePlayerConfirm = EditorGUILayout.Toggle("Require Player Confirm", chooseCombat.requirePlayerConfirm);
                chooseCombat.timeoutUnscaledSeconds = EditorGUILayout.FloatField("Timeout (unscaled)", chooseCombat.timeoutUnscaledSeconds);
            }
            else if (a is NarrativeCommitCombatCardAction commitCombat)
            {
                commitCombat.sessionKey = EditorGUILayout.TextField("Session Key", commitCombat.sessionKey);
            }
            else if (a is CombatCommuniqueNarrativeAction communique)
            {
                communique.facilitatorKey = EditorGUILayout.TextField("Facilitator Key", communique.facilitatorKey);
                communique.issuerKey = EditorGUILayout.TextField("Issuer Key", communique.issuerKey);
                communique.troupeId = EditorGUILayout.TextField("Troupe Id", communique.troupeId);
                communique.channel = (CombatCommuniqueChannel)EditorGUILayout.EnumPopup("Channel", communique.channel);
                communique.callToArms = EditorGUILayout.Toggle("Call To Arms", communique.callToArms);
                communique.ignoreCallToArmsRange = EditorGUILayout.Toggle("Ignore Range", communique.ignoreCallToArmsRange);
                communique.dialogueSpanRef = EditorGUILayout.TextField("Dialogue Span", communique.dialogueSpanRef);
            }
            else if (a is NarrativeTradeAction trade)
            {
                trade.selfKey = EditorGUILayout.TextField("Self Key", trade.selfKey);
                trade.otherKey = EditorGUILayout.TextField("Other Key", trade.otherKey);
                trade.faceDistance = EditorGUILayout.FloatField("Face Distance", trade.faceDistance);
                trade.requireConversationBeforeTransfer = EditorGUILayout.Toggle("Require Conversation", trade.requireConversationBeforeTransfer);
                trade.aiAutoAccept = EditorGUILayout.Toggle("AI Auto Accept", trade.aiAutoAccept);
                trade.iconMode = (TradeIconMode)EditorGUILayout.EnumPopup("Icon Mode", trade.iconMode);
            }
            else if (a is BiteNarrativeAction bite)
            {
                bite.actorKey = EditorGUILayout.TextField("Actor Key", bite.actorKey);
                bite.duration = EditorGUILayout.FloatField("Duration", bite.duration);
            }
            else if (a is ChewNarrativeAction chew)
            {
                chew.actorKey = EditorGUILayout.TextField("Actor Key", chew.actorKey);
                chew.duration = EditorGUILayout.FloatField("Duration", chew.duration);
            }
            else if (a is SwallowNarrativeAction swallow)
            {
                swallow.actorKey = EditorGUILayout.TextField("Actor Key", swallow.actorKey);
                swallow.foodKey = EditorGUILayout.TextField("Food Key", swallow.foodKey);
                swallow.duration = EditorGUILayout.FloatField("Duration", swallow.duration);
            }
            else if (a is AnimationChewNarrativeAction animChew)
            {
                animChew.actorKey = EditorGUILayout.TextField("Actor Key", animChew.actorKey);
                animChew.animationGroupTag = EditorGUILayout.TextField("Anim Group Tag", animChew.animationGroupTag);
                animChew.duration = EditorGUILayout.FloatField("Duration", animChew.duration);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(tree, "Edit Narrative Action");
                EditorUtility.SetDirty(tree);
            }
        }

        private enum NarrativeActionKind
        {
            CallMethod,
            SetProperty,
            SpawnPrefab,
            RunBehaviorTree,
            SendThought,
            EnterSlowTimeGambit,
            ChooseGambitAperture,
            CommitGambitPath,
            EnterSlowTimeWrestling,
            ChooseWrestlingCard,
            CommitWrestlingCard,
            WrestlingBioRhythm,
            EnterSlowTimeLoveMaking,
            ChooseLoveMakingCard,
            CommitLoveMakingCard,
            LoveMakingBioRhythm,
            EnterSlowTimeCombat,
            ChooseCombatCard,
            CommitCombatCard,
            CombatCommunique,
            Trade,
            Bite,
            Chew,
            Swallow,
            AnimationChew
        }

        private static NarrativeActionKind GetKind(NarrativeActionSpec a)
        {
            return a switch
            {
                SpawnPrefabAction => NarrativeActionKind.SpawnPrefab,
                SetPropertyAction => NarrativeActionKind.SetProperty,
                CallMethodAction => NarrativeActionKind.CallMethod,
                RunBehaviorTreeAction => NarrativeActionKind.RunBehaviorTree,
                SendThoughtAction => NarrativeActionKind.SendThought,
                NarrativeEnterSlowTimeGambitAction => NarrativeActionKind.EnterSlowTimeGambit,
                NarrativeChooseGambitApertureAction => NarrativeActionKind.ChooseGambitAperture,
                NarrativeCommitGambitPathAction => NarrativeActionKind.CommitGambitPath,
                NarrativeEnterSlowTimeWrestlingAction => NarrativeActionKind.EnterSlowTimeWrestling,
                NarrativeChooseWrestlingCardAction => NarrativeActionKind.ChooseWrestlingCard,
                NarrativeCommitWrestlingCardAction => NarrativeActionKind.CommitWrestlingCard,
                NarrativeWrestlingBioRhythmAction => NarrativeActionKind.WrestlingBioRhythm,
                NarrativeEnterSlowTimeLoveMakingAction => NarrativeActionKind.EnterSlowTimeLoveMaking,
                NarrativeChooseLoveMakingCardAction => NarrativeActionKind.ChooseLoveMakingCard,
                NarrativeCommitLoveMakingCardAction => NarrativeActionKind.CommitLoveMakingCard,
                NarrativeLoveMakingBioRhythmAction => NarrativeActionKind.LoveMakingBioRhythm,
                NarrativeEnterSlowTimeCombatAction => NarrativeActionKind.EnterSlowTimeCombat,
                NarrativeChooseCombatCardAction => NarrativeActionKind.ChooseCombatCard,
                NarrativeCommitCombatCardAction => NarrativeActionKind.CommitCombatCard,
                CombatCommuniqueNarrativeAction => NarrativeActionKind.CombatCommunique,
                NarrativeTradeAction => NarrativeActionKind.Trade,
                BiteNarrativeAction => NarrativeActionKind.Bite,
                ChewNarrativeAction => NarrativeActionKind.Chew,
                SwallowNarrativeAction => NarrativeActionKind.Swallow,
                AnimationChewNarrativeAction => NarrativeActionKind.AnimationChew,
                _ => NarrativeActionKind.CallMethod
            };
        }

        private static void DrawNarrativeValue(string label, ref NarrativeValue v)
        {
            v.type = (NarrativeValueType)EditorGUILayout.EnumPopup(label + " Type", v.type);
            switch (v.type)
            {
                case NarrativeValueType.Bool:
                    v.boolValue = EditorGUILayout.Toggle(label, v.boolValue);
                    break;
                case NarrativeValueType.Int:
                    v.intValue = EditorGUILayout.IntField(label, v.intValue);
                    break;
                case NarrativeValueType.Float:
                    v.floatValue = EditorGUILayout.FloatField(label, v.floatValue);
                    break;
                case NarrativeValueType.String:
                    v.stringValue = EditorGUILayout.TextField(label, v.stringValue);
                    break;
                case NarrativeValueType.Vector3:
                    v.vector3Value = EditorGUILayout.Vector3Field(label, v.vector3Value);
                    break;
                case NarrativeValueType.ObjectKey:
                    v.objectKey = EditorGUILayout.TextField(label + " Key", v.objectKey);
                    break;
            }
        }
    }

    internal sealed class NarrativeGraphView : GraphView
    {
        public Action<NarrativeNode> OnNodeSelected;

        private NarrativeTreeAsset tree;
        private readonly Dictionary<string, NarrativeGraphNode> nodesById = new Dictionary<string, NarrativeGraphNode>();

        public NarrativeGraphView()
        {
            style.flexGrow = 1f;
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        }

        public void Populate(NarrativeTreeAsset tree)
        {
            this.tree = tree;
            DeleteElements(graphElements.ToList());
            nodesById.Clear();

            if (tree == null || tree.root == null)
                return;

            // Build nodes
            int depth = 0;
            BuildNodeRecursive(tree.root, depth, 0);

            // Build edges
            ConnectRecursive(tree.root);
        }

        private int BuildNodeRecursive(NarrativeNode n, int depth, int siblingIndex)
        {
            var gn = new NarrativeGraphNode(n);
            gn.SetPosition(new Rect(40 + depth * 240, 40 + siblingIndex * 120, 200, 80));
            gn.OnSelectedCallback = () => OnNodeSelected?.Invoke(n);

            AddElement(gn);
            nodesById[n.id] = gn;

            int nextSibling = siblingIndex + 1;
            if (n is NarrativeSequenceNode seq && seq.children != null)
            {
                for (int i = 0; i < seq.children.Count; i++)
                    nextSibling = BuildNodeRecursive(seq.children[i], depth + 1, nextSibling);
            }
            else if (n is NarrativeSelectorNode sel && sel.children != null)
            {
                for (int i = 0; i < sel.children.Count; i++)
                    nextSibling = BuildNodeRecursive(sel.children[i], depth + 1, nextSibling);
            }

            return nextSibling;
        }

        private void ConnectRecursive(NarrativeNode n)
        {
            if (n == null) return;
            if (!nodesById.TryGetValue(n.id, out var parentNode)) return;

            if (n is NarrativeSequenceNode seq && seq.children != null)
            {
                for (int i = 0; i < seq.children.Count; i++)
                {
                    var child = seq.children[i];
                    if (child == null) continue;
                    Connect(parentNode, child);
                    ConnectRecursive(child);
                }
            }
            else if (n is NarrativeSelectorNode sel && sel.children != null)
            {
                for (int i = 0; i < sel.children.Count; i++)
                {
                    var child = sel.children[i];
                    if (child == null) continue;
                    Connect(parentNode, child);
                    ConnectRecursive(child);
                }
            }
        }

        private void Connect(NarrativeGraphNode parent, NarrativeNode child)
        {
            if (!nodesById.TryGetValue(child.id, out var childNode)) return;

            var edge = parent.output.ConnectTo(childNode.input);
            AddElement(edge);
        }
    }

    internal sealed class NarrativeGraphNode : Node
    {
        public NarrativeNode data;
        public Action OnSelectedCallback;

        public Port input;
        public Port output;

        public NarrativeGraphNode(NarrativeNode data)
        {
            this.data = data;
            title = data != null ? data.title : "Node";

            input = Port.Create<Edge>(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            input.portName = "";
            inputContainer.Add(input);

            output = Port.Create<Edge>(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
            output.portName = "";
            outputContainer.Add(output);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnSelectedCallback?.Invoke();
        }
    }
}
#endif

