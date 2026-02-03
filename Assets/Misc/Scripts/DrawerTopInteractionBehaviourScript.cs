using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawerTopInteractionBehaviourScript : MonoBehaviour
{
    [SerializeField] private Rigidbody drawerRigidbody;
    [SerializeField] private UnityEngine.Camera mainCamera;
    [SerializeField] private float impulseStrength = 50f;
    [SerializeField] private float debounceTime = 0.1f;
    
    private float lastImpulseTime = 0f;
    private Vector2 lastScreenPosition;
    private bool isTracking = false;
    
    void Start()
    {
        // If no camera is assigned, try to find the main camera
        if (mainCamera == null)
        {
            mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (cameraObj != null)
                {
                    mainCamera = cameraObj.GetComponent<UnityEngine.Camera>();
                }
            }
        }
        
        // If no rigidbody is assigned, try to get it from this GameObject
        if (drawerRigidbody == null)
        {
            drawerRigidbody = GetComponent<Rigidbody>();
        }
    }

    void Update()
    {
        // Handle both mouse and touch input
        Vector2 screenPosition = Vector2.zero;
        bool hasInput = false;
        
        if (Input.GetMouseButton(0))
        {
            screenPosition = Input.mousePosition;
            hasInput = true;
        }
        else if (Input.touchCount > 0)
        {
            screenPosition = Input.GetTouch(0).position;
            hasInput = true;
        }
        else
        {
            // Input released, reset tracking
            isTracking = false;
            return;
        }
        
        if (hasInput && drawerRigidbody != null && mainCamera != null)
        {
            if (!isTracking)
            {
                // Start tracking - initialize last position
                lastScreenPosition = screenPosition;
                isTracking = true;
                return;
            }
            
            // Calculate drag distance
            float dragDistance = Vector2.Distance(screenPosition, lastScreenPosition);
            
            // Only apply impulse if there's meaningful movement
            if (dragDistance > 0.1f)
            {
                // Check debounce
                float currentTime = Time.time;
                if (currentTime - lastImpulseTime >= debounceTime)
                {
                    ApplyImpulse(lastScreenPosition, screenPosition);
                    lastImpulseTime = currentTime;
                    lastScreenPosition = screenPosition;
                }
            }
        }
    }
    
    private void ApplyImpulse(Vector2 previousScreenPos, Vector2 currentScreenPos)
    {
        Plane drawerPlane = new Plane(transform.up, transform.position);
        
        // Convert previous screen position to world position
        Ray previousRay = mainCamera.ScreenPointToRay(previousScreenPos);
        float previousEnter = 0f;
        Vector3 previousWorldPos = Vector3.zero;
        bool previousHit = drawerPlane.Raycast(previousRay, out previousEnter);
        if (previousHit)
        {
            previousWorldPos = previousRay.GetPoint(previousEnter);
        }
        
        // Convert current screen position to world position
        Ray currentRay = mainCamera.ScreenPointToRay(currentScreenPos);
        float currentEnter = 0f;
        Vector3 currentWorldPos = Vector3.zero;
        bool currentHit = drawerPlane.Raycast(currentRay, out currentEnter);
        if (currentHit)
        {
            currentWorldPos = currentRay.GetPoint(currentEnter);
        }
        
        if (previousHit && currentHit)
        {
            // Calculate drag direction in world space
            Vector3 dragDirection = (currentWorldPos - previousWorldPos);
            
            // Project the drag direction onto the drawer plane (tangent to the plane)
            Vector3 dragDirectionOnPlane = Vector3.ProjectOnPlane(dragDirection, transform.up);
            
            // Normalize and apply impulse
            if (dragDirectionOnPlane.magnitude > 0.01f)
            {
                Vector3 impulseDirection = dragDirectionOnPlane.normalized;
                float dragMagnitude = dragDirectionOnPlane.magnitude;
                
                // Scale impulse by drag distance (longer drag = stronger impulse)
                drawerRigidbody.AddForce(impulseDirection * impulseStrength * dragMagnitude, ForceMode.Impulse);
            }
        }
    }
}
