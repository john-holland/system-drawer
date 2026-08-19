using UnityEngine;

/// <summary>Move-in after last construction frame — TravelAgent + open/close for doors/boxes.</summary>
[System.Serializable]
public class MoveInCard : TravelAgentCard
{
    public string boxOpenCloseJointId = "moving_box";
    public bool occupancyUnlocked;

    public MoveInCard()
    {
        sectionName = "move_in";
        isTravelAgentGoal = true;
        physicalPathingTag = "move_in";
        preferFlee = false;
    }

    public static MoveInCard Generate(Vector3 doorWorld)
    {
        return new MoveInCard { goalWorld = doorWorld };
    }
}
