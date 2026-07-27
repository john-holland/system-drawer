#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Card planning editor: pick a BehaviorTree, compose a saveable CardPlanAsset of
/// Partial&lt;Card&gt;-style configs, add defaults from a horizontal chip bar, and reorder
/// a vertical encounter list with Sequence/Selector branch bars.
/// </summary>
public sealed class CardPlanningEditorWindow : EditorWindow
{
    CardPlanAsset _plan;
    BehaviorTree _behaviorTree;
    PhysicsCardSolver _cardSolver;

    Vector2 _defaultsScroll;
    Vector2 _planScroll;
    Vector2 _detailScroll;

    CardPlanNode _selected;
    List<CardPlanNode> _selectedParent;
    int _selectedIndex = -1;

    // Tree sibling reorder (hotControl-owned drag)
    List<CardPlanNode> _dragList;
    int _dragFrom = -1;
    int _dragInsert = -1; // insert-before index within _dragList
    int _dragHotControl;
    readonly List<TreeRowHit> _rowHits = new List<TreeRowHit>();

    struct TreeRowHit
    {
        public List<CardPlanNode> List;
        public int Index;
        public Rect Row;
        public Rect Handle;
    }

    static readonly Color BarColor = new Color(0.35f, 0.55f, 0.75f, 0.85f);
    static readonly Color ChoiceBarColor = new Color(0.75f, 0.55f, 0.25f, 0.9f);
    const float ChipWidth = 118f;
    const float ChipHeight = 28f;
    const float IndentPx = 18f;
    const float RowHeight = 26f;
    const float DragHandleWidth = 22f;

    [MenuItem("Window/System Drawer/Physics/Card Planning Editor", false, 402)]
    public static void ShowWindow()
    {
        var w = GetWindow<CardPlanningEditorWindow>("Card Planning");
        w.minSize = new Vector2(780, 520);
    }

    public static void ShowWindow(CardPlanAsset plan)
    {
        ShowWindow();
        var w = GetWindow<CardPlanningEditorWindow>();
        w.BindPlan(plan);
    }

    void BindPlan(CardPlanAsset plan)
    {
        _plan = plan;
        _selected = null;
        _selectedParent = null;
        _selectedIndex = -1;
        ClearDrag();
    }

    void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4f);

        if (_plan == null)
        {
            EditorGUILayout.HelpBox(
                "Create or assign a Card Plan asset. Defaults below add Wrestling / Sit / Goal / Action / Tree nodes.",
                MessageType.Info);
            DrawDefaultsBar();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.58f));
        DrawDefaultsBar();
        EditorGUILayout.Space(6f);
        DrawPlanTree();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        DrawDetailPanel();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUI.BeginChangeCheck();
        var next = (CardPlanAsset)EditorGUILayout.ObjectField(
            _plan, typeof(CardPlanAsset), false, GUILayout.Width(220f));
        if (EditorGUI.EndChangeCheck())
            BindPlan(next);

        if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(44f)))
            CreateNewPlanAsset();

        GUILayout.Space(8f);
        _behaviorTree = (BehaviorTree)EditorGUILayout.ObjectField(
            _behaviorTree, typeof(BehaviorTree), true, GUILayout.Width(200f));
        if (_behaviorTree == null)
            EditorGUILayout.LabelField("Behavior Tree", EditorStyles.miniLabel, GUILayout.Width(90f));

        _cardSolver = (PhysicsCardSolver)EditorGUILayout.ObjectField(
            _cardSolver, typeof(PhysicsCardSolver), true, GUILayout.Width(180f));

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Apply → BT", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            ApplyToBehaviorTree();
        if (GUILayout.Button("Apply → Solver", EditorStyles.toolbarButton, GUILayout.Width(100f)))
            ApplyToSolver();
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50f)))
            SavePlan();

        EditorGUILayout.EndHorizontal();

        if (_plan != null)
        {
            EditorGUI.BeginChangeCheck();
            _plan.planName = EditorGUILayout.TextField("Plan Name", _plan.planName);
            _plan.defaultGoalType = (GoalType)EditorGUILayout.EnumPopup("Default Goal", _plan.defaultGoalType);
            if (EditorGUI.EndChangeCheck())
                MarkDirty();
        }
    }

    void DrawDefaultsBar()
    {
        EditorGUILayout.LabelField("Defaults (scroll sideways — click to add)", EditorStyles.boldLabel);
        var chips = BuildDefaultChips();
        float totalW = chips.Count * (ChipWidth + 4f) + 8f;
        float viewH = ChipHeight + 18f;

        // Reserve the bar rect so we can map mouse-wheel (vertical) → horizontal scroll.
        var barRect = GUILayoutUtility.GetRect(0f, viewH, GUILayout.ExpandWidth(true));
        HandleDefaultsBarMouseWheel(barRect, totalW);

        GUI.BeginGroup(barRect);
        var viewRect = new Rect(0f, 0f, barRect.width, barRect.height);
        var contentRect = new Rect(0f, 0f, Mathf.Max(totalW, barRect.width), ChipHeight + 4f);
        _defaultsScroll = GUI.BeginScrollView(viewRect, _defaultsScroll, contentRect, false, false);

        float x = 0f;
        for (int i = 0; i < chips.Count; i++)
        {
            var chip = chips[i];
            var r = new Rect(x, 2f, ChipWidth, ChipHeight);
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = chip.tint;
            if (GUI.Button(r, new GUIContent(chip.label, chip.tooltip)))
            {
                if (_plan != null)
                    AddNode(chip.factory());
                else
                    EditorUtility.DisplayDialog("Card Planning", "Assign or create a Card Plan asset first.", "OK");
            }
            GUI.backgroundColor = prev;
            x += ChipWidth + 4f;
        }

        GUI.EndScrollView();
        GUI.EndGroup();
    }

    void HandleDefaultsBarMouseWheel(Rect barRect, float contentWidth)
    {
        var e = Event.current;
        if (e.type != EventType.ScrollWheel || !barRect.Contains(e.mousePosition))
            return;

        float maxX = Mathf.Max(0f, contentWidth - barRect.width);
        // Unity scroll delta.y is typically ±3; invert so wheel-up moves left-to-right content naturally.
        _defaultsScroll.x = Mathf.Clamp(_defaultsScroll.x + e.delta.y * 20f, 0f, maxX);
        e.Use();
        Repaint();
    }

    void DrawPlanTree()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Encounter plan ({CountNodes(_plan.roots)} nodes)", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Add Choice under selection", GUILayout.Width(180f)))
            AddChoiceUnderSelection();
        if (GUILayout.Button("Remove", GUILayout.Width(70f)))
            RemoveSelected();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Tree — drag ≡ to reorder siblings", EditorStyles.miniBoldLabel);
        _planScroll = EditorGUILayout.BeginScrollView(_planScroll, GUILayout.ExpandHeight(true));

        // One stable hotControl id for the whole tree drag session.
        int treeDragId = GUIUtility.GetControlID("CardPlanTreeDrag".GetHashCode(), FocusType.Passive);

        // Rebuild hit targets every IMGUI pass so drag uses current scroll layout.
        _rowHits.Clear();
        DrawNodes(_plan.roots, 0, _plan.roots, treeDragId);
        ProcessTreeDrag(treeDragId);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Branch preview", EditorStyles.miniBoldLabel);
        EditorGUILayout.TextArea(_plan.FormatBranchPreview(), GUILayout.MinHeight(64f));
        EditorGUILayout.EndScrollView();
    }

    void DrawNodes(List<CardPlanNode> list, int depth, List<CardPlanNode> parentList, int treeDragId)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            var node = list[i];
            if (node == null) continue;
            DrawNodeRow(node, depth, parentList, i, treeDragId);
            if (node.foldedOut && node.children != null && node.children.Count > 0)
                DrawNodes(node.children, depth + 1, node.children, treeDragId);
        }
    }

    void DrawNodeRow(CardPlanNode node, int depth, List<CardPlanNode> parentList, int index, int treeDragId)
    {
        var row = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
        float x = row.x + depth * IndentPx;

        // Vertical branch bar
        if (depth > 0)
        {
            var bar = new Rect(row.x + (depth - 1) * IndentPx + 6f, row.y, 3f, row.height);
            EditorGUI.DrawRect(bar, node.kind == CardPlanNodeKind.Choice ? ChoiceBarColor : BarColor);
            var pipeRect = new Rect(x, row.y, 12f, row.height);
            GUI.Label(pipeRect, "|", EditorStyles.miniLabel);
            x += 10f;
        }

        bool selected = _selected == node;
        bool draggingThis = _dragList == parentList && _dragFrom == index && _dragHotControl != 0;
        if (selected)
            EditorGUI.DrawRect(row, new Color(0.24f, 0.36f, 0.5f, 0.35f));
        if (draggingThis)
            EditorGUI.DrawRect(row, new Color(0.2f, 0.6f, 0.9f, 0.25f));

        // Drag handle (wide enough to grab)
        var handleRect = new Rect(x, row.y, DragHandleWidth, row.height);
        EditorGUI.LabelField(handleRect, "≡", EditorStyles.boldLabel);
        EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.Pan);
        _rowHits.Add(new TreeRowHit
        {
            List = parentList,
            Index = index,
            Row = row,
            Handle = handleRect
        });

        // Drop insert line (before this row)
        if (_dragList == parentList && _dragInsert == index && _dragFrom >= 0 && _dragFrom != index)
            EditorGUI.DrawRect(new Rect(row.x, row.y - 1f, row.width, 3f), Color.cyan);

        x += DragHandleWidth;

        // Foldout for containers
        if (node.IsBranchContainer || (node.children != null && node.children.Count > 0))
        {
            var foldRect = new Rect(x, row.y, 16f, row.height);
            node.foldedOut = EditorGUI.Foldout(foldRect, node.foldedOut, GUIContent.none, true);
            x += 16f;
        }

        var kindRect = new Rect(x, row.y + 3f, 72f, row.height - 6f);
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = KindTint(node.kind);
        GUI.Box(kindRect, node.kind.ToString(), EditorStyles.miniButton);
        GUI.backgroundColor = prevBg;
        x += 76f;

        var labelRect = new Rect(x, row.y, row.xMax - x - 4f, row.height);
        // Use Button only when not dragging so it doesn't steal the mouse.
        if (_dragHotControl == 0)
        {
            if (GUI.Button(labelRect, node.DisplayLabel, selected ? EditorStyles.boldLabel : EditorStyles.label))
                Select(parentList, index);
        }
        else
        {
            GUI.Label(labelRect, node.DisplayLabel, selected ? EditorStyles.boldLabel : EditorStyles.label);
        }

        // Insert-after marker for last sibling
        if (_dragList == parentList && _dragInsert == parentList.Count && index == parentList.Count - 1 && _dragFrom >= 0)
            EditorGUI.DrawRect(new Rect(row.x, row.yMax - 1f, row.width, 3f), Color.cyan);

        BeginTreeDragIfNeeded(handleRect, parentList, index, treeDragId);
    }

    void BeginTreeDragIfNeeded(Rect handleRect, List<CardPlanNode> parentList, int index, int treeDragId)
    {
        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && handleRect.Contains(e.mousePosition))
        {
            GUIUtility.hotControl = treeDragId;
            _dragHotControl = treeDragId;
            _dragList = parentList;
            _dragFrom = index;
            _dragInsert = index;
            Select(parentList, index);
            e.Use();
            Repaint();
        }
    }

    void ProcessTreeDrag(int treeDragId)
    {
        if (_dragList == null || _dragFrom < 0)
            return;

        var e = Event.current;
        bool ours = GUIUtility.hotControl == treeDragId || _dragHotControl == treeDragId;
        if (!ours)
            return;

        if (e.type == EventType.MouseDrag)
        {
            GUIUtility.hotControl = treeDragId;
            _dragHotControl = treeDragId;
            UpdateDragInsert(e.mousePosition);
            e.Use();
            Repaint();
            return;
        }

        if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
        {
            UpdateDragInsert(e.mousePosition);
            CommitTreeDrag();
            if (GUIUtility.hotControl == treeDragId)
                GUIUtility.hotControl = 0;
            ClearDrag();
            e.Use();
            Repaint();
        }
    }

    void UpdateDragInsert(Vector2 mousePos)
    {
        if (_dragList == null) return;

        int bestInsert = _dragFrom;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _rowHits.Count; i++)
        {
            var hit = _rowHits[i];
            if (hit.List != _dragList) continue;

            // Distance to top edge = insert before; bottom edge = insert after.
            float topDist = Mathf.Abs(mousePos.y - hit.Row.yMin);
            float botDist = Mathf.Abs(mousePos.y - hit.Row.yMax);

            if (topDist < bestDist)
            {
                bestDist = topDist;
                bestInsert = hit.Index;
            }
            if (botDist < bestDist)
            {
                bestDist = botDist;
                bestInsert = hit.Index + 1;
            }
        }

        _dragInsert = Mathf.Clamp(bestInsert, 0, _dragList.Count);
    }

    void CommitTreeDrag()
    {
        if (_plan == null || _dragList == null || _dragFrom < 0)
            return;
        if (_dragFrom >= _dragList.Count)
            return;

        int insert = _dragInsert;
        if (insert > _dragFrom)
            insert--;
        insert = Mathf.Clamp(insert, 0, _dragList.Count - 1);
        if (insert == _dragFrom)
            return;

        Undo.RecordObject(_plan, "Reorder Card Plan Node");
        var item = _dragList[_dragFrom];
        _dragList.RemoveAt(_dragFrom);
        _dragList.Insert(insert, item);
        EditorUtility.SetDirty(_plan);
        Select(_dragList, insert);
    }

    void ClearDrag()
    {
        _dragList = null;
        _dragFrom = -1;
        _dragInsert = -1;
        _dragHotControl = 0;
    }

    void DrawDetailPanel()
    {
        EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

        if (_selected == null)
        {
            EditorGUILayout.HelpBox("Select a plan node to edit its card partial fields, goal, or action.", MessageType.None);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUI.BeginChangeCheck();
        _selected.kind = (CardPlanNodeKind)EditorGUILayout.EnumPopup("Kind", _selected.kind);
        _selected.label = EditorGUILayout.TextField("Label", _selected.label);

        switch (_selected.kind)
        {
            case CardPlanNodeKind.Card:
                DrawCardPartialInspector(_selected);
                break;
            case CardPlanNodeKind.Goal:
                _selected.goalType = (GoalType)EditorGUILayout.EnumPopup("Goal Type", _selected.goalType);
                break;
            case CardPlanNodeKind.Action:
                _selected.actionKind = (CardPlanActionKind)EditorGUILayout.EnumPopup("Action", _selected.actionKind);
                break;
            case CardPlanNodeKind.Sequence:
            case CardPlanNodeKind.Selector:
            case CardPlanNodeKind.Choice:
                EditorGUILayout.HelpBox(
                    "Children render as indented |choice rows. Use defaults bar or Add Choice under selection.",
                    MessageType.Info);
                if (GUILayout.Button("Add Child Choice"))
                {
                    _selected.children.Add(CardPlanNode.NewTree(CardPlanNodeKind.Choice, $"choice {_selected.children.Count + 1}"));
                    _selected.foldedOut = true;
                }
                break;
        }

        if (EditorGUI.EndChangeCheck())
            MarkDirty();

        EditorGUILayout.EndScrollView();
    }

    void DrawCardPartialInspector(CardPlanNode node)
    {
        if (node.cardPartial == null)
            node.cardPartial = new CardPartial();
        var p = node.cardPartial;
        p.displayName = EditorGUILayout.TextField("Display Name", p.displayName);

        EditorGUILayout.LabelField("Template type", p.CardType.Name);
        if (p.template == null)
        {
            EditorGUILayout.HelpBox("No template. Re-add from Defaults or pick a type below.", MessageType.Warning);
            DrawNewTemplateButtons(p);
            return;
        }

        // Common GoodSection fields
        var card = p.template;
        card.sectionName = EditorGUILayout.TextField("Section Name", card.sectionName);
        card.description = EditorGUILayout.TextField("Description", card.description);
        card.physicalPathingTag = EditorGUILayout.TextField("Pathing Tag", card.physicalPathingTag);

        if (card is WrestlingCard w)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Wrestling Partial", EditorStyles.boldLabel);
            w.mode = (WrestlingMode)EditorGUILayout.EnumPopup("Mode", w.mode);
            w.moveKind = (WrestlingMoveKind)EditorGUILayout.EnumPopup("Move", w.moveKind);
            w.professionalStyle = EditorGUILayout.Toggle("Professional", w.professionalStyle);
            w.hotkey = (KeyCode)EditorGUILayout.EnumPopup("Hotkey", w.hotkey);
            w.inputActionName = EditorGUILayout.TextField("Input Action", w.inputActionName);
            w.dropHitBoneName = EditorGUILayout.TextField("Drop Hit Bone", w.dropHitBoneName);
            w.liftBranch = (WrestlingMoveKind)EditorGUILayout.EnumPopup("Lift Branch", w.liftBranch);
            w.throwBranch = (WrestlingMoveKind)EditorGUILayout.EnumPopup("Throw Branch", w.throwBranch);
            w.isWrestlingGoal = true;
        }
        else if (card is SitCard sit)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Sit Partial", EditorStyles.boldLabel);
            sit.occupancyMode = (SurfaceOccupancyMode)EditorGUILayout.EnumPopup("Occupancy", sit.occupancyMode);
            sit.maxHangOverDistance = EditorGUILayout.FloatField("Max Hang Over", sit.maxHangOverDistance);
            sit.safeEdgeDistance = EditorGUILayout.FloatField("Safe Edge Distance", sit.safeEdgeDistance);
            sit.isSitGoal = sit.occupancyMode != SurfaceOccupancyMode.StandOn;
            sit.isStandOnSurfaceGoal = sit.occupancyMode == SurfaceOccupancyMode.StandOn;
        }
        else if (card is HemisphericalGraspCard grasp)
        {
            EditorGUILayout.LabelField("Grasp card template", EditorStyles.miniLabel);
            grasp.sectionName = card.sectionName;
        }
        else if (card is TippingCard tip)
        {
            tip.tipAngle = EditorGUILayout.FloatField("Tip Angle", tip.tipAngle);
            tip.viabilityScore = EditorGUILayout.Slider("Viability", tip.viabilityScore, 0f, 1f);
            tip.requiresStabilization = EditorGUILayout.Toggle("Requires Stabilization", tip.requiresStabilization);
        }

        EditorGUILayout.Space(4f);
        card.isThrowGoalOnly = EditorGUILayout.Toggle("Throw Goal", card.isThrowGoalOnly);
        card.isCarry = EditorGUILayout.Toggle("Carry", card.isCarry);
        card.isCatchGoal = EditorGUILayout.Toggle("Catch", card.isCatchGoal);
        card.isShootGoal = EditorGUILayout.Toggle("Shoot", card.isShootGoal);
        card.isHitGoal = EditorGUILayout.Toggle("Hit", card.isHitGoal);
        EditorGUILayout.LabelField("Flying", "use pathing tag 'flying' / GoalType.Flying");
    }

    void DrawNewTemplateButtons(CardPartial p)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Wrestling"))
            p.template = WrestlingCard.Generate(WrestlingMode.Play, WrestlingMoveKind.LockGrapple, null, null);
        if (GUILayout.Button("Sit"))
            p.template = new SitCard { sectionName = "sit", isSitGoal = true };
        if (GUILayout.Button("GoodSection"))
            p.template = new GoodSection { sectionName = "card" };
        EditorGUILayout.EndHorizontal();
    }

    void Select(List<CardPlanNode> parent, int index)
    {
        if (parent == null || index < 0 || index >= parent.Count) return;
        _selectedParent = parent;
        _selectedIndex = index;
        _selected = parent[index];
        Repaint();
    }

    void AddNode(CardPlanNode node)
    {
        if (_plan == null || node == null) return;
        Undo.RecordObject(_plan, "Add Card Plan Node");
        if (_selected != null && _selected.IsBranchContainer)
        {
            _selected.children.Add(node);
            _selected.foldedOut = true;
        }
        else
        {
            _plan.roots.Add(node);
        }
        MarkDirty();
        _selected = node;
    }

    void AddChoiceUnderSelection()
    {
        if (_plan == null) return;
        var choice = CardPlanNode.NewTree(CardPlanNodeKind.Choice, "choice");
        if (_selected != null && _selected.IsBranchContainer)
        {
            Undo.RecordObject(_plan, "Add Choice");
            _selected.children.Add(choice);
            _selected.foldedOut = true;
        }
        else if (_selected != null && _selectedParent != null)
        {
            // Wrap selection's parent as selector if needed — simpler: append choice as sibling under nearest selector or create selector
            Undo.RecordObject(_plan, "Add Choice");
            var sel = CardPlanNode.NewTree(CardPlanNodeKind.Selector, "Selector");
            int idx = _selectedIndex;
            _selectedParent[idx] = sel;
            sel.children.Add(_selected);
            sel.children.Add(choice);
            _selected = sel;
        }
        else
        {
            AddNode(choice);
            return;
        }
        MarkDirty();
    }

    void RemoveSelected()
    {
        if (_plan == null || _selectedParent == null || _selectedIndex < 0) return;
        Undo.RecordObject(_plan, "Remove Card Plan Node");
        _selectedParent.RemoveAt(_selectedIndex);
        _selected = null;
        _selectedParent = null;
        _selectedIndex = -1;
        MarkDirty();
    }

    void ApplyToBehaviorTree()
    {
        if (_plan == null)
            return;
        if (_behaviorTree == null)
        {
            EditorUtility.DisplayDialog("Card Planning", "Assign a Behavior Tree first.", "OK");
            return;
        }
        var cards = _plan.MaterializeCards();
        Undo.RecordObject(_behaviorTree, "Apply Card Plan");
        _behaviorTree.availableCards = cards;
        if (_behaviorTree.currentGoal == null)
            _behaviorTree.currentGoal = new BehaviorTreeGoal();
        _behaviorTree.currentGoal.type = _plan.defaultGoalType;
        EditorUtility.SetDirty(_behaviorTree);
        Debug.Log($"[CardPlanning] Applied {cards.Count} cards to BehaviorTree '{_behaviorTree.name}'.");
    }

    void ApplyToSolver()
    {
        if (_plan == null) return;
        if (_cardSolver == null && _behaviorTree != null)
            _cardSolver = _behaviorTree.GetComponent<PhysicsCardSolver>();
        if (_cardSolver == null)
        {
            EditorUtility.DisplayDialog("Card Planning", "Assign a PhysicsCardSolver first.", "OK");
            return;
        }
        var cards = _plan.MaterializeCards();
        Undo.RecordObject(_cardSolver, "Apply Card Plan");
        _cardSolver.AddCards(cards);
        EditorUtility.SetDirty(_cardSolver);
        Debug.Log($"[CardPlanning] Added {cards.Count} cards to solver '{_cardSolver.name}'.");
    }

    void SavePlan()
    {
        if (_plan == null) return;
        EditorUtility.SetDirty(_plan);
        AssetDatabase.SaveAssets();
    }

    void MarkDirty()
    {
        if (_plan == null) return;
        EditorUtility.SetDirty(_plan);
    }

    void CreateNewPlanAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Card Plan", "CardPlan", "asset", "Save card plan asset");
        if (string.IsNullOrEmpty(path)) return;
        var asset = CreateInstance<CardPlanAsset>();
        asset.planName = System.IO.Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        BindPlan(asset);
    }

    static int CountNodes(List<CardPlanNode> nodes)
    {
        if (nodes == null) return 0;
        int n = 0;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;
            n++;
            n += CountNodes(nodes[i].children);
        }
        return n;
    }

    static Color KindTint(CardPlanNodeKind kind)
    {
        switch (kind)
        {
            case CardPlanNodeKind.Card: return new Color(0.55f, 0.75f, 1f);
            case CardPlanNodeKind.Goal: return new Color(0.7f, 1f, 0.7f);
            case CardPlanNodeKind.Action: return new Color(1f, 0.75f, 0.55f);
            case CardPlanNodeKind.Sequence: return new Color(0.85f, 0.85f, 0.85f);
            case CardPlanNodeKind.Selector: return new Color(1f, 0.9f, 0.5f);
            case CardPlanNodeKind.Choice: return new Color(1f, 0.8f, 0.4f);
            default: return Color.white;
        }
    }

    struct DefaultChip
    {
        public string label;
        public string tooltip;
        public Color tint;
        public Func<CardPlanNode> factory;
    }

    static List<DefaultChip> BuildDefaultChips()
    {
        var list = new List<DefaultChip>();

        // Wrestling defaults
        foreach (WrestlingMoveKind move in Enum.GetValues(typeof(WrestlingMoveKind)))
        {
            var m = move;
            list.Add(new DefaultChip
            {
                label = $"W:{m}",
                tooltip = $"Add WrestlingCard partial ({m})",
                tint = new Color(0.55f, 0.7f, 1f),
                factory = () => CardPlanNode.NewCard(CardPartial.FromCard(
                    WrestlingCard.Generate(WrestlingMode.Play, m, null, null),
                    $"wrestling_{m}"))
            });
        }

        list.Add(Chip("Sit", "SitCard partial", new Color(0.65f, 0.9f, 0.7f), () =>
            CardPlanNode.NewCard(CardPartial.FromCard(new SitCard
            {
                sectionName = "sit",
                description = "Sit on surface",
                isSitGoal = true,
                occupancyMode = SurfaceOccupancyMode.Sit
            }, "sit"))));

        list.Add(Chip("StandOn", "StandOnSurfaceCard partial", new Color(0.65f, 0.9f, 0.7f), () =>
            CardPlanNode.NewCard(CardPartial.FromCard(new StandOnSurfaceCard
            {
                sectionName = "stand_on",
                isStandOnSurfaceGoal = true,
                occupancyMode = SurfaceOccupancyMode.StandOn
            }, "stand_on"))));

        list.Add(Chip("ChairRot", "ChairRotateCard", new Color(0.65f, 0.9f, 0.7f), () =>
            CardPlanNode.NewCard(CardPartial.FromCard(new ChairRotateCard
            {
                sectionName = "chair_rotate",
                isChairRotateGoal = true
            }, "chair_rotate"))));

        list.Add(Chip("Schooch", "ChairSchoochCard", new Color(0.65f, 0.9f, 0.7f), () =>
            CardPlanNode.NewCard(CardPartial.FromCard(new ChairSchoochCard
            {
                sectionName = "chair_schooch",
                isChairSchoochGoal = true
            }, "chair_schooch"))));

        list.Add(Chip("Grasp", "HemisphericalGraspCard", new Color(0.8f, 0.7f, 1f), () =>
            CardPlanNode.NewCard(CardPartial.FromCard(new HemisphericalGraspCard
            {
                sectionName = "grasp"
            }, "grasp"))));

        list.Add(Chip("Tip", "TippingCard", new Color(0.8f, 0.7f, 1f), () =>
            CardPlanNode.NewCard(CardPartial.FromCard(new TippingCard(), "tip"))));

        list.Add(Chip("Fly", "Flying GoodSection (pathing tag)", new Color(0.7f, 0.85f, 1f), () =>
            CardPlanNode.NewCard(CardPartial.FromCard(new GoodSection
            {
                sectionName = "fly",
                physicalPathingTag = "flying",
                traversabilityMode = TraversabilityMode.Custom,
                traversabilityTag = "flying"
            }, "fly"))));

        // Goals
        foreach (GoalType g in Enum.GetValues(typeof(GoalType)))
        {
            var goal = g;
            list.Add(Chip($"G:{goal}", $"GoalType.{goal}", new Color(0.7f, 1f, 0.7f),
                () => CardPlanNode.NewGoal(goal)));
        }

        // Actions
        foreach (CardPlanActionKind a in Enum.GetValues(typeof(CardPlanActionKind)))
        {
            var action = a;
            list.Add(Chip($"A:{action}", $"Action {action}", new Color(1f, 0.8f, 0.6f),
                () => CardPlanNode.NewAction(action)));
        }

        // Tree types
        list.Add(Chip("Sequence", "Sequence branch container", new Color(0.85f, 0.85f, 0.85f),
            () => CardPlanNode.NewTree(CardPlanNodeKind.Sequence)));
        list.Add(Chip("Selector", "Selector — children as |choice", new Color(1f, 0.9f, 0.5f),
            () => CardPlanNode.NewTree(CardPlanNodeKind.Selector)));
        list.Add(Chip("|choice", "Choice leaf under selector", new Color(1f, 0.85f, 0.45f),
            () => CardPlanNode.NewTree(CardPlanNodeKind.Choice, "choice")));

        return list;
    }

    static DefaultChip Chip(string label, string tip, Color tint, Func<CardPlanNode> factory) =>
        new DefaultChip { label = label, tooltip = tip, tint = tint, factory = factory };
}
#endif
