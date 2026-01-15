using UnityEngine;
using UnityEngine.InputSystem;
using Riftbourne.Core;
using Riftbourne.Characters;
using Riftbourne.Skills;
using Riftbourne.Combat;

namespace Riftbourne.Exploration
{
    /// <summary>
    /// Handles free 3D movement using CharacterController with WASD controls.
    /// Movement is relative to camera direction, supports sprint, gravity, and ground detection.
    /// Uses POV character's narrative skills for exploration checks.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ExplorationController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.8f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float rotationSpeed = 10f; // Speed at which character rotates to face movement direction
        
        [Header("Click-to-Move Settings")]
        [SerializeField] private bool enableClickToMove = true;
        [SerializeField] private float clickToMoveStopDistance = 0.5f;
        [SerializeField] private LayerMask clickableLayers = -1;
        
        [Header("Gravity Settings")]
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundCheckDistance = 0.1f;
        
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private BattleSceneLoader battleSceneLoader; // Optional: assign manually, or will be found automatically
        
        [Header("Animation")]
        [Tooltip("Animator component for character animations. If not assigned, will search in children.")]
        [SerializeField] private Animator animator;
        
        [Header("Battle Trigger Settings")]
        [Tooltip("Default encounter to use when F2 is pressed (for testing). Leave empty to load from Resources/Encounters/DefaultEncounter")]
        [SerializeField] private EncounterData defaultEncounter;
        
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
        private float stableYPosition; // Store stable Y position to prevent root motion sinking
        
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

        /// <summary>
        /// Get the POV character's narrative skill level for a category.
        /// Used by exploration systems for skill checks.
        /// </summary>
        public int GetPOVNarrativeSkillLevel(NarrativeSkillCategory category)
        {
            if (PartyManager.Instance == null)
            {
                return 0;
            }

            CharacterState povCharacter = PartyManager.Instance.POVCharacter;
            if (povCharacter == null)
            {
                return 0;
            }

            return povCharacter.GetNarrativeSkillLevel(category);
        }

        /// <summary>
        /// Get the POV character.
        /// </summary>
        public CharacterState GetPOVCharacter()
        {
            if (PartyManager.Instance == null)
            {
                return null;
            }

            return PartyManager.Instance.POVCharacter;
        }

        /// <summary>
        /// Check if POV character's narrative skill meets a threshold.
        /// </summary>
        public bool CheckNarrativeSkill(NarrativeSkillCategory category, int requiredLevel)
        {
            int skillLevel = GetPOVNarrativeSkillLevel(category);
            return skillLevel >= requiredLevel;
        }
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputActions = new PlayerInputActions();
            
            // Find Animator if not assigned
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
            
            // CRITICAL: Disable root motion on ALL Animators in the hierarchy (including self)
            // This prevents animations from moving the character's transform
            // Note: This will make "Apply Root Motion" show as "Handled by script" in Inspector - this is correct
            Animator[] allAnimators = GetComponentsInChildren<Animator>(true); // Include inactive
            foreach (Animator anim in allAnimators)
            {
                anim.applyRootMotion = false;
                // Build path string for debugging
                string path = anim.gameObject.name;
                Transform parent = anim.transform.parent;
                while (parent != null)
                {
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
                Debug.Log($"[ROOT MOTION] ExplorationController: Disabled root motion on Animator '{anim.gameObject.name}' (Path: {path})");
            }
            
            // Also disable root motion on the main animator if found
            if (animator != null)
            {
                animator.applyRootMotion = false;
                Debug.Log($"[ROOT MOTION] ExplorationController: Main animator is on '{animator.gameObject.name}'");
            }
            else
            {
                Debug.LogWarning("[ROOT MOTION] ExplorationController: No Animator found! Character may not animate.");
            }
            
            // Initialize stable Y position
            stableYPosition = transform.position.y;
            
            // Restore position IMMEDIATELY in Awake (before player is visible)
            // This prevents the player from appearing at spawn location first
            RestorePositionIfReturningFromBattle();
            
            // If no camera transform assigned, try to find main camera
            if (cameraTransform == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    cameraTransform = mainCam.transform;
                }
            }
            
            // Find BattleSceneLoader if not assigned
            if (battleSceneLoader == null)
            {
                battleSceneLoader = FindFirstObjectByType<BattleSceneLoader>();
                if (battleSceneLoader == null)
                {
                    Debug.LogWarning("ExplorationController: No BattleSceneLoader found in scene. F2 battle trigger will not work. Add BattleSceneLoader component to a GameObject in the exploration scene.");
                }
            }
        }
        
        /// <summary>
        /// Restore position immediately in Awake if returning from battle.
        /// This prevents the player from appearing at spawn location first.
        /// </summary>
        private void RestorePositionIfReturningFromBattle()
        {
            if (SceneTransitionData.Instance == null)
            {
                return; // No saved data, first load
            }
            
            Vector3 savedPosition = SceneTransitionData.Instance.ExplorationPosition;
            if (savedPosition == Vector3.zero)
            {
                return; // No saved position, first load
            }
            
            // Set position immediately before player is visible
            transform.position = savedPosition;
            Debug.Log($"[POSITION RESTORE] ExplorationController: Restored position in Awake: {savedPosition} (X:{savedPosition.x:F2}, Y:{savedPosition.y:F2}, Z:{savedPosition.z:F2})");
            
            // Clear the saved position so it doesn't interfere with future loads
            SceneTransitionData.Instance.ExplorationPosition = Vector3.zero;
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
            
            // Handle F2 key for battle trigger
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                HandleBattleTrigger();
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
        
        /// <summary>
        /// Handle F2 key press to trigger battle.
        /// </summary>
        private void HandleBattleTrigger()
        {
            // Try to find BattleSceneLoader if not assigned
            if (battleSceneLoader == null)
            {
                battleSceneLoader = FindFirstObjectByType<BattleSceneLoader>();
            }
            
            if (battleSceneLoader == null)
            {
                Debug.LogWarning("ExplorationController: Cannot trigger battle - BattleSceneLoader not found! Add BattleSceneLoader component to a GameObject in the exploration scene.");
                return;
            }
            
            // Check if party exists
            if (PartyManager.Instance == null)
            {
                Debug.LogWarning("ExplorationController: Cannot trigger battle - PartyManager not available!");
                return;
            }
            
            var party = PartyManager.Instance.GetPartyMembers();
            if (party == null || party.Count == 0)
            {
                Debug.LogWarning("ExplorationController: Cannot trigger battle - No party members!");
                return;
            }
            
            Debug.Log($"ExplorationController: F2 pressed - Triggering battle with {party.Count} party members");
            
            // Get encounter data (use assigned default, or try to load from Resources)
            EncounterData encounter = defaultEncounter;
            if (encounter == null)
            {
                encounter = Resources.Load<EncounterData>("Encounters/DefaultEncounter");
                if (encounter == null)
                {
                    Debug.LogWarning("ExplorationController: No default encounter assigned and DefaultEncounter not found in Resources/Encounters/. Battle will load without encounter data.");
                }
            }
            
            battleSceneLoader.LoadBattleScene(null, encounter);
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
            
            // Apply movement direction (only if we have a valid target)
            if (hasClickTarget)
            {
                velocity.x = direction.x * currentSpeed;
                velocity.z = direction.z * currentSpeed;
            }
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
            float targetSpeedValue = moveInput.magnitude > 0.1f ? targetSpeed : 0f;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeedValue, acceleration * Time.deltaTime);
            
            // Only apply movement if there's input
            if (moveInput.magnitude > 0.1f)
            {
                // Apply movement to velocity (horizontal only, gravity handled separately)
                velocity.x = moveDirection.x * currentSpeed;
                velocity.z = moveDirection.z * currentSpeed;
            }
            else
            {
                // Decelerate when no input
                velocity.x = Mathf.Lerp(velocity.x, 0f, acceleration * Time.deltaTime);
                velocity.z = Mathf.Lerp(velocity.z, 0f, acceleration * Time.deltaTime);
            }
        }
        
        private void HandleGravity()
        {
            // Apply gravity
            if (IsGrounded && velocity.y < 0)
            {
                // Reset vertical velocity when grounded
                velocity.y = -2f; // Small negative value to keep grounded
                
                // Update stable Y position when grounded (for root motion correction)
                stableYPosition = transform.position.y;
            }
            else
            {
                // Apply gravity when not grounded
                velocity.y += gravity * Time.deltaTime;
            }
        }
        
        private void ApplyMovement()
        {
            // Calculate horizontal movement direction for rotation
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            
            // Rotate character to face movement direction (only when moving)
            if (horizontalVelocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // Move the character
            Vector3 movement = velocity * Time.deltaTime;
            characterController.Move(movement);
            
            // AGGRESSIVE FIX: If root motion is still being applied despite "Bake Into Pose",
            // force Y position to stay stable when grounded
            // This is a workaround for animations that have root motion that can't be baked out
            if (IsGrounded && animator != null)
            {
                Vector3 currentPos = transform.position;
                
                // If we're not intentionally moving vertically (not jumping/falling)
                // and Y position has drifted from stable position, correct it
                if (Mathf.Abs(velocity.y + 2f) < 0.5f) // We're grounded
                {
                    float yDrift = currentPos.y - stableYPosition;
                    
                    // If Y has drifted more than 0.02 units (root motion interference), correct it
                    if (Mathf.Abs(yDrift) > 0.02f)
                    {
                        currentPos.y = stableYPosition;
                        transform.position = currentPos;
                    }
                }
            }
        }
        
        /// <summary>
        /// Called by Unity when Animator has root motion enabled.
        /// We override this to prevent root motion from being applied automatically.
        /// CRITICAL: This method MUST exist and be empty to prevent root motion application.
        /// </summary>
        private void OnAnimatorMove()
        {
            // Completely ignore root motion - don't apply animator.deltaPosition or animator.deltaRotation
            // By overriding this method and not using animator.deltaPosition, we prevent root motion
            // However, some animations still apply root motion even with "Bake Into Pose" checked
            // In that case, we correct it in ApplyMovement() after CharacterController.Move()
            
            // If root motion is somehow still enabled, explicitly prevent it
            if (animator != null && animator.applyRootMotion)
            {
                // Don't apply the delta - this prevents root motion
                // By not calling transform.position += animator.deltaPosition, we block root motion
            }
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
            
            // Update animator parameters
            if (animator != null)
            {
                // Use horizontal velocity magnitude for Speed parameter (prevents root motion issues)
                Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);
                float speedMagnitude = horizontalVel.magnitude;
                animator.SetFloat("Speed", speedMagnitude);
                animator.SetBool("IsSprinting", isSprinting);
                animator.SetBool("IsGrounded", IsGrounded);
            }
        }
        
        /// <summary>
        /// Sets the camera transform reference for movement direction calculation.
        /// </summary>
        public void SetCameraTransform(Transform camTransform)
        {
            cameraTransform = camTransform;
        }
        
        /// <summary>
        /// Helper method to get full GameObject path for debugging.
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
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
