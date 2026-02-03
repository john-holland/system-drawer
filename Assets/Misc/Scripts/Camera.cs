using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float distance = 10f;
    [SerializeField] private bool invertY = false;
    
    private Quaternion currentRotation;
    
    void Start()
    {
        // Initialize rotation based on current transform
        currentRotation = transform.rotation;
        
        // Update camera position based on initial rotation
        UpdateCameraPosition();
    }

    void Update()
    {
        // Get mouse delta
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        
        // Only rotate when mouse is being moved
        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
        {
            // Create quaternion rotations directly from mouse delta
            // Rotate around world Y axis for horizontal mouse movement
            Quaternion yRotation = Quaternion.AngleAxis(mouseX * rotationSpeed, Vector3.up);
            
            // Rotate around local X axis (right axis of current rotation) for vertical mouse movement
            Vector3 rightAxis = currentRotation * Vector3.right;
            Quaternion xRotation = Quaternion.AngleAxis((invertY ? 1f : -1f) * mouseY * rotationSpeed, rightAxis);
            
            // Apply rotations: first horizontal (world Y), then vertical (local X)
            // This accumulates the rotation
            currentRotation = yRotation * currentRotation * xRotation;
            
            // Apply rotation directly to transform rotation axis
            transform.rotation = currentRotation;
            
            // Update camera position to maintain distance on sphere
            UpdateCameraPosition();
        }
    }
    
    private void UpdateCameraPosition()
    {
        Vector3 targetPos = GetTargetPosition();
        
        // Calculate position on sphere at the current rotation
        // Camera looks backward along its forward axis, so we offset by -forward
        Vector3 offset = currentRotation * Vector3.back * distance;
        transform.position = targetPos + offset;
    }
    
    private Vector3 GetTargetPosition()
    {
        if (target != null)
        {
            return target.position;
        }
        return Vector3.zero;
    }
}
