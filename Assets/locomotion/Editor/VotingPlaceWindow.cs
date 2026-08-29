using UnityEditor;
using UnityEngine;

public sealed class VotingPlaceWindow : EditorWindow
{
    VotingPlaceBioRhythm _bio;
    VoteLedger _ledger;
    BallotSpec _ballot;
    Vector2 _scroll;

    [MenuItem("Locomotion/Voting Place")]
    public static void OpenPlace()
    {
        var w = GetWindow<VotingPlaceWindow>("Voting Place");
        w.minSize = new Vector2(420, 480);
    }

    [MenuItem("Locomotion/Ballot UI")]
    public static void OpenBallot() => OpenPlace();

    [MenuItem("Locomotion/Vote Runs")]
    public static void OpenRuns() => OpenPlace();

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _bio = (VotingPlaceBioRhythm)EditorGUILayout.ObjectField(
            "Voting Place", _bio, typeof(VotingPlaceBioRhythm), true);
        _ledger = (VoteLedger)EditorGUILayout.ObjectField(
            "Ledger", _ledger != null ? _ledger : (_bio != null ? _bio.ledger : null), typeof(VoteLedger), true);
        _ballot = (BallotSpec)EditorGUILayout.ObjectField("Ballot", _ballot, typeof(BallotSpec), false);
        if (_bio != null)
        {
            EditorGUILayout.LabelField("Issued", _bio.ballotsIssued.ToString());
            EditorGUILayout.LabelField("Cast", _bio.ballotsCast.ToString());
            EditorGUILayout.LabelField("Spoiled", _bio.ballotsSpoiled.ToString());
            if (_bio.perimeter != null)
            {
                EditorGUI.BeginChangeCheck();
                bool inpaint = EditorGUILayout.Toggle("Developer in-paint", _bio.perimeter.developerInpaint);
                if (EditorGUI.EndChangeCheck())
                    _bio.perimeter.developerInpaint = inpaint;
            }
            if (_bio.laneGrid != null)
                EditorGUILayout.LabelField("LaneGrid occupied", _bio.laneGrid.OccupiedCount.ToString());
            var hub = _bio.queueHub != null ? _bio.queueHub : _bio.GetComponent<VotingQueueHub>();
            if (hub != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("In-paint prompt", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                string prompt = EditorGUILayout.TextArea(hub.inpaintPrompt, GUILayout.MinHeight(48));
                if (EditorGUI.EndChangeCheck())
                    hub.inpaintPrompt = prompt;
                if (GUILayout.Button("Execute in-paint on local SG node"))
                {
                    if (string.IsNullOrEmpty(hub.inpaintPrompt))
                        hub.inpaintPrompt = VoteLemmaPropertyKeys.DefaultInpaintPrompt;
                    var sg = VotingPlaceSgNode.Ensure(_bio.gameObject);
                    hub.ExecuteInpaintPrompt();
                    if (sg != null)
                        sg.inpaintPrompt = hub.inpaintPrompt;
                }
                EditorGUILayout.HelpBox(VoteLemmaPropertyKeys.DefaultInpaintPrompt, MessageType.None);
            }
        }
        if (_ledger != null && _ledger.runs != null)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Vote runs", EditorStyles.boldLabel);
            for (int i = 0; i < _ledger.runs.Count; i++)
            {
                var r = _ledger.runs[i];
                if (r == null) continue;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(r.runId, r.gameSessionId);
                EditorGUILayout.LabelField("Ballot", r.ballotId);
                EditorGUILayout.LabelField("Certified", r.certified ? "yes" : "no");
                if (r.result != null)
                    EditorGUILayout.LabelField("Hash", r.result.tallyHash.ToString());
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
