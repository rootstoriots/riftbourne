using UnityEngine;
using UnityEngine.InputSystem;

namespace Riftbourne.Core
{
    /// <summary>
    /// Third-person follow camera for exploration mode.
    /// Follows the player smoothly, supports mouse rotation, and includes collision avoidance.
    /// </summary>
    public class ExplorationCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        
        [Header("Follow Settings")]
        [SerializeField] private float followDistance = 8f;
        [SerializeField] private float minFollowDistance = 3f;
        [SerializeField] private float maxFollowDistance = 15f;
        [SerializeField] private float heightOffset = 2f;
        [SerializeField] private float followSmoothing = 0.1f;
        
        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 2f;
        
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSensitivity = 0.5f;
        [SerializeField] private float minVerticalAngle = -30f;
        [SerializeField] private float maxVerticalAngle = 60f;
        [SerializeField] private float defaultVerticalAngle = 25f;
        [SerializeField] private bool requireRightMouse = true;
        
        [Header("Collision Settings")]
        [SerializeField] private bool useCollisionAvoidance = true;
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private LayerMask collisionLayers = -1;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // Input
        private PlayerInputActions inputActions;
        private Mouse mouse;
        
        // Camera rotation state
        private float horizontalAngle = 0f; // Yaw (rotation around Y axis)
        private float verticalAngle = 25f;   // Pitch (rotation around X axis)
        
        // Smoothing
        private Vector3 currentVelocity;
        
        // Public properties
        public float HorizontalAngle => horizontalAngle;
        public float VerticalAngle => verticalAngle;
        
        private void Awake()
        {
            inputActions = new PlayerInputActions();
            mouse = Mouse.current;
        }
        
        private void OnEnable()
        {
            inputActions?.Gameplay.Enable();
        }
        
        private void OnDisable()
        {
            inputActions?.Gameplay.Disable();
        }
        
        private void Start()
        {
            // Initialize camera rotation based on current transform
            if (target != null)
            {
                Vector3 directionToCamera = transform.position - (target.position + Vector3.up * heightOffset);
                horizontalAngle = Mathf.Atan2(directionToCamera.x, directionToCamera.z) * Mathf.Rad2Deg;
                float calculatedAngle = Mathf.Asin(directionToCamera.y / directionToCamera.magnitude) * Mathf.Rad2Deg;
                // Use default angle if calculated angle is invalid or use it if valid
                verticalAngle = float.IsNaN(calculatedAngle) ? defaultVerticalAngle : calculatedAngle;
            }
            else
            {
                verticalAngle = defaultVerticalAngle;
            }
            
            // Clamp initial follow distance
            followDistance = Mathf.Clamp(followDistance, minFollowDistance, maxFollowDistance);
        }
        
        private void LateUpdate()
        {
            if (target == null) return;
            
            HandleRotation();
            HandleZoom();
            UpdateCameraPosition();
        }
        
        private void HandleRotation()
        {
            // Check if right mouse button is required and held
            bool canRotate = !requireRightMouse || (mouse != null && mouse.rightButton.isPressed);
            
            if (!canRotate) return;
            
            // Read look input
            Vector2 lookInput = inputActions.Gameplay.Look.ReadValue<Vector2>();
            
            if (lookInput.magnitude > 0.1f)
            {
                // Scale down mouse input for smoother rotation (mouse delta is in pixels)
                float sensitivityMultiplier = 0.1f;
                
                // Update rotation angles
                horizontalAngle += lookInput.x * rotationSensitivity * sensitivityMultiplier;
                verticalAngle -= lookInput.y * rotationSensitivity * sensitivityMultiplier;
                
                // Clamp vertical angle
                verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
            }
        }
        
        private void HandleZoom()
        {
            if (inputActions == null) return;
            
            // Read zoom input (mouse scroll wheel)
            float zoomInput = inputActions.Gameplay.CameraZoom.ReadValue<float>();
            
            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                // Adjust follow distance based on scroll input
                followDistance -= zoomInput * zoomSpeed;
                followDistance = Mathf.Clamp(followDistance, minFollowDistance, maxFollowDistance);
            }
        }
        
        private void UpdateCameraPosition()
        {
            // Calculate target position behind and above player
            Vector3 targetPosition = CalculateTargetPosition();
            
            // Apply collision avoidance if enabled
            if (useCollisionAvoidance)
            {
                targetPosition = ApplyCollisionAvoidance(targetPosition);
            }
            
            // Smoothly move camera to target position
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, followSmoothing);
            
            // Make camera look at target
            Vector3 lookTarget = target.position + Vector3.up * heightOffset;
            transform.LookAt(lookTarget);
        }
        
        private Vector3 CalculateTargetPosition()
        {
            // Convert angles to radians
            float horizontalRad = horizontalAngle * Mathf.Deg2Rad;
            float verticalRad = verticalAngle * Mathf.Deg2Rad;
            
            // Calculate offset from target using spherical coordinates
            float horizontalDistance = followDistance * Mathf.Cos(verticalRad);
            float x = Mathf.Sin(horizontalRad) * horizontalDistance;
            float z = Mathf.Cos(horizontalRad) * horizontalDistance;
            float y = Mathf.Sin(verticalRad) * followDistance;
            
            Vector3 offset = new Vector3(x, y, z);
            Vector3 targetPos = target.position + Vector3.up * heightOffset + offset;
            
            return targetPos;
        }
        
        private Vector3 ApplyCollisionAvoidance(Vector3 desiredPosition)
        {
            Vector3 startPosition = target.position + Vector3.up * heightOffset;
            Vector3 direction = desiredPosition - startPosition;
            float distance = direction.magnitude;
            
            // Perform sphere cast to check for obstacles
            RaycastHit hit;
            if (Physics.SphereCast(startPosition, collisionRadius, direction.normalized, out hit, distance, collisionLayers))
            {
                // If we hit something, pull camera closer
                float safeDistance = hit.distance - collisionRadius;
                safeDistance = Mathf.Max(0.1f, safeDistance); // Ensure minimum distance
                return startPosition + direction.normalized * safeDistance;
            }
            
            return desiredPosition;
        }
        
        /// <summary>
        /// Sets the target transform to follow.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || target == null) return;
            
            GUIStyle style = new GUIStyle();
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            
            float yPos = 100f;
            GUI.Label(new Rect(10, yPos, 300, 20), $"Camera Horizontal: {horizontalAngle:F1}°", style);
            yPos += 25;
            GUI.Label(new Rect(10, yPos, 300, 20), $"Camera Vertical: {verticalAngle:F1}°", style);
            yPos += 25;
            GUI.Label(new Rect(10, yPos, 300, 20), $"Distance: {followDistance:F2}m ({minFollowDistance}-{maxFollowDistance})", style);
            yPos += 25;
            GUI.Label(new Rect(10, yPos, 300, 20), $"Actual Distance: {Vector3.Distance(transform.position, target.position + Vector3.up * heightOffset):F2}m", style);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (target == null) return;
            
            // Draw line from target to camera
            Gizmos.color = Color.yellow;
            Vector3 targetPos = target.position + Vector3.up * heightOffset;
            Gizmos.DrawLine(targetPos, transform.position);
            
            // Draw collision sphere
            if (useCollisionAvoidance)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, collisionRadius);
            }
        }
    }
}
