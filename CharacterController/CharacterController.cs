// Simple reusable Monobehavior script to give wasd movement control to player
// use with cameraFollow script for 3rd person controller
// MW 10/25

using System;
using System.Drawing;
using System.Numerics;
using UnityEngine;
using static System.Runtime.CompilerServices.RuntimeHelpers;

[RequireComponent(typeof(CharacterController))]
public class AdvancedPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private int maxJumps = 2;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    // Private variables
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentMovement;
    private bool isGrounded;
    private int jumpsRemaining;
    private float cameraRotationX = 0f;
    private bool isCrouching = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        jumpsRemaining = maxJumps;

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // If no camera assigned, try to find main camera
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
        }
    }

    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleMouseLook();
    }

    void HandleGroundCheck()
    {
        // Cast a sphere slightly below the character to check for ground
        isGrounded = Physics.CheckSphere(
            transform.position + Vector3.down * (controller.height / 2),
            groundCheckDistance,
            groundMask
        );

        // Reset jumps when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
            jumpsRemaining = maxJumps;
        }
    }

    void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Determine target speed
        float targetSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift) && !isCrouching)
        {
            targetSpeed = sprintSpeed;
        }
        else if (isCrouching)
        {
            targetSpeed = crouchSpeed;
        }

        // Calculate movement direction relative to camera
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 targetMovement = (forward * vertical + right * horizontal).normalized * targetSpeed;

        // Smooth acceleration/deceleration
        float accelRate = targetMovement.magnitude > 0.01f ? acceleration : deceleration;
        currentMovement = Vector3.Lerp(currentMovement, targetMovement, accelRate * Time.deltaTime);

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Combine horizontal movement with vertical velocity
        Vector3 finalMovement = currentMovement + Vector3.up * velocity.y;

        // Move the character
        controller.Move(finalMovement * Time.deltaTime);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && jumpsRemaining > 0)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpsRemaining--;
        }
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
        }

        // Smooth crouch transition
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // Adjust center to keep feet on ground
        controller.center = Vector3.up * (controller.height / 2);
    }

    void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate player body horizontally
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera vertically with clamping
        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);
    }

    // Optional: Unlock cursor when ESC is pressed
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (controller == null) return;

        // Draw ground check sphere
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(
            transform.position + Vector3.down * (controller.height / 2),
            groundCheckDistance
        );
    }
}