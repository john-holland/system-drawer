#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class BranchEditorShortcuts
{
    public static bool IsBranchStep(TreeGrowthStepKind kind)
    {
        return kind == TreeGrowthStepKind.Sapling || kind == TreeGrowthStepKind.Canopy;
    }

    public static void Draw(TreeGrowthTravelAgent agent)
    {
        if (agent == null || agent.SelectedStep == null || !IsBranchStep(agent.SelectedStep.kind))
            return;
        agent.treeGen = (TreeGenConfig)EditorGUILayout.ObjectField(
            "Tree gen", agent.treeGen, typeof(TreeGenConfig), false);
        if (!GUILayout.Button("Branch editor"))
            return;
        if (agent.treeGen != null)
            BranchConfigWindow.Open(agent.treeGen);
        else
            agent.treeGen = BranchConfigWindow.CreateAndOpen();
        EditorUtility.SetDirty(agent);
    }
}
#endif
