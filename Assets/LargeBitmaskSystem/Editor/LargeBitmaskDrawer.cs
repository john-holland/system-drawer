using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the Large Bitmask package.
// It should be put in an Editor folder for use.
// Author : Simon Albou <ominous.lab@gmail.com>

[CustomPropertyDrawer(typeof(LargeBitmask))]
public class LargeBitmaskDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        property.NextVisible(true);
        int numberOfLines = 1 + property.arraySize;
        property.NextVisible(false);
        if (!property.boolValue) numberOfLines = 1;

        return numberOfLines * (EditorGUIUtility.singleLineHeight + 1);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // auto-adjustment
        property.NextVisible(true);
        if (property.arraySize == 0) property.arraySize++;

        // foldout prop
        SerializedProperty foldoutProp = property.Copy();
        foldoutProp.NextVisible(false);

        // fake indentation, so tickboxes aren't buggy
        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        float fakeIndent = oldIndent * 16;
        position = new Rect(position.x + fakeIndent, position.y, position.width-fakeIndent, position.height);

        // tweakable
        int numberOfButtons = 7;
        float buttonWidth = Mathf.Min(40f, position.width * 0.8f / (float)numberOfButtons);
        float headerSpace = 5f;
        float postHeaderIndent = 32f;
        float lineLabelWidth = 60f;
        float lineSpace = 5f;
        float toggleWidth = 16f;

        // header
        Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        Rect[] btnRects = new Rect[numberOfButtons];
        Rect labelRect = new Rect(headerRect.x, headerRect.y, headerRect.width - (headerSpace + buttonWidth*btnRects.Length), headerRect.height);
        float curX = labelRect.x + labelRect.width + headerSpace;
        for (int i = 0; i < btnRects.Length; i++)
        {
            btnRects[i] = new Rect(curX, labelRect.y, buttonWidth, labelRect.height);
            curX += buttonWidth;
        }
        foldoutProp.boolValue = EditorGUI.Foldout(labelRect, foldoutProp.boolValue, label, true);

        #region buttons

        int curRect = 0;

        // Data print button
        if (GUI.Button(btnRects[curRect++], new GUIContent("Data", "Debug.Log some data about this mask"), EditorStyles.miniButton))
        {
            int byteSize = property.arraySize;
            int bitSize = byteSize * 8;
            int trueBits = 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty prop = property.GetArrayElementAtIndex(i);
                int intVal = prop.intValue;
                for (int j = 0; j < 8; j++)
                    if ((intVal & (1<<j)) > 0)
                        trueBits++;
            }

            string str = string.Format(label.text + " : {0} bytes, {1} bits. {2} of these {1} bits are true.", byteSize, bitSize, trueBits);
            Debug.Log(str);
        }

        // Value buttons
        if (GUI.Button(btnRects[curRect++], new GUIContent("Zero", "Set all bits to False"), EditorStyles.miniButton))
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).intValue = 0;
        if (GUI.Button(btnRects[curRect++], new GUIContent("Full", "Set all bits to True"), EditorStyles.miniButton))
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).intValue = 255;
        if (GUI.Button(btnRects[curRect++], new GUIContent("Inv.", "Invert all bits"), EditorStyles.miniButton))
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty prop = property.GetArrayElementAtIndex(i);
                prop.intValue = 255-prop.intValue;
            }
        if (GUI.Button(btnRects[curRect++], new GUIContent("Rnd.", "Randomize all bits"), EditorStyles.miniButton))
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).intValue = Random.Range(0, 256);
        
        // resize buttons
        EditorGUI.BeginDisabledGroup(property.arraySize < 2);
        if (GUI.Button(btnRects[curRect++], new GUIContent("-8", "Remove 8 bits to this mask"), EditorStyles.miniButton)) property.arraySize--;
        EditorGUI.EndDisabledGroup();
        if (GUI.Button(btnRects[curRect++], new GUIContent("+8", "Add 8 bits to this mask"), EditorStyles.miniButton)) property.arraySize++;

        #endregion

        // foldout
        if (!foldoutProp.boolValue)
        {
            EditorGUI.indentLevel = oldIndent;
            return;
        }

        // lines
        float curY = position.y;
        for (int i = 0; i < property.arraySize; i++)
        {
            curY += EditorGUIUtility.singleLineHeight + 1;
            Rect lineRect = new Rect(position.x + postHeaderIndent, curY, position.width - postHeaderIndent, EditorGUIUtility.singleLineHeight);
            Rect subLabelRect = new Rect(lineRect.x, lineRect.y, lineLabelWidth, lineRect.height);
            EditorGUI.LabelField(subLabelRect, (i*8).ToString()+" - "+(i*8+7).ToString());
            curX = lineRect.x + lineLabelWidth + lineSpace;
            
            SerializedProperty byteProp = property.GetArrayElementAtIndex(i);
            //EditorGUI.LabelField(subLabelRect, byteProp.intValue.ToString()); // debug
            string transcription = "";
            for (int j = 0; j < 8; j++)
            {
                Rect toggleRect = new Rect(curX, curY, toggleWidth, lineRect.height);
                curX += toggleWidth + lineSpace;

                bool isOne = (byteProp.intValue & (1<<(7-j))) != 0;
                bool result = EditorGUI.Toggle(toggleRect, GUIContent.none, isOne);
                if (isOne != result)
                {
                    if (result) byteProp.intValue |= (1<<(7-j));
                    else byteProp.intValue &= (255-(1<<(7-j)));
                }

                if (result) transcription += (i*8+j).ToString() + " / ";
            }

            if (!string.IsNullOrEmpty(transcription))
            {
                transcription = transcription.Substring(0, transcription.Length-3);
                Rect leftoverRect = new Rect(curX, curY, position.xMax-curX, lineRect.height);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.LabelField(leftoverRect, transcription);
                EditorGUI.EndDisabledGroup();
            }
        }

        EditorGUI.indentLevel = oldIndent;
    }
}
