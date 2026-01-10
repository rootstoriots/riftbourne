using UnityEngine;
using UnityEngine.InputSystem;

namespace Riftbourne.Exploration
{
    /// <summary>
    /// Handles free 3D movement using CharacterController with WASD controls.
    /// Movement is relative to camera direction, supports sprint, gravity, and ground detection.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ExplorationController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.8f;
        [SerializeField] private float acceleration = 10f;
        
        [Header("Click-to-Move Settings")]
        [SerializeField] private bool enableClickToMove = true;
        [SerializeField] private float clickToMoveStopDistance = 0.5f;
        [SerializeField] private LayerMask clickableLayers = -1;
        
        [Header("Gravity Settings")]
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundCheckDistance = 0.1f;
        
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // Components
        private CharacterController characterController;
        private PlayerInputActions inputActions;
        
        // Movement state
        private Vector3 velocity;
        private Vector2 moveInput;
        private bool isSprinting;
        private bool wasMoving;
        private float currentSpeed;
        
        // Click-to-move state
        private Vector3? clickTargetPosition;
        private bool hasClickTarget = false;
        
        // Events for animation hooks
        public System.Action OnMovementStarted;
        public System.Action OnMovementStopped;
        public System.Action<bool> OnSprintChanged;
        
        // Public properties
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public bool IsSprinting => isSprinting;
        public float CurrentSpeed => currentSpeed;
        public Vector3 Velocity => velocity;
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputActions = new PlayerInputActions();
            
            // If no camera transform assigned, try to find main camera
            if (cameraTransform == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    cameraTransform = mainCam.transform;
                }
            }
        }
        
        private void OnEnable()
        {
            inputActions?.Gameplay.Enable();
        }
        
        private void OnDisable()
        {
            inputActions?.Gameplay.Disable();
        }
        
        private void Update()
        {
            ReadInput();
            HandleClickToMove();
            HandleMovement();
            HandleGravity();
            ApplyMovement();
            CheckMovementState();
        }
        
        private void ReadInput()
        {
            // Read movement input
            moveInput = inputActions.Gameplay.Move.ReadValue<Vector2>();
            
            // Read sprint input
            bool sprintPressed = inputActions.Gameplay.Sprint.IsPressed();
            if (sprintPressed != isSprinting)
            {
                isSprinting = sprintPressed;
                OnSprintChanged?.Invoke(isSprinting);
            }
            
            // Handle left click for click-to-move
            if (enableClickToMove && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleLeftClick();
            }
        }
        
        private void HandleLeftClick()
        {
            // Get camera for raycast
            Camera cam = null;
            if (cameraTransform != null)
            {
                cam = cameraTransform.GetComponent<Camera>();
            }
            
            if (cam == null)
            {
                cam = Camera.main;
            }
            
            if (cam == null)
            {
                Debug.LogWarning("ExplorationController: No camera found for click-to-move!");
                return;
            }
            
            // Perform raycast from camera through mouse position
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, clickableLayers))
            {
                // Store the hit point as target
                clickTargetPosition = hit.point;
                hasClickTarget = true;
                Debug.Log($"Click-to-move target set: {hit.point} (hit {hit.collider.name})");
            }
        }
        
        private void HandleClickToMove()
        {
            // If player is providing WASD input, cancel click-to-move
            if (moveInput.magnitude > 0.1f)
            {
                hasClickTarget = false;
                clickTargetPosition = null;
                return;
            }
            
            // If no click target, nothing to do
            if (!hasClickTarget || !clickTargetPosition.HasValue)
            {
                return;
            }
            
            Vector3 targetPos = clickTargetPosition.Value;
            Vector3 currentPos = transform.position;
            
            // Calculate direction to target (horizontal only)
            Vector3 direction = targetPos - currentPos;
            direction.y = 0f; // Keep movement on horizontal plane
            
            float distanceToTarget = direction.magnitude;
            
            // Check if we've reached the target
            if (distanceToTarget <= clickToMoveStopDistance)
            {
                // Reached target, stop
                hasClickTarget = false;
                clickTargetPosition = null;
                return;
            }
            
            // Normalize direction and apply movement
            direction.Normalize();
            
            // Calculate target speed (use normal move speed, no sprint for click-to-move)
            float targetSpeed = moveSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            
            // Apply movement direction
            velocity.x = direction.x * currentSpeed;
            velocity.z = direction.z * currentSpeed;
        }
        
        private void HandleMovement()
        {
            // If click-to-move is active, don't override with WASD movement
            // (click-to-move handles its own movement)
            if (hasClickTarget && moveInput.magnitude < 0.1f)
            {
                return;
            }
            
            if (cameraTransform == null)
            {
                Debug.LogWarning("ExplorationController: No camera transform assigned!");
                return;
            }
            
            // Calculate movement direction relative to camera
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            
            // Flatten camera vectors to horizontal plane
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            // Calculate desired movement direction
            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            
            // Calculate target speed
            float targetSpeed = moveSpeed;
            if (isSprinting && moveInput.magnitude > 0.1f)
            {
                targetSpeed *= sprintMultiplier;
            }
            
            // Apply acceleration
            currentSpeed = Mathf.Lerp(currentSpeed, moveInput.magnitude > 0.1f ? targetSpeed : 0f, acceleration * Time.deltaTime);
            
            // Apply movement to velocity (horizontal only, gravity handled separately)
            velocity.x = moveDirection.x * currentSpeed;
            velocity.z = moveDirection.z * currentSpeed;
        }
        
        private void HandleGravity()
        {
            // Apply gravity
            if (IsGrounded && velocity.y < 0)
            {
                // Reset vertical velocity when grounded
                velocity.y = -2f; // Small negative value to keep grounded
            }
            else
            {
                // Apply gravity when not grounded
                velocity.y += gravity * Time.deltaTime;
            }
        }
        
        private void ApplyMovement()
        {
            // Move the character
            Vector3 movement = velocity * Time.deltaTime;
            characterController.Move(movement);
        }
        
        private void CheckMovementState()
        {
            // Check if moving via WASD or click-to-move
            bool isMoving = moveInput.magnitude > 0.1f || (hasClickTarget && currentSpeed > 0.1f);
            
            if (isMoving && !wasMoving)
            {
                OnMovementStarted?.Invoke();
            }
            else if (!isMoving && wasMoving)
            {
                OnMovementStopped?.Invoke();
            }
            
            wasMoving = isMoving;
        }
        
        /// <summary>
        /// Sets the camera transform reference for movement direction calculation.
        /// </summary>
        public void SetCameraTransform(Transform camTransform)
        {
            cameraTransform = camTransform;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUIStyle style = new GUIStyle();
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            
            float yPos = 10f;
            GUI.Label(new Rect(10, yPos, 300, 20), $"Speed: {currentSpeed:F2} m/s", style);
            yPos += 25;
            GUI.Label(new Rect(10, yPos, 300, 20), $"Grounded: {IsGrounded}", style);
            yPos += 25;
            GUI.Label(new Rect(10, yPos, 300, 20), $"Sprinting: {isSprinting}", style);
            yPos += 25;
            GUI.Label(new Rect(10, yPos, 300, 20), $"Move Input: ({moveInput.x:F2}, {moveInput.y:F2})", style);
            yPos += 25;
            if (hasClickTarget && clickTargetPosition.HasValue)
            {
                float distance = Vector3.Distance(transform.position, clickTargetPosition.Value);
                GUI.Label(new Rect(10, yPos, 300, 20), $"Click Target: {distance:F2}m away", style);
            }
        }
    }
}
