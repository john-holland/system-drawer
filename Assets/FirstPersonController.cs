using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    ////////////////////
    /////////      /////
    // ///          ////
    /////            ///
    // ///          ////
    ////////      //////    
    ////////////////////
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 20f;
    
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;
    [SerializeField] private bool invertY = false;
    
    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundMask = 1; // Default layer
    
    private CharacterController characterController;
    private Camera playerCamera;
    private Vector3 velocity;
    private float verticalRotation = 0f;
    private float horizontalRotation = 0f;
    private bool isGrounded;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
        // If no camera found, try to get the main camera
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize rotations based on current transform and camera
        horizontalRotation = transform.eulerAngles.y;
        if (playerCamera != null)
        {
            verticalRotation = -playerCamera.transform.localEulerAngles.x;
            if (verticalRotation > 180f)
            {
                verticalRotation -= 360f;
            }
        }
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        CheckGrounded();
    }
    
    void HandleMouseLook()
    {
        // Ensure cursor is locked
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        // Get mouse input - try both GetAxis and GetAxisRaw
        float mouseXAxis = Input.GetAxis("Mouse X");
        float mouseYAxis = Input.GetAxis("Mouse Y");
        float mouseXRaw = Input.GetAxisRaw("Mouse X");
        float mouseYRaw = Input.GetAxisRaw("Mouse Y");
        
        float mouseX = mouseXAxis * mouseSensitivity;
        float mouseY = mouseYAxis * mouseSensitivity;
        
        // // Log mouse input values and cursor state
        // if (Mathf.Abs(mouseXAxis) > 0.001f || Mathf.Abs(mouseYAxis) > 0.001f || Mathf.Abs(mouseXRaw) > 0.001f || Mathf.Abs(mouseYRaw) > 0.001f)
        // {
        //     Debug.Log($"Cursor Lock: {Cursor.lockState}, Mouse X Axis: {mouseXAxis}, Mouse Y Axis: {mouseYAxis}, Mouse X Raw: {mouseXRaw}, Mouse Y Raw: {mouseYRaw}");
        // }
        
        // Rotate player horizontally (Y-axis rotation)
        horizontalRotation += mouseX;
        // Normalize to 0-360 range to prevent overflow
        if (horizontalRotation > 360f) horizontalRotation -= 360f;
        if (horizontalRotation < 0f) horizontalRotation += 360f;
        
        // Rotate camera vertically (X-axis rotation)
        verticalRotation += (invertY ? 1f : -1f) * mouseY;
        // maybe modulo and allow flip
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
        
        // Merge horizontal and vertical rotations before applying
        Quaternion horizontalRot = Quaternion.Euler(0f, horizontalRotation, 0f);
        Quaternion verticalRot = Quaternion.Euler(verticalRotation, 0f, 0f);
        
        // Apply horizontal rotation to transform (only Y-axis rotation)
        transform.rotation = horizontalRot;
        
        // Apply combined rotation to camera
        if (playerCamera != null)
        {
            // Combine transform rotation with vertical rotation
            playerCamera.transform.rotation = transform.rotation * verticalRot;
        }
    }
    
    void HandleMovement()
    {
        // Check if grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
        
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Determine if running (holding Left Shift)
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        
        // Calculate movement direction relative to player's forward direction
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        move = move.normalized * currentSpeed;
        
        // Apply movement
        characterController.Move(move * Time.deltaTime);
        
        // Handle jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = jumpForce;
        }
        
        // Apply gravity
        velocity.y -= gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
    
    void CheckGrounded()
    {
        // Use ground check transform if available, otherwise use character controller's bottom
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : 
            transform.position + Vector3.down * (characterController.height / 2f + 0.1f);
        
        isGrounded = Physics.CheckSphere(checkPosition, groundCheckDistance, groundMask);
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw ground check sphere in editor
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : 
            transform.position + Vector3.down * (GetComponent<CharacterController>()?.height / 2f + 0.1f ?? 0.1f);
        
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(checkPosition, groundCheckDistance);
    }
}
