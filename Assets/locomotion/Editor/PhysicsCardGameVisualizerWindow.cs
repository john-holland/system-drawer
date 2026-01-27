using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// EditorWindow that visualizes GoodSection physics cards as a card game interface.
/// Helps identify which real-world card game mechanics are closest to the system.
/// </summary>
public class PhysicsCardGameVisualizerWindow : EditorWindow
{
    // Target objects
    private PhysicsCardSolver targetCardSolver;
    private NervousSystem targetNervousSystem;
    private BehaviorTree targetBehaviorTree;

    // Card collection
    private List<GoodSection> allCards = new List<GoodSection>();
    private List<GoodSection> filteredCards = new List<GoodSection>();
    private GoodSection selectedCard = null;

    // UI state
    private Vector2 cardScrollPosition;
    private Vector2 detailsScrollPosition;
    private Vector2 networkScrollPosition;
    private string searchText = "";
    private bool showNetworkGraph = true;
    private bool showCardGameAnalysis = true;

    // Card game analysis
    private CardGameMatch bestMatch = null;

    // Card display settings
    private float cardWidth = 200f;
    private float cardHeight = 280f;
    private int cardsPerRow = 4;

    [MenuItem("Window/Locomotion/Physics Card Game Visualizer")]
    public static void OpenWindow()
    {
        PhysicsCardGameVisualizerWindow window = GetWindow<PhysicsCardGameVisualizerWindow>();
        window.titleContent = new GUIContent("Card Game Visualizer");
        window.Show();
    }

    private void OnEnable()
    {
        RefreshCards();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Physics Card Game Visualizer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Target object selection
        DrawTargetSelection();
        EditorGUILayout.Space();

        // Card game analysis
        if (showCardGameAnalysis)
        {
            DrawCardGameAnalysis();
            EditorGUILayout.Space();
        }

        // Search and filter
        DrawSearchAndFilter();
        EditorGUILayout.Space();

        // Main content area
        EditorGUILayout.BeginHorizontal();

        // Card grid view
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f));
        DrawCardGridView();
        EditorGUILayout.EndVertical();

        // Details and network panel
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
        DrawCardDetailsPanel();
        if (showNetworkGraph)
        {
            EditorGUILayout.Space();
            DrawNetworkGraph();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTargetSelection()
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField("Target Objects:", GUILayout.Width(100f));
        
        targetCardSolver = (PhysicsCardSolver)EditorGUILayout.ObjectField(
            "Card Solver", targetCardSolver, typeof(PhysicsCardSolver), true, GUILayout.Width(200f));
        
        targetNervousSystem = (NervousSystem)EditorGUILayout.ObjectField(
            "Nervous System", targetNervousSystem, typeof(NervousSystem), true, GUILayout.Width(200f));
        
        targetBehaviorTree = (BehaviorTree)EditorGUILayout.ObjectField(
            "Behavior Tree", targetBehaviorTree, typeof(BehaviorTree), true, GUILayout.Width(200f));

        if (GUILayout.Button("Refresh Cards", GUILayout.Width(120f)))
        {
            RefreshCards();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCardGameAnalysis()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Card Game Match Analysis", EditorStyles.boldLabel);

        if (bestMatch != null)
        {
            EditorGUILayout.LabelField($"Closest Match: {bestMatch.gameName} ({bestMatch.similarityScore:P0})");
            EditorGUILayout.Space(5f);
            
            EditorGUILayout.LabelField("Similarities:", EditorStyles.miniLabel);
            foreach (var similarity in bestMatch.similarities)
            {
                EditorGUILayout.LabelField($"  • {similarity}", EditorStyles.wordWrappedMiniLabel);
            }
        }
        else if (allCards.Count > 0)
        {
            EditorGUILayout.HelpBox("Click 'Refresh Cards' to analyze card game mechanics.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSearchAndFilter()
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField("Search:", GUILayout.Width(60f));
        string newSearchText = EditorGUILayout.TextField(searchText);
        if (newSearchText != searchText)
        {
            searchText = newSearchText;
            ApplyFilters();
        }

        if (GUILayout.Button("Clear", GUILayout.Width(60f)))
        {
            searchText = "";
            ApplyFilters();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCardGridView()
    {
        EditorGUILayout.LabelField($"Cards ({filteredCards.Count} shown)", EditorStyles.boldLabel);
        
        cardScrollPosition = EditorGUILayout.BeginScrollView(cardScrollPosition);

        if (filteredCards.Count == 0)
        {
            EditorGUILayout.HelpBox("No cards found. Assign target objects and click 'Refresh Cards'.", MessageType.Info);
        }
        else
        {
            // Calculate grid layout
            int rowCount = Mathf.CeilToInt((float)filteredCards.Count / cardsPerRow);
            
            for (int row = 0; row < rowCount; row++)
            {
                EditorGUILayout.BeginHorizontal();
                
                for (int col = 0; col < cardsPerRow; col++)
                {
                    int index = row * cardsPerRow + col;
                    if (index < filteredCards.Count)
                    {
                        DrawCard(filteredCards[index]);
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCard(GoodSection card)
    {
        bool isSelected = selectedCard == card;
        bool wasSelected = isSelected;

        // Card background
        Rect cardRect = GUILayoutUtility.GetRect(cardWidth, cardHeight, GUILayout.Width(cardWidth), GUILayout.Height(cardHeight));
        
        // Draw card background
        Color originalColor = GUI.color;
        if (isSelected)
        {
            GUI.color = new Color(0.5f, 0.7f, 1f, 0.3f);
        }
        else
        {
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }
        GUI.Box(cardRect, "");
        GUI.color = originalColor;

        // Draw card border
        if (isSelected)
        {
            GUI.color = Color.cyan;
            GUI.Box(cardRect, "", GUI.skin.box);
            GUI.color = originalColor;
        }

        // Card content
        GUILayout.BeginArea(cardRect);
        EditorGUILayout.BeginVertical();

        // Card name
        string displayName = string.IsNullOrEmpty(card.sectionName) ? "Unnamed Card" : card.sectionName;
        if (displayName.Length > 20)
        {
            displayName = displayName.Substring(0, 17) + "...";
        }
        EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel, GUILayout.Height(20f));

        EditorGUILayout.Space(5f);

        // Description preview
        if (!string.IsNullOrEmpty(card.description))
        {
            string desc = card.description.Length > 60 ? card.description.Substring(0, 57) + "..." : card.description;
            EditorGUILayout.LabelField(desc, EditorStyles.wordWrappedMiniLabel, GUILayout.Height(40f));
        }

        EditorGUILayout.Space(5f);

        // Stats
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Actions: {card.impulseStack?.Count ?? 0}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        int connectionCount = card.connectedSectionNames?.Count ?? 0;
        EditorGUILayout.LabelField($"Connections: {connectionCount}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        // State indicators
        EditorGUILayout.BeginHorizontal();
        if (card.requiredState != null)
        {
            GUI.color = Color.green;
            GUILayout.Label("●", GUILayout.Width(10f));
            GUI.color = originalColor;
        }
        if (card.targetState != null)
        {
            GUI.color = Color.blue;
            GUILayout.Label("●", GUILayout.Width(10f));
            GUI.color = originalColor;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        GUILayout.EndArea();

        // Handle click
        if (Event.current.type == EventType.MouseDown && cardRect.Contains(Event.current.mousePosition))
        {
            selectedCard = card;
            Repaint();
            Event.current.Use();
        }
    }

    private void DrawCardDetailsPanel()
    {
        EditorGUILayout.LabelField("Selected Card Details", EditorStyles.boldLabel);
        
        detailsScrollPosition = EditorGUILayout.BeginScrollView(detailsScrollPosition, GUILayout.Height(300f));

        if (selectedCard == null)
        {
            EditorGUILayout.HelpBox("Select a card to view details.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Name:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selectedCard.sectionName ?? "Unnamed");

            EditorGUILayout.Space(5f);

            EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selectedCard.description ?? "No description", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5f);

            EditorGUILayout.LabelField("Impulse Actions:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{selectedCard.impulseStack?.Count ?? 0} actions");
            
            if (selectedCard.impulseStack != null && selectedCard.impulseStack.Count > 0)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < selectedCard.impulseStack.Count; i++)
                {
                    var action = selectedCard.impulseStack[i];
                    EditorGUILayout.LabelField($"{i + 1}. {action.muscleGroup} (Activation: {action.activation:F2})");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5f);

            EditorGUILayout.LabelField("Connections:", EditorStyles.boldLabel);
            int connectionCount = selectedCard.connectedSectionNames?.Count ?? 0;
            EditorGUILayout.LabelField($"{connectionCount} connected cards");
            
            if (selectedCard.connectedSectionNames != null && selectedCard.connectedSectionNames.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var connName in selectedCard.connectedSectionNames)
                {
                    EditorGUILayout.LabelField($"• {connName}");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5f);

            EditorGUILayout.LabelField("States:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Required: {(selectedCard.requiredState != null ? "Yes" : "No")}");
            EditorGUILayout.LabelField($"Target: {(selectedCard.targetState != null ? "Yes" : "No")}");

            EditorGUILayout.Space(10f);

            // JSON Export button
            if (GUILayout.Button("Copy Card JSON", GUILayout.Height(30f)))
            {
                CopyCardToJSON(selectedCard);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawNetworkGraph()
    {
        EditorGUILayout.LabelField("Card Network", EditorStyles.boldLabel);
        
        networkScrollPosition = EditorGUILayout.BeginScrollView(networkScrollPosition, GUILayout.Height(200f));

        if (selectedCard == null)
        {
            EditorGUILayout.HelpBox("Select a card to view its network connections.", MessageType.Info);
        }
        else
        {
            // Visual network representation
            EditorGUILayout.LabelField($"Card: {selectedCard.sectionName}", EditorStyles.boldLabel);
            
            if (selectedCard.connectedSectionNames != null && selectedCard.connectedSectionNames.Count > 0)
            {
                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField($"Connected to {selectedCard.connectedSectionNames.Count} card(s):", EditorStyles.miniLabel);
                EditorGUILayout.Space(5f);
                
                EditorGUI.indentLevel++;
                foreach (var connName in selectedCard.connectedSectionNames)
                {
                    // Try to find the connected card
                    var connectedCard = allCards.FirstOrDefault(c => c.sectionName == connName);
                    if (connectedCard != null)
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        
                        // Connection indicator
                        Color originalColor = GUI.color;
                        GUI.color = Color.green;
                        GUILayout.Label("●", GUILayout.Width(15f));
                        GUI.color = originalColor;
                        
                        EditorGUILayout.LabelField(connName, GUILayout.ExpandWidth(true));
                        
                        // Show connection count for this connected card
                        int connCount = connectedCard.connectedSectionNames?.Count ?? 0;
                        EditorGUILayout.LabelField($"({connCount} conn)", EditorStyles.miniLabel, GUILayout.Width(60f));
                        
                        if (GUILayout.Button("Select", GUILayout.Width(60f)))
                        {
                            selectedCard = connectedCard;
                            Repaint();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        Color originalColor = GUI.color;
                        GUI.color = Color.yellow;
                        GUILayout.Label("○", GUILayout.Width(15f));
                        GUI.color = originalColor;
                        EditorGUILayout.LabelField($"{connName} (not found)", EditorStyles.miniLabel);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUI.indentLevel--;
                
                EditorGUILayout.Space(5f);
                
                // Show reverse connections (cards that connect TO this card)
                var reverseConnections = allCards.Where(c => 
                    c.connectedSectionNames != null && 
                    c.connectedSectionNames.Contains(selectedCard.sectionName)).ToList();
                
                if (reverseConnections.Count > 0)
                {
                    EditorGUILayout.LabelField($"Connected FROM {reverseConnections.Count} card(s):", EditorStyles.miniLabel);
                    EditorGUI.indentLevel++;
                    foreach (var reverseCard in reverseConnections)
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        Color originalColor = GUI.color;
                        GUI.color = Color.cyan;
                        GUILayout.Label("←", GUILayout.Width(15f));
                        GUI.color = originalColor;
                        EditorGUILayout.LabelField(reverseCard.sectionName, GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("Select", GUILayout.Width(60f)))
                        {
                            selectedCard = reverseCard;
                            Repaint();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.LabelField("No outgoing connections", EditorStyles.miniLabel);
                
                // Check for reverse connections
                var reverseConnections = allCards.Where(c => 
                    c.connectedSectionNames != null && 
                    c.connectedSectionNames.Contains(selectedCard.sectionName)).ToList();
                
                if (reverseConnections.Count > 0)
                {
                    EditorGUILayout.Space(5f);
                    EditorGUILayout.LabelField($"Connected FROM {reverseConnections.Count} card(s):", EditorStyles.miniLabel);
                    EditorGUI.indentLevel++;
                    foreach (var reverseCard in reverseConnections)
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        Color originalColor = GUI.color;
                        GUI.color = Color.cyan;
                        GUILayout.Label("←", GUILayout.Width(15f));
                        GUI.color = originalColor;
                        EditorGUILayout.LabelField(reverseCard.sectionName, GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("Select", GUILayout.Width(60f)))
                        {
                            selectedCard = reverseCard;
                            Repaint();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshCards()
    {
        allCards.Clear();

        // Collect cards from PhysicsCardSolver
        if (targetCardSolver != null && targetCardSolver.availableCards != null)
        {
            foreach (var card in targetCardSolver.availableCards)
            {
                if (card != null && !allCards.Contains(card))
                {
                    allCards.Add(card);
                }
            }
        }

        // Collect cards from NervousSystem
        if (targetNervousSystem != null)
        {
            // Try to get cards directly from goodSections list (using reflection if needed)
            try
            {
                var nervousCards = targetNervousSystem.GetAvailableGoodSections(null);
                foreach (var card in nervousCards)
                {
                    if (card != null && !allCards.Contains(card))
                    {
                        allCards.Add(card);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PhysicsCardGameVisualizerWindow] Could not get cards from NervousSystem: {e.Message}");
            }
            
            // Also try to access goodSections directly via reflection
            var goodSectionsField = typeof(NervousSystem).GetField("goodSections", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (goodSectionsField != null)
            {
                var goodSections = goodSectionsField.GetValue(targetNervousSystem) as List<GoodSection>;
                if (goodSections != null)
                {
                    foreach (var card in goodSections)
                    {
                        if (card != null && !allCards.Contains(card))
                        {
                            allCards.Add(card);
                        }
                    }
                }
            }
        }

        // Collect cards from BehaviorTree
        if (targetBehaviorTree != null && targetBehaviorTree.availableCards != null)
        {
            foreach (var card in targetBehaviorTree.availableCards)
            {
                if (card != null && !allCards.Contains(card))
                {
                    allCards.Add(card);
                }
            }
        }

        // Rebuild connections from names
        foreach (var card in allCards)
        {
            if (card.connectedSectionNames != null && card.connectedSectionNames.Count > 0)
            {
                card.connectedSections.Clear();
                foreach (var name in card.connectedSectionNames)
                {
                    var connectedCard = allCards.FirstOrDefault(c => c.sectionName == name);
                    if (connectedCard != null && !card.connectedSections.Contains(connectedCard))
                    {
                        card.connectedSections.Add(connectedCard);
                    }
                }
            }
        }

        ApplyFilters();
        AnalyzeCardGameMechanics();
        
        Debug.Log($"[PhysicsCardGameVisualizerWindow] Refreshed {allCards.Count} cards");
    }

    private void ApplyFilters()
    {
        filteredCards.Clear();

        if (string.IsNullOrEmpty(searchText))
        {
            filteredCards.AddRange(allCards);
        }
        else
        {
            string searchLower = searchText.ToLower();
            foreach (var card in allCards)
            {
                bool matches = false;
                
                if (!string.IsNullOrEmpty(card.sectionName) && card.sectionName.ToLower().Contains(searchLower))
                    matches = true;
                
                if (!string.IsNullOrEmpty(card.description) && card.description.ToLower().Contains(searchLower))
                    matches = true;
                
                if (card.impulseStack != null)
                {
                    foreach (var action in card.impulseStack)
                    {
                        if (!string.IsNullOrEmpty(action.muscleGroup) && action.muscleGroup.ToLower().Contains(searchLower))
                        {
                            matches = true;
                            break;
                        }
                    }
                }
                
                if (matches)
                {
                    filteredCards.Add(card);
                }
            }
        }
    }

    private void AnalyzeCardGameMechanics()
    {
        if (allCards.Count == 0)
        {
            bestMatch = null;
            return;
        }

        float mtgScore = 0f;        // Magic: The Gathering
        float dominionScore = 0f;   // Dominion
        float gloomhavenScore = 0f; // Gloomhaven
        float netrunnerScore = 0f;  // Netrunner
        float slayTheSpireScore = 0f; // Slay the Spire

        int totalCards = allCards.Count;
        int stateBasedCards = 0;
        int actionStackCards = 0;
        int connectedCards = 0;
        int feasibilityCards = 0;

        foreach (var card in allCards)
        {
            // State-based mechanics (MTG, Gloomhaven)
            if (card.requiredState != null && card.targetState != null)
            {
                stateBasedCards++;
                mtgScore += 0.3f;
                gloomhavenScore += 0.4f; // Gloomhaven is very state-based
            }

            // Action stacks (Dominion, Slay the Spire)
            if (card.impulseStack != null && card.impulseStack.Count > 1)
            {
                actionStackCards++;
                dominionScore += 0.4f;
                slayTheSpireScore += 0.3f;
            }
            else if (card.impulseStack != null && card.impulseStack.Count == 1)
            {
                dominionScore += 0.2f;
            }

            // Network connections (Netrunner, KeyForge)
            if (card.connectedSectionNames != null && card.connectedSectionNames.Count > 0)
            {
                connectedCards++;
                netrunnerScore += 0.5f;
            }

            // Feasibility checks (Resource/energy systems - MTG, Gloomhaven)
            if (card.limits != null)
            {
                feasibilityCards++;
                mtgScore += 0.2f;
                gloomhavenScore += 0.2f;
            }

            // Behavior tree integration (Slay the Spire - deck building)
            if (card.behaviorTree != null)
            {
                slayTheSpireScore += 0.2f;
            }
        }

        // Normalize scores
        mtgScore /= totalCards;
        dominionScore /= totalCards;
        gloomhavenScore /= totalCards;
        netrunnerScore /= totalCards;
        slayTheSpireScore /= totalCards;

        // Clamp to 0-1 range
        mtgScore = Mathf.Clamp01(mtgScore);
        dominionScore = Mathf.Clamp01(dominionScore);
        gloomhavenScore = Mathf.Clamp01(gloomhavenScore);
        netrunnerScore = Mathf.Clamp01(netrunnerScore);
        slayTheSpireScore = Mathf.Clamp01(slayTheSpireScore);

        // Find best match
        List<CardGameMatch> matches = new List<CardGameMatch>
        {
            new CardGameMatch
            {
                gameName = "Magic: The Gathering",
                similarityScore = mtgScore,
                similarities = new List<string>
                {
                    $"State-based mechanics ({stateBasedCards} cards)",
                    $"Feasibility/Resource systems ({feasibilityCards} cards)",
                    "Card connections and interactions"
                }
            },
            new CardGameMatch
            {
                gameName = "Dominion",
                similarityScore = dominionScore,
                similarities = new List<string>
                {
                    $"Action stack mechanics ({actionStackCards} cards)",
                    "Sequential card execution",
                    "Card combinations"
                }
            },
            new CardGameMatch
            {
                gameName = "Gloomhaven",
                similarityScore = gloomhavenScore,
                similarities = new List<string>
                {
                    $"Strong state-based mechanics ({stateBasedCards} cards)",
                    $"Feasibility checks ({feasibilityCards} cards)",
                    "Physical state transitions"
                }
            },
            new CardGameMatch
            {
                gameName = "Netrunner / KeyForge",
                similarityScore = netrunnerScore,
                similarities = new List<string>
                {
                    $"Network connections ({connectedCards} cards)",
                    "Card relationship graphs",
                    "Interconnected card systems"
                }
            },
            new CardGameMatch
            {
                gameName = "Slay the Spire",
                similarityScore = slayTheSpireScore,
                similarities = new List<string>
                {
                    $"Action stacks ({actionStackCards} cards)",
                    "Behavior tree integration",
                    "Card sequencing"
                }
            }
        };

        bestMatch = matches.OrderByDescending(m => m.similarityScore).First();
    }

    private void CopyCardToJSON(GoodSection card)
    {
        if (card == null)
        {
            EditorUtility.DisplayDialog("Error", "No card selected.", "OK");
            return;
        }

        try
        {
            // Create a serializable copy for JSON export
            // Note: We need to handle non-serialized fields
            string json = EditorJsonUtility.ToJson(card, true);
            
            // Copy to clipboard
            EditorGUIUtility.systemCopyBuffer = json;
            
            EditorUtility.DisplayDialog("Success", $"Card '{card.sectionName}' JSON copied to clipboard!", "OK");
            Debug.Log($"[PhysicsCardGameVisualizerWindow] Copied card JSON to clipboard:\n{json}");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to export card JSON: {e.Message}", "OK");
            Debug.LogError($"[PhysicsCardGameVisualizerWindow] Failed to export card JSON: {e}");
        }
    }

    /// <summary>
    /// Represents a card game match with similarity score.
    /// </summary>
    private class CardGameMatch
    {
        public string gameName;
        public float similarityScore;
        public List<string> similarities = new List<string>();
    }
}
