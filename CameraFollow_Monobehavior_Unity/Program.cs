using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -5f);

    [Header("Camera Behavior")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float returnToPlayerSpeed = 5f;

    [Header("Mouse Control")]
    [SerializeField] private float mouseSensitivityX = 3f;
    [SerializeField] private float mouseSensitivityY = 2f;
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("Collision Detection")]
    [SerializeField] private bool enableCollisionDetection = true;
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private LayerMask collisionLayers;

    [Header("Zoom Settings")]
    [SerializeField] private float minZoomDistance = 2f;
    [SerializeField] private float maxZoomDistance = 10f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomSmoothSpeed = 10f;

    // Private variables
    private float playerYRotation = 0f;
    private float cameraVerticalAngle = 20f;
    private float cameraHorizontalOffset = 0f;
    private float currentZoomDistance;
    private float targetZoomDistance;
    private bool isFreeLooking = false;

    void Start()
    {
        // Initialize zoom
        currentZoomDistance = offset.magnitude;
        targetZoomDistance = currentZoomDistance;

        // Get initial player rotation
        if (target != null)
        {
            playerYRotation = target.eulerAngles.y;
        }

        // Keep cursor visible for left-click control
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("Camera target is not assigned!");
            return;
        }

        HandleFreeLook();
        HandleZoom();
        UpdateCameraPosition();
    }

    void HandleFreeLook()
    {
        // Check if left mouse button is held
        if (Input.GetMouseButtonDown(0))
        {
            isFreeLooking = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isFreeLooking = false;
        }

        if (isFreeLooking && Input.GetMouseButton(0))
        {
            // Get mouse input for free look
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;

            // Update camera angles independently
            cameraHorizontalOffset += mouseX;
            cameraVerticalAngle -= mouseY;

            // Clamp vertical angle
            cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle, minVerticalAngle, maxVerticalAngle);
        }
        else
        {
            // Smoothly return camera to follow player rotation
            playerYRotation = target.eulerAngles.y;
            cameraHorizontalOffset = Mathf.Lerp(cameraHorizontalOffset, 0f, returnToPlayerSpeed * Time.deltaTime);

            // Also smoothly return vertical angle to default when not free looking
            float defaultVerticalAngle = 20f;
            cameraVerticalAngle = Mathf.Lerp(cameraVerticalAngle, defaultVerticalAngle, returnToPlayerSpeed * Time.deltaTime);
        }
    }

    void HandleZoom()
    {
        // Handle scroll wheel zoom
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f)
        {
            targetZoomDistance -= scrollInput * zoomSpeed;
            targetZoomDistance = Mathf.Clamp(targetZoomDistance, minZoomDistance, maxZoomDistance);
        }

        // Smooth zoom transition
        currentZoomDistance = Mathf.Lerp(currentZoomDistance, targetZoomDistance, zoomSmoothSpeed * Time.deltaTime);
    }

    void UpdateCameraPosition()
    {
        // Calculate final rotation (player rotation + camera offset)
        float finalYRotation = playerYRotation + cameraHorizontalOffset;
        Quaternion rotation = Quaternion.Euler(cameraVerticalAngle, finalYRotation, 0f);

        // Calculate desired offset with current zoom distance
        Vector3 normalizedOffset = offset.normalized;
        Vector3 zoomedOffset = normalizedOffset * currentZoomDistance;
        Vector3 desiredPosition = target.position + rotation * zoomedOffset;

        // Handle collision detection
        if (enableCollisionDetection)
        {
            desiredPosition = HandleCollision(target.position, desiredPosition);
        }

        // Smooth position transition
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Always look at target
        Vector3 lookDirection = target.position - transform.position;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
        }
    }

    Vector3 HandleCollision(Vector3 targetPosition, Vector3 desiredPosition)
    {
        Vector3 direction = desiredPosition - targetPosition;
        float distance = direction.magnitude;

        // Perform spherecast from target to desired camera position
        RaycastHit hit;
        if (Physics.SphereCast(targetPosition, collisionRadius, direction.normalized, out hit, distance, collisionLayers))
        {
            // Position camera just before the collision point
            float safeDistance = Mathf.Max(hit.distance - collisionRadius, minZoomDistance);
            return targetPosition + direction.normalized * safeDistance;
        }

        return desiredPosition;
    }

    // Public method to set target at runtime
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            playerYRotation = target.eulerAngles.y;
        }
    }

    // Public method to reset camera
    public void ResetCamera()
    {
        cameraHorizontalOffset = 0f;
        cameraVerticalAngle = 20f;
        targetZoomDistance = offset.magnitude;
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        // Draw target position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(target.position, 0.5f);

        // Draw camera to target line
        Gizmos.color = isFreeLooking ? Color.red : Color.blue;
        Gizmos.DrawLine(target.position, transform.position);

        // Draw collision sphere
        if (enableCollisionDetection)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
        }

        // Draw zoom range
        Gizmos.color = Color.cyan;
        Vector3 minPos = target.position + (transform.position - target.position).normalized * minZoomDistance;
        Vector3 maxPos = target.position + (transform.position - target.position).normalized * maxZoomDistance;
        Gizmos.DrawWireSphere(target.position, minZoomDistance);
        Gizmos.DrawWireSphere(target.position, maxZoomDistance);
    }
}