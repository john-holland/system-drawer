using UnityEngine;

[CreateAssetMenu(fileName = "ScribePaperDoll", menuName = "Locomotion/Civil/Scribe Paper Doll")]
public sealed class ScribePaperDoll : ScriptableObject
{
    public const int AxisCount = 4;
    public static readonly string[] GradeAxes = { "Legibility", "Speed", "Accuracy", "Authority" };

    public string personaKey = "scribe";
    public int peckingOrder = 20;
    public string dialogTreeSetId;
    [Range(0f, 1f)] public float[] expected01 = { 0.6f, 0.55f, 0.7f, 0.4f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };

    public float[] Expected01() => CivilianPaperDoll.Pad4(expected01, 0.55f);
    public float[] FireLimit01() => CivilianPaperDoll.Pad4(fireLimit01, 0.9f);

    public static ScribePaperDoll CreateHeadScribe()
    {
        var d = CreateInstance<ScribePaperDoll>();
        d.name = "HeadScribe";
        d.personaKey = "head-scribe";
        d.peckingOrder = 4;
        return d;
    }
}
