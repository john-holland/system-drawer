#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Locomotion.Musculature;

/// <summary>
/// Custom inspector for RagdollFinger with auto-fill digit list functionality.
/// </summary>
[CustomEditor(typeof(RagdollFinger))]
public class RagdollFingerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();

        RagdollFinger finger = (RagdollFinger)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Digit Management", EditorStyles.boldLabel);

        // Auto fill digit list button
        if (GUILayout.Button("Auto Fill Digit List", GUILayout.Height(25)))
        {
            AutoFillDigitList(finger);
        }

        EditorGUILayout.HelpBox(
            "Automatically finds all RagdollDigit components in child GameObjects and adds them to the digits list in order (proximal->distal).",
            MessageType.Info
        );
    }

    private void AutoFillDigitList(RagdollFinger finger)
    {
        if (finger == null)
            return;

        // Ensure digits list exists
        if (finger.digits == null)
            finger.digits = new System.Collections.Generic.List<RagdollDigit>();

        // Find direct child RagdollDigit components only
        System.Collections.Generic.List<RagdollDigit> directChildDigits = new System.Collections.Generic.List<RagdollDigit>();
        
        for (int i = 0; i < finger.transform.childCount; i++)
        {
            Transform child = finger.transform.GetChild(i);
            RagdollDigit digit = child.GetComponent<RagdollDigit>();
            if (digit != null)
            {
                directChildDigits.Add(digit);
            }
        }

        if (directChildDigits.Count == 0)
        {
            EditorUtility.DisplayDialog("Auto Fill Complete",
                "No RagdollDigit components found in direct child GameObjects.",
                "OK");
            return;
        }

        // Sort by sibling index to maintain order
        directChildDigits.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        int addedCount = 0;
        int existingCount = finger.digits.Count;

        // Check for partial completions and fill gaps
        foreach (var digit in directChildDigits)
        {
            if (digit == null)
                continue;

            // Check if this digit is already in the list
            if (!finger.digits.Contains(digit))
            {
                // Find the appropriate position based on sibling index
                int targetIndex = digit.transform.GetSiblingIndex();
                
                // Ensure list is large enough
                while (finger.digits.Count <= targetIndex)
                {
                    finger.digits.Add(null);
                }

                // Insert at the target position (or append if beyond current size)
                if (targetIndex < finger.digits.Count)
                {
                    finger.digits[targetIndex] = digit;
                }
                else
                {
                    finger.digits.Add(digit);
                }

                digit.indexInFinger = targetIndex;
                addedCount++;
            }
            else
            {
                // Update indexInFinger to match sibling index
                int targetIndex = digit.transform.GetSiblingIndex();
                digit.indexInFinger = targetIndex;
            }
        }

        // Remove null entries and re-index
        finger.digits.RemoveAll(d => d == null);
        
        // Re-index all digits based on their position in the list
        for (int i = 0; i < finger.digits.Count; i++)
        {
            if (finger.digits[i] != null)
            {
                finger.digits[i].indexInFinger = i;
            }
        }

        // Mark the last digit as caboose
        if (finger.digits.Count > 0)
        {
            for (int i = 0; i < finger.digits.Count; i++)
            {
                if (finger.digits[i] != null)
                {
                    finger.digits[i].isCabooseDigit = (i == finger.digits.Count - 1);
                }
            }
        }

        EditorUtility.SetDirty(finger);
        foreach (var digit in finger.digits)
        {
            if (digit != null)
                EditorUtility.SetDirty(digit);
        }

        string message = $"Found {directChildDigits.Count} direct child digit(s).\n";
        if (addedCount > 0)
            message += $"Added {addedCount} new digit(s) to the list.\n";
        if (existingCount > 0)
            message += $"Preserved {existingCount - (directChildDigits.Count - addedCount)} existing digit(s).\n";
        message += $"\nTotal digits in list: {finger.digits.Count}";

        EditorUtility.DisplayDialog("Auto Fill Complete", message, "OK");
    }
}
#endif
