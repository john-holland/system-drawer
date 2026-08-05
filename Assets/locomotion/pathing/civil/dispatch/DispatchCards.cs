using UnityEngine;

[System.Serializable]
public class DispatchCard : GoodSection
{
    public DispatchRequest request;

    public DispatchCard()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "dispatch";
        traversabilityTag = "dispatch";
    }

    protected static void Fill(DispatchCard c, DispatchRequest request, string name)
    {
        c.request = request;
        c.sectionName = name;
        c.description = request != null ? request.kind : name;
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.limits = new SectionLimits { maxForce = 70f, maxTorque = 18f, maxVelocityChange = 1.4f };
    }
}

[System.Serializable]
public class DispatchRequestRouteCard : DispatchCard
{
    public static DispatchRequestRouteCard Generate(DispatchRequest request)
    {
        var c = new DispatchRequestRouteCard();
        Fill(c, request, "dispatch_route");
        return c;
    }
}

[System.Serializable]
public class DispatchRequestPickupCard : DispatchCard
{
    public static DispatchRequestPickupCard Generate(DispatchRequest request)
    {
        var c = new DispatchRequestPickupCard();
        Fill(c, request, "dispatch_pickup");
        return c;
    }
}

[System.Serializable]
public class DispatchRequestLoadCard : DispatchCard
{
    public static DispatchRequestLoadCard Generate(DispatchRequest request)
    {
        var c = new DispatchRequestLoadCard();
        Fill(c, request, "dispatch_load");
        return c;
    }
}

[System.Serializable]
public class DispatchRequestUnloadCard : DispatchCard
{
    public static DispatchRequestUnloadCard Generate(DispatchRequest request)
    {
        var c = new DispatchRequestUnloadCard();
        Fill(c, request, "dispatch_unload");
        return c;
    }
}

[System.Serializable]
public class DispatchRequestPassengerPickupCard : DispatchCard
{
    public static DispatchRequestPassengerPickupCard Generate(DispatchRequest request)
    {
        var c = new DispatchRequestPassengerPickupCard();
        Fill(c, request, "dispatch_passenger_pickup");
        return c;
    }
}

[System.Serializable]
public class DispatchRequestPassengerDropoffCard : DispatchCard
{
    public static DispatchRequestPassengerDropoffCard Generate(DispatchRequest request)
    {
        var c = new DispatchRequestPassengerDropoffCard();
        Fill(c, request, "dispatch_passenger_dropoff");
        return c;
    }
}

[System.Serializable]
public class DispatchRequestReleasePassengerCard : DispatchCard
{
    public static DispatchRequestReleasePassengerCard Generate(DispatchRequest request)
    {
        var c = new DispatchRequestReleasePassengerCard();
        Fill(c, request, "dispatch_release_passenger");
        return c;
    }
}

[System.Serializable]
public class DispatchConfirmCard : DispatchCard
{
    public static DispatchConfirmCard Generate(DispatchRequest request)
    {
        var c = new DispatchConfirmCard();
        Fill(c, request, "dispatch_confirm");
        return c;
    }
}
