using UnityEngine;

[CreateAssetMenu(fileName = "SenatePersonPaperDoll", menuName = "Locomotion/Civil/Senate Person Paper Doll")]
public sealed class SenatePersonPaperDoll : ScriptableObject
{
    public string personaKey = "senator";
    [Range(0f, 1f)] public float[] expected01 = { 0.55f, 0.55f, 0.55f, 0.5f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };
    public CivilianPaperDoll civilian;
}

[CreateAssetMenu(fileName = "CongressPersonPaperDoll", menuName = "Locomotion/Civil/Congress Person Paper Doll")]
public sealed class CongressPersonPaperDoll : ScriptableObject
{
    public string personaKey = "representative";
    [Range(0f, 1f)] public float[] expected01 = { 0.55f, 0.55f, 0.55f, 0.5f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };
    public CivilianPaperDoll civilian;
}

[CreateAssetMenu(fileName = "ParliamentPersonPaperDoll", menuName = "Locomotion/Civil/Parliament Person Paper Doll")]
public sealed class ParliamentPersonPaperDoll : ScriptableObject
{
    public string personaKey = "mp";
    [Range(0f, 1f)] public float[] expected01 = { 0.55f, 0.55f, 0.55f, 0.5f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };
    public CivilianPaperDoll civilian;
}

[CreateAssetMenu(fileName = "MonarchPaperDoll", menuName = "Locomotion/Civil/Monarch Paper Doll")]
public class MonarchPaperDoll : ScriptableObject
{
    public string personaKey = "monarch";
    public bool ceremonial = true;
    [Range(0f, 1f)] public float[] expected01 = { 0.55f, 0.55f, 0.55f, 0.7f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };
    public CivilianPaperDoll civilian;
}

[CreateAssetMenu(fileName = "KingPaperDoll", menuName = "Locomotion/Civil/King Paper Doll")]
public sealed class KingPaperDoll : MonarchPaperDoll
{
}

[CreateAssetMenu(fileName = "QueenPaperDoll", menuName = "Locomotion/Civil/Queen Paper Doll")]
public sealed class QueenPaperDoll : MonarchPaperDoll
{
}

[CreateAssetMenu(fileName = "PrincePaperDoll", menuName = "Locomotion/Civil/Prince Paper Doll")]
public sealed class PrincePaperDoll : MonarchPaperDoll
{
}

[CreateAssetMenu(fileName = "PrincessPaperDoll", menuName = "Locomotion/Civil/Princess Paper Doll")]
public sealed class PrincessPaperDoll : MonarchPaperDoll
{
}
