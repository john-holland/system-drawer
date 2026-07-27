using UnityEngine;

/// <summary>Keyboard / action input for adding and cycling waypoints.</summary>
[AddComponentMenu("Locomotion/Waypoints/Planner Input")]
public sealed class WaypointPlannerInput : MonoBehaviour
{
    public WaypointRoute route = new WaypointRoute();
    public FormationCatalog formationCatalog;
    public Camera rayCamera;
    public KeyCode addWaypointKey = KeyCode.F;
    public KeyCode removeLastKey = KeyCode.G;
    public KeyCode clearKey = KeyCode.H;
    public KeyCode nextFormationKey = KeyCode.RightBracket;
    public KeyCode prevFormationKey = KeyCode.LeftBracket;
    public LayerMask groundMask = ~0;
    public float maxRayDistance = 200f;
    public WaypointGuidanceService guidance;

    void Update()
    {
        if (guidance != null && route != null)
            guidance.route = route;
        if (Input.GetKeyDown(addWaypointKey))
            TryAddUnderCrosshair();
        if (Input.GetKeyDown(removeLastKey) && route != null && route.Count > 0)
            route.RemoveAt(route.Count - 1);
        if (Input.GetKeyDown(clearKey))
            route?.Clear();
        if (Input.GetKeyDown(nextFormationKey))
            CycleFormationNext();
        if (Input.GetKeyDown(prevFormationKey))
            CycleFormationPrev();
    }

    public bool TryAddUnderCrosshair()
    {
        var cam = rayCamera != null ? rayCamera : Camera.main;
        if (cam == null || route == null) return false;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            return false;
        string form = route.defaultFormationId;
        if (formationCatalog != null)
            form = formationCatalog.NormalizeId(form);
        var marker = route.Add(hit.point, null, form);
        var actor = hit.collider.GetComponentInParent<RagdollSystem>();
        if (actor != null)
        {
            marker.targetActorOrObject = actor.gameObject;
            marker.attackMark = true;
        }
        guidance?.OnRouteChanged();
        return true;
    }

    public void CycleFormationNext()
    {
        if (route == null || formationCatalog == null) return;
        route.CycleFormationNext(formationCatalog.Ids);
        guidance?.OnRouteChanged();
    }

    public void CycleFormationPrev()
    {
        if (route == null || formationCatalog == null) return;
        route.CycleFormationPrev(formationCatalog.Ids);
        guidance?.OnRouteChanged();
    }
}
