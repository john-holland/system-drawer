/// <summary>
/// Menu paths for Facilitator Hub buttons (<see cref="UnityEditor.EditorApplication.ExecuteMenuItem"/>).
/// Includes Window/System Drawer/ and Locomotion/ designer windows.
/// </summary>
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
        new Entry("Animation", "IK Webcam Video Interpretation", "Window/System Drawer/Animation/IK Webcam Video Interpretation"),
        new Entry("Animation", "Dance Animation Editor", "Window/System Drawer/Animation/Dance Animation Editor"),
        new Entry("Animation", "Animal Animation Fitting Wizard", "Window/System Drawer/Animation/Animal Animation Fitting Wizard"),
        new Entry("Travel", "Pathing Editor", "Window/System Drawer/Travel/Pathing Editor"),
        new Entry("Travel", "Airplane Designer", "Locomotion/Airplane Designer"),
        new Entry("Travel", "Magneto / Helicopter Designer", "Locomotion/Magneto / Helicopter Designer"),
        new Entry("City Planning", "City Pixel Grid Designer", "Locomotion/City Pixel Grid Designer"),
        new Entry("City Planning", "Street Blocks Designer", "Locomotion/Street Blocks Designer"),
        new Entry("City Planning", "Building Requirements", "Window/System Drawer/Civil/Building Requirements"),
        new Entry("City Planning", "House Foundation Layers", "Locomotion/House Foundation Layers"),
        new Entry("City Planning", "Wall Brush Designer", "Locomotion/Wall Brush Designer"),
        new Entry("City Planning", "House Envelope Designer", "Locomotion/House Envelope Designer"),
        new Entry("City Planning", "Window PixelLight Grid Designer", "Locomotion/Window PixelLight Grid Designer"),
        new Entry("City Planning", "House Construction Travel Agent", "Locomotion/House Construction Travel Agent"),
        new Entry("City Planning", "Park Plant Planner", "Locomotion/Park Plant Planner"),
        new Entry("City Planning", "Ladder Logic Designer", "Locomotion/Ladder Logic Designer"),
        new Entry("City Planning", "Pixel Light Timed Designer", "Locomotion/Pixel Light Timed Designer"),
        new Entry("City Planning", "Airport Pixel Light Designer", "Locomotion/Airport Pixel Light Designer"),
        new Entry("City Planning", "Prison Warden Power Diamond", "Locomotion/Prison Warden Power Diamond"),
        new Entry("City Planning", "Civilian Paper Doll", "Locomotion/Civilian Paper Doll"),
        new Entry("City Planning", "Educational Travel Agent", "Locomotion/Educational Travel Agent"),
        new Entry("City Planning", "SDF Max Composition Editor", "Window/System Drawer/SDF Max Composition Editor"),
        new Entry("Ragdoll", "Fitting Wizard", "Window/System Drawer/Ragdoll/Fitting Wizard"),
        new Entry("Ragdoll", "From-Scratch Replicator", "Window/System Drawer/Ragdoll/From-Scratch Replicator"),
        new Entry("Ragdoll", "Systems Matrix", "Window/System Drawer/Ragdoll/Systems Matrix"),
        new Entry("Hair", "Hairdo Designer", "Window/System Drawer/Hairdo Designer"),
        new Entry("Hair", "Hair Lattice Bake", "Window/System Drawer/Hair Lattice Bake"),
        new Entry("Physics", "Nervous System Impulse Viewer", "Window/System Drawer/Physics/Nervous System Impulse Viewer"),
        new Entry("Physics", "Card Game Visualizer", "Window/System Drawer/Physics/Card Game Visualizer"),
        new Entry("Physics", "Card Planning Editor", "Window/System Drawer/Physics/Card Planning Editor"),
        new Entry("Physics", "Physics Bridge Editor", "Window/System Drawer/Physics/Physics Bridge Editor"),
        new Entry("Physics", "Aquaplane Demo Setup", "Window/System Drawer/Physics/Aquaplane Demo Setup"),
        new Entry("Cards", "Active Cards", "Window/System Drawer/Active Cards"),
        new Entry("Cards", "Recipes", "Window/System Drawer/Recipes"),
        new Entry("Cards", "Combat", "Window/System Drawer/Cards/Combat"),
        new Entry("Cards", "Love", "Window/System Drawer/Cards/Love"),
        new Entry("Cards", "Wrestling", "Window/System Drawer/Cards/Wrestling"),
        new Entry("Cards", "Chef", "Window/System Drawer/Cards/Chef"),
        new Entry("Audio", "Sound Cache Generator", "Window/System Drawer/Audio/Sound Cache Generator"),
        new Entry("Music", "Composition Summary", "Window/System Drawer/Music/Composition Summary"),
        new Entry("Music", "Timeline Overlays", "Window/System Drawer/Music/Timeline Overlays"),
        new Entry("Music", "Audio Equipment Timeline", "Window/System Drawer/Music/Audio Equipment Timeline"),
        new Entry("Weather", "Weather Service Wizard", "Window/System Drawer/Weather/Service Wizard"),
        new Entry("Hygiene", "Hygiene Editor", "Window/System Drawer/Hygiene/Hygiene Editor"),
        new Entry("Look", "Paint Studio Bake", "Window/System Drawer/Paint Studio Bake"),
        new Entry("System Tests", "Audio", "Window/System Drawer/System Tests/Audio"),
        new Entry("System Tests", "Smell", "Window/System Drawer/System Tests/Smell"),
        new Entry("System Tests", "Ragdoll Cohesion", "Window/System Drawer/System Tests/Ragdoll Cohesion"),
        new Entry("Continuuuum", "Continuuuum Library", "Window/Continuuuum/Continuuuum Library"),
        new Entry("Continuuuum", "Continuuuum Explorer", "Window/Continuuuum/Continuuuum Explorer"),
        new Entry("Continuuuum", "Lemma Properties", "Window/Continuuuum/Lemma Properties"),
        new Entry("Continuuuum", "Lemma Build", "Window/System Drawer/Lemmas/Lemma Build"),
        new Entry("Continuuuum", "Notifications", "Window/Continuuuum/Notifications"),
        new Entry("Continuuuum", "Stations", "Window/System Drawer/Stations"),
        new Entry("Diagnostics", "Memory Swizzle View", "Window/System Drawer/Diagnostics/Memory Swizzle View"),
        new Entry("Diagnostics", "Perf Trace View", "Window/System Drawer/Diagnostics/Perf Trace View"),
        new Entry("Diagnostics", "Feature Budget", "Window/System Drawer/Diagnostics/Feature Budget"),
        new Entry("Diagnostics", "Perform GC Pass", "Window/System Drawer/Diagnostics/Perform GC Pass"),
        new Entry("Planet", "Import Planar Scan", "Window/System Drawer/Planet/Import Planar Scan"),
        new Entry("Planet", "Bake Composition", "Window/System Drawer/Planet/Bake Composition"),
        new Entry("Planet", "Composition UI", "Window/System Drawer/Planet/Composition UI"),
        new Entry("Planet", "Asteroid Belt", "Window/System Drawer/Planet/Asteroid Belt"),
        new Entry("Planet", "Galactic Night Sky Bake", "Window/System Drawer/Planet/Galactic Night Sky Bake"),
        new Entry("Networking", "Dedicated Server Window", "Window/System Drawer/Networking/Dedicated Server Window"),
        new Entry("Networking", "Create Main Menu Ragdoll", "Window/System Drawer/Networking/Create Main Menu Ragdoll"),
        new Entry("Networking", "Update Main Menu Network Requirements", "Window/System Drawer/Networking/Update Main Menu for Network Requirements"),
        new Entry("Networking", "Create Structured Chat Ragdoll", "Window/System Drawer/Networking/Create Structured Chat Ragdoll"),
        new Entry("Networking", "Update Structured Chat for Lexicon", "Window/System Drawer/Networking/Update Structured Chat for Lexicon"),
        new Entry("Networking", "Structured Chat Lexicon", "Window/System Drawer/Networking/Structured Chat Lexicon"),
        new Entry("Networking", "Copy Dedicated Server Launch Args", "Window/System Drawer/Networking/Copy Dedicated Server Launch Args"),
    };
}
