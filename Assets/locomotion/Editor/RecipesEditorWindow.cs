#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Window → System Drawer → Recipes — author Recipe/Meal behavior trees.</summary>
public sealed class RecipesEditorWindow : EditorWindow
{
    enum Tab { Recipes, Meal, TearDown }

    Tab _tab = Tab.Recipes;
    Vector2 _leftScroll;
    Vector2 _mainScroll;
    RecipeBehaviorTreeAsset _recipe;
    MealRecipeBehaviorTreeAsset _meal;
    string _status = "";
    readonly List<RecipeBehaviorTreeAsset> _recipeAssets = new List<RecipeBehaviorTreeAsset>();
    readonly List<MealRecipeBehaviorTreeAsset> _mealAssets = new List<MealRecipeBehaviorTreeAsset>();

    const string RecipeFolder = "Assets/Recipes";

    [MenuItem("Window/System Drawer/Recipes", false, 520)]
    public static void ShowWindow()
    {
        var w = GetWindow<RecipesEditorWindow>("Recipes");
        w.minSize = new Vector2(880, 520);
    }

    void OnEnable() => RefreshLists();

    void RefreshLists()
    {
        _recipeAssets.Clear();
        _mealAssets.Clear();
        EnsureFolder(RecipeFolder);
        foreach (var guid in AssetDatabase.FindAssets("t:RecipeBehaviorTreeAsset", new[] { RecipeFolder, "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var a = AssetDatabase.LoadAssetAtPath<RecipeBehaviorTreeAsset>(path);
            if (a != null) _recipeAssets.Add(a);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:MealRecipeBehaviorTreeAsset", new[] { RecipeFolder, "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var a = AssetDatabase.LoadAssetAtPath<MealRecipeBehaviorTreeAsset>(path);
            if (a != null) _mealAssets.Add(a);
        }
    }

    void OnGUI()
    {
        _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "Recipes", "Meal", "Tear-down" });
        EditorGUILayout.BeginHorizontal();
        DrawLeft();
        _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
        switch (_tab)
        {
            case Tab.Recipes: DrawRecipe(); break;
            case Tab.Meal: DrawMeal(); break;
            case Tab.TearDown: DrawTearDown(); break;
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, MessageType.Info);
    }

    void DrawLeft()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(220));
        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
        if (GUILayout.Button("Refresh")) RefreshLists();
        if (_tab == Tab.Meal)
        {
            if (GUILayout.Button("+ New Meal"))
            {
                _meal = CreateAsset<MealRecipeBehaviorTreeAsset>("MealRecipe");
                RefreshLists();
            }
            foreach (var m in _mealAssets)
            {
                if (m == null) continue;
                if (GUILayout.Toggle(_meal == m, m.displayName, "Button"))
                    _meal = m;
            }
        }
        else
        {
            if (GUILayout.Button("+ New Recipe"))
            {
                _recipe = CreateAsset<RecipeBehaviorTreeAsset>("Recipe");
                RefreshLists();
            }
            foreach (var r in _recipeAssets)
            {
                if (r == null) continue;
                if (GUILayout.Toggle(_recipe == r, r.displayName, "Button"))
                    _recipe = r;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawRecipe()
    {
        _recipe = (RecipeBehaviorTreeAsset)EditorGUILayout.ObjectField("Recipe", _recipe, typeof(RecipeBehaviorTreeAsset), false);
        if (_recipe == null)
        {
            EditorGUILayout.HelpBox("Select or create a recipe.", MessageType.Info);
            return;
        }
        Undo.RecordObject(_recipe, "Edit Recipe");
        _recipe.displayName = EditorGUILayout.TextField("Name", _recipe.displayName);
        _recipe.servesAmount = EditorGUILayout.FloatField("Serves", _recipe.servesAmount);
        _recipe.tasteIntensity01 = EditorGUILayout.Slider("Taste intensity", _recipe.tasteIntensity01, 0f, 1f);

        EditorGUILayout.LabelField("Ingredients", EditorStyles.boldLabel);
        DrawIngredients(_recipe.ingredients);
        EditorGUILayout.LabelField("Commodities (specials)", EditorStyles.boldLabel);
        DrawCommodities(_recipe.commodities);
        EditorGUILayout.LabelField("Taste notes", EditorStyles.boldLabel);
        DrawTasteNotes(_recipe.tasteNotes);
        EditorGUILayout.LabelField("Steps", EditorStyles.boldLabel);
        DrawSteps(_recipe.steps);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate"))
        {
            _status = _recipe.ValidateAmounts(out var err) ? "OK" : err;
        }
        if (GUILayout.Button("Copy taste lemma"))
        {
            var token = TasteNotesApplicator.BuildLemmaToken(_recipe.tasteNotes, _recipe.tasteIntensity01);
            EditorGUIUtility.systemCopyBuffer = token;
            _status = "Copied " + token;
        }
        if (GUILayout.Button("Preview CardPlan actions"))
        {
            var plan = NarrativeMealPrepAction.PreviewPlan(_recipe);
            _status = "Actions: " + string.Join(", ", plan);
        }
        EditorGUILayout.EndHorizontal();
        EditorUtility.SetDirty(_recipe);
    }

    void DrawMeal()
    {
        _meal = (MealRecipeBehaviorTreeAsset)EditorGUILayout.ObjectField("Meal", _meal, typeof(MealRecipeBehaviorTreeAsset), false);
        if (_meal == null)
        {
            EditorGUILayout.HelpBox("Select or create a meal.", MessageType.Info);
            return;
        }
        Undo.RecordObject(_meal, "Edit Meal");
        _meal.displayName = EditorGUILayout.TextField("Name", _meal.displayName);
        _meal.servesAmount = EditorGUILayout.FloatField("Serves", _meal.servesAmount);
        _meal.platesPerActor = EditorGUILayout.FloatField("Plates / actor", _meal.platesPerActor);
        _meal.assignedActorKey = EditorGUILayout.TextField("Assigned actor", _meal.assignedActorKey);
        _meal.tableLayoutBranchKey = EditorGUILayout.TextField("Table layout key", _meal.tableLayoutBranchKey);

        EditorGUILayout.LabelField("Recipes", EditorStyles.boldLabel);
        if (_meal.recipes == null) _meal.recipes = new List<MealRecipeEntry>();
        for (int i = 0; i < _meal.recipes.Count; i++)
        {
            if (_meal.recipes[i] == null) _meal.recipes[i] = new MealRecipeEntry();
            EditorGUILayout.BeginHorizontal();
            _meal.recipes[i].recipe = (RecipeBehaviorTreeAsset)EditorGUILayout.ObjectField(
                _meal.recipes[i].recipe, typeof(RecipeBehaviorTreeAsset), false);
            _meal.recipes[i].portionsMultiplier = EditorGUILayout.FloatField(_meal.recipes[i].portionsMultiplier, GUILayout.Width(60));
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                _meal.recipes.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Recipe entry"))
            _meal.recipes.Add(new MealRecipeEntry());

        EditorGUILayout.LabelField("Meal commodities", EditorStyles.boldLabel);
        DrawCommodities(_meal.commodities);
        EditorGUILayout.LabelField("Meal taste notes", EditorStyles.boldLabel);
        DrawTasteNotes(_meal.tasteNotes);

        if (_meal.tray == null) _meal.tray = new TrayBinSettings();
        _meal.tray.maxPlateSlots = EditorGUILayout.IntField("Tray slots", _meal.tray.maxPlateSlots);
        _meal.tray.allowSinglePersonLoads = EditorGUILayout.Toggle("Single-person loads", _meal.tray.allowSinglePersonLoads);
        _meal.tray.allowSansTrayFallback = EditorGUILayout.Toggle("Sans-tray fallback", _meal.tray.allowSansTrayFallback);
        _meal.tray.placeMode = (TrayPlaceMode)EditorGUILayout.EnumPopup("Place mode", _meal.tray.placeMode);

        EditorUtility.SetDirty(_meal);
    }

    void DrawTearDown()
    {
        var settings = _meal != null ? _meal.tearDown : _recipe != null ? _recipe.tearDown : null;
        if (settings == null)
        {
            EditorGUILayout.HelpBox("Select a recipe or meal for tear-down defaults.", MessageType.Info);
            return;
        }
        settings.enableTearDown = EditorGUILayout.Toggle("Enable tear-down", settings.enableTearDown);
        settings.seasonPanMode = (ChefSeasonPanMode)EditorGUILayout.EnumPopup("Season pan mode", settings.seasonPanMode);
        settings.oilWipeAmount01 = EditorGUILayout.Slider("Oil wipe", settings.oilWipeAmount01, 0f, 1f);
        settings.seedDirtyDishes = EditorGUILayout.Toggle("Seed dirty dishes", settings.seedDirtyDishes);
        settings.emitSeasonPanCards = EditorGUILayout.Toggle("Emit season pan cards", settings.emitSeasonPanCards);
        settings.emitDishwashingCards = EditorGUILayout.Toggle("Emit dishwashing cards", settings.emitDishwashingCards);
        EditorGUILayout.HelpBox(
            "Dish zones: Dirty → Sink → Dishwasher → Dry (nearest trash → furthest). Compost optional on DishWashingStationConfig.",
            MessageType.None);
        if (_recipe != null) EditorUtility.SetDirty(_recipe);
        if (_meal != null) EditorUtility.SetDirty(_meal);
    }

    void DrawIngredients(List<RecipeIngredientSpec> list)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) list[i] = new RecipeIngredientSpec();
            if (list[i].commodity == null) list[i].commodity = new RecipeCommoditySpec();
            EditorGUILayout.BeginVertical("box");
            list[i].commodity.displayName = EditorGUILayout.TextField("Item", list[i].commodity.displayName);
            list[i].amount = EditorGUILayout.FloatField("Amount", list[i].amount);
            list[i].unit = EditorGUILayout.TextField("Unit", list[i].unit);
            list[i].commodity.specialOf = EditorGUILayout.TextField("Special of", list[i].commodity.specialOf);
            list[i].commodity.supplementable = EditorGUILayout.Toggle("Supplementable", list[i].commodity.supplementable);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create inventory item"))
                UpsertInventoryPlaceholder(list[i].commodity.ResolvedInventoryName);
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                list.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        if (GUILayout.Button("+ Ingredient"))
            list.Add(new RecipeIngredientSpec());
    }

    void DrawCommodities(List<RecipeCommoditySpec> list)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) list[i] = new RecipeCommoditySpec();
            EditorGUILayout.BeginVertical("box");
            list[i].displayName = EditorGUILayout.TextField("Display", list[i].displayName);
            list[i].specialOf = EditorGUILayout.TextField("Special of (base)", list[i].specialOf);
            list[i].supplementable = EditorGUILayout.Toggle("Supplementable", list[i].supplementable);
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                list.RemoveAt(i);
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndVertical();
        }
        if (GUILayout.Button("+ Special commodity"))
            list.Add(new RecipeCommoditySpec { displayName = "nanas classic sauce", specialOf = "nanas", supplementable = true });
    }

    void DrawTasteNotes(List<TasteNoteEntry> list)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) list[i] = new TasteNoteEntry();
            EditorGUILayout.BeginHorizontal();
            list[i].note = (TasteNoteId)EditorGUILayout.EnumPopup(list[i].note);
            list[i].intensity01 = EditorGUILayout.Slider(list[i].intensity01, 0f, 1f);
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                list.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Taste note"))
            list.Add(new TasteNoteEntry());
    }

    void DrawSteps(List<RecipeStepSpec> list)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) list[i] = new RecipeStepSpec();
            EditorGUILayout.BeginVertical("box");
            list[i].label = EditorGUILayout.TextField("Label", list[i].label);
            list[i].chefActivity = (ChefActivity)EditorGUILayout.EnumPopup("Chef activity", list[i].chefActivity);
            list[i].narrativeAction = (NarrativeMealPrepActionKind)EditorGUILayout.EnumPopup("Narrative", list[i].narrativeAction);
            list[i].durationSeconds = EditorGUILayout.FloatField("Duration", list[i].durationSeconds);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("↑", GUILayout.Width(24)) && i > 0)
            {
                var tmp = list[i - 1];
                list[i - 1] = list[i];
                list[i] = tmp;
            }
            if (GUILayout.Button("↓", GUILayout.Width(24)) && i < list.Count - 1)
            {
                var tmp = list[i + 1];
                list[i + 1] = list[i];
                list[i] = tmp;
            }
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                list.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        if (GUILayout.Button("+ Step"))
            list.Add(new RecipeStepSpec());
    }

    static void UpsertInventoryPlaceholder(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        var mgr = Object.FindFirstObjectByType<InventoryManager>();
        if (mgr == null)
        {
            EditorUtility.DisplayDialog("Recipes", "No InventoryManager in the open scene.", "OK");
            return;
        }
        mgr.NoteScriptMention(name);
        var existing = mgr.FindByName(name);
        if (existing == null)
        {
            mgr.UpsertLocal(new InventoryItem
            {
                id = System.Guid.NewGuid().ToString("N"),
                name = name,
                loadoutSetId = mgr.activeLoadoutSetId
            });
        }
        EditorUtility.SetDirty(mgr);
    }

    static T CreateAsset<T>(string prefix) where T : ScriptableObject
    {
        EnsureFolder(RecipeFolder);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{RecipeFolder}/{prefix}.asset");
        var asset = CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
        return asset;
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string name = Path.GetFileName(folder);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
