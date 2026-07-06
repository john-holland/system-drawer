/// <summary>Menu paths matching <see cref="UnityEditor.EditorApplication.ExecuteMenuItem"/> entries under Window/System Drawer/.</summary>
internal static class SystemDrawerHubMenuCatalog
{
    internal readonly struct Entry
    {
        public readonly string Category;
        public readonly string Label;
        public readonly string MenuPath;

        public Entry(string category, string label, string menuPath)
        {
            Category = category;
            Label = label;
            MenuPath = menuPath;
        }
    }

    internal static readonly Entry[] All =
    {
        new Entry("Narrative", "Quest Map", "Window/System Drawer/Quest Map"),
        new Entry("Narrative", "Dream Cycle", "Window/System Drawer/Dream Cycle"),
        new Entry("Narrative", "Tree Editor", "Window/System Drawer/Narrative/Tree Editor"),
        new Entry("Narrative", "Calendar Wizard", "Window/System Drawer/Narrative/Calendar Wizard"),
        new Entry("Narrative", "Calendar Timeline", "Window/System Drawer/Narrative/Calendar Timeline"),
        new Entry("Narrative", "Prompt Editor", "Window/System Drawer/Narrative/Prompt Editor"),
        new Entry("Narrative", "Prompt Tree Inspector", "Window/System Drawer/Narrative/Prompt Tree Inspector"),
        new Entry("Narrative", "Interpretation Examiner", "Window/System Drawer/Narrative/Interpretation Examiner"),
        new Entry("Narrative", "Prompt Interpreter Diff", "Window/System Drawer/Narrative/Prompt Interpreter Diff"),
        new Entry("Narrative", "Weather Event Wizard", "Window/System Drawer/Narrative/Weather Event Wizard"),
        new Entry("Animation", "Animation Hierarchy", "Window/System Drawer/Animation/Animation Hierarchy"),
        new Entry("Animation", "Behavior Tree Timeline", "Window/System Drawer/Animation/Behavior Tree Timeline"),
        new Entry("Animation", "IK Animation Training", "Window/System Drawer/Animation/IK Animation Training"),
        new Entry("Travel", "Pathing Editor", "Window/System Drawer/Travel/Pathing Editor"),
        new Entry("Ragdoll", "Fitting Wizard", "Window/System Drawer/Ragdoll/Fitting Wizard"),
        new Entry("Ragdoll", "Systems Matrix", "Window/System Drawer/Ragdoll/Systems Matrix"),
        new Entry("Physics", "Nervous System Impulse Viewer", "Window/System Drawer/Physics/Nervous System Impulse Viewer"),
        new Entry("Physics", "Card Game Visualizer", "Window/System Drawer/Physics/Card Game Visualizer"),
        new Entry("Physics", "Physics Bridge Editor", "Window/System Drawer/Physics/Physics Bridge Editor"),
        new Entry("Physics", "Aquaplane Demo Setup", "Window/System Drawer/Physics/Aquaplane Demo Setup"),
        new Entry("Audio", "Sound Cache Generator", "Window/System Drawer/Audio/Sound Cache Generator"),
        new Entry("Weather", "Weather Service Wizard", "Window/System Drawer/Weather/Service Wizard"),
        new Entry("System Tests", "Audio", "Window/System Drawer/System Tests/Audio"),
        new Entry("System Tests", "Smell", "Window/System Drawer/System Tests/Smell"),
        new Entry("System Tests", "Ragdoll Cohesion", "Window/System Drawer/System Tests/Ragdoll Cohesion"),
        new Entry("Continuuuum", "Continuuuum Library", "Window/Continuuuum/Continuuuum Library"),
        new Entry("Diagnostics", "Memory Swizzle View", "Window/System Drawer/Diagnostics/Memory Swizzle View"),
        new Entry("Diagnostics", "Perf Trace View", "Window/System Drawer/Diagnostics/Perf Trace View"),
        new Entry("Diagnostics", "Feature Budget", "Window/System Drawer/Diagnostics/Feature Budget"),
        new Entry("Diagnostics", "Perform GC Pass", "Window/System Drawer/Diagnostics/Perform GC Pass"),
        new Entry("Planet", "Import Planar Scan", "Window/System Drawer/Planet/Import Planar Scan"),
        new Entry("Planet", "Bake Composition", "Window/System Drawer/Planet/Bake Composition"),
        new Entry("Networking", "Dedicated Server Window", "Window/System Drawer/Networking/Dedicated Server Window"),
        new Entry("Networking", "Create Main Menu Ragdoll", "Window/System Drawer/Networking/Create Main Menu Ragdoll"),
        new Entry("Networking", "Update Main Menu Network Requirements", "Window/System Drawer/Networking/Update Main Menu for Network Requirements"),
        new Entry("Networking", "Copy Dedicated Server Launch Args", "Window/System Drawer/Networking/Copy Dedicated Server Launch Args"),
    };
}
