using System;
using System.Collections.Generic;
using UnityEngine;

public enum InspectorTravelOption
{
    InspectorGoesToVehicle = 0,
    VehicleGoesToInspector = 1
}

public enum InspectableCraftKind
{
    Elevator = 0,
    Plane = 1,
    Train = 2,
    Bus = 3,
    Automobile = 4,
    Spaceship = 5,
    Helicopter = 6,
    Boat = 7
}

[Serializable]
public class InspectorCard : TravelAgentCard
{
    public DispatchRequest request;
    public InspectableCraftKind craftKind = InspectableCraftKind.Elevator;
    [CronExpr] public string hoursCron = "0 9 1 * *"; // month/year capable cron (day-of-month 1)
    public string openCloseTopologyId = "inspect_panel";
    public List<string> dialogSuggestions = new List<string>();

    protected static void Fill(InspectorCard c, DispatchRequest request, string name)
    {
        c.request = request;
        c.sectionName = name;
        c.description = request != null ? request.kind : name;
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
    }
}

[Serializable]
public class InspectorKnockCard : InspectorCard
{
    public static InspectorKnockCard Generate(DispatchRequest request, InspectableCraftKind kind = InspectableCraftKind.Elevator)
    {
        var c = new InspectorKnockCard();
        Fill(c, request, "inspector_knock");
        c.craftKind = kind;
        c.dialogSuggestions.Add("Inspection. Please open.");
        return c;
    }
}

[Serializable]
public class InspectorTravelOptionCard : InspectorCard
{
    public InspectorTravelOption travelOption = InspectorTravelOption.InspectorGoesToVehicle;

    public static InspectorTravelOptionCard Generate(DispatchRequest request, InspectableCraftKind kind)
    {
        var c = new InspectorTravelOptionCard();
        Fill(c, request, "inspector_travel_option");
        c.craftKind = kind;
        // Boats always inspector-goes; cars may drive to garage.
        c.travelOption = kind == InspectableCraftKind.Boat || kind == InspectableCraftKind.Elevator
            ? InspectorTravelOption.InspectorGoesToVehicle
            : InspectorTravelOption.VehicleGoesToInspector;
        if (kind == InspectableCraftKind.Automobile || kind == InspectableCraftKind.Bus)
            c.travelOption = InspectorTravelOption.VehicleGoesToInspector;
        return c;
    }
}

[Serializable]
public class JusticeDogCard : InspectorCard
{
    public float ownerCoefficient = 0.5f;
    public CombatCard combat;

    public static JusticeDogCard Generate(DispatchRequest request, RagdollSystem dog = null, UnityEngine.Object ownerOrActor = null)
    {
        var c = new JusticeDogCard();
        Fill(c, request, "justice_dog");
        c.justice = JusticeCard.Generate(JusticeAction.SecureArea, null);
        c.combat = new CombatCard { sectionName = "justice_dog" };
        if (dog != null && ownerOrActor != null)
            c.ownerCoefficient = dog.OpinionFor(ownerOrActor).OwnerCoefficient;
        return c;
    }
}

[Serializable]
public class InspectorPersonaSchedule
{
    public string personaKey = "inspector";
    public InspectableCraftKind craftKind = InspectableCraftKind.Elevator;
    [Tooltip("Cron with month/year fields supported by CronDue.")]
    [CronExpr] public string hoursCron = "0 9 1 * *";
    public bool enabled = true;
}
