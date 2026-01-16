using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Riftbourne.Exploration.Behaviors;

namespace Riftbourne.Exploration
{
    /// <summary>
    /// Controller for NPCs in exploration mode.
    /// Handles movement, behavior management, and animation updates.
    /// Uses behavior components (Strategy pattern) for extensible NPC behaviors.
    /// Based on ExplorationController but without player input.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class NPCController : MonoBehaviour
    {
        [Header("Behavior Settings")]
        [Tooltip("Behavior component that defines how this NPC acts. " +
                 "Add IdleBehavior, WaypointPatrolBehavior, or create custom behaviors inheriting from NPCBehavior.")]
        [SerializeField] private NPCBehavior currentBehavior;
        
        [Header("Movement Settings")]
        [Tooltip("Speed at which character rotates to face movement direction.")]
        [SerializeField] private float rotationSpeed = 10f;
        
        [Header("Gravity Settings")]
        [Tooltip("Gravity force applied to the NPC.")]
        [SerializeField] private float gravity = -9.81f;
        
        [Header("Animation")]
        [Tooltip("Animator component for character animations. If not assigned, will search in children.")]
        [SerializeField] private Animator animator;
        
        [Header("Debug")]
        [Tooltip("Show debug information in the scene view.")]
        [SerializeField] private bool showDebugInfo = false;
        
        // Components
        private CharacterController characterController;
        
        // Movement state
        private Vector3 velocity;
        private float currentSpeed;
        private float stableYPosition;
        
        // Public properties
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public float CurrentSpeed => currentSpeed;
        public Vector3 Velocity => velocity;
        public NPCBehavior CurrentBehavior => currentBehavior;
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            
            // Find Animator if not assigned
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
            
            // Disable root motion on ALL Animators in the hierarchy
            Animator[] allAnimators = GetComponentsInChildren<Animator>(true);
            foreach (Animator anim in allAnimators)
            {
                anim.applyRootMotion = false;
            }
            
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
            else
            {
                Debug.LogWarning($"[NPCController] {gameObject.name}: No Animator found! Character may not animate.");
            }
            
            // Initialize stable Y position
            stableYPosition = transform.position.y;
            
            // Initialize behavior if assigned
            if (currentBehavior != null)
            {
                currentBehavior.Initialize(this, characterController, animator);
                currentBehavior.OnBehaviorActivated();
            }
            else
            {
                Debug.LogWarning($"[NPCController] {gameObject.name}: No behavior component assigned! NPC will not move. Add an NPCBehavior component (e.g., IdleBehavior or WaypointPatrolBehavior).");
            }
        }
        
        private void Update()
        {
            HandleBehavior();
            HandleGravity();
            ApplyMovement();
            UpdateAnimation();
        }
        
        /// <summary>
        /// Handle NPC behavior using the assigned behavior component.
        /// </summary>
        private void HandleBehavior()
        {
            if (currentBehavior != null)
            {
                // Let the behavior update velocity and speed
                currentBehavior.UpdateBehavior(ref velocity, ref currentSpeed, Time.deltaTime);
            }
            else
            {
                // No behavior assigned, stop movement
                velocity.x = Mathf.Lerp(velocity.x, 0f, 10f * Time.deltaTime);
                velocity.z = Mathf.Lerp(velocity.z, 0f, 10f * Time.deltaTime);
                currentSpeed = 0f;
            }
        }
        
        /// <summary>
        /// Handle gravity application.
        /// </summary>
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
        
        /// <summary>
        /// Apply movement to the character controller.
        /// </summary>
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
            
            // Fix Y position drift (same as player controller)
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
        /// </summary>
        private void OnAnimatorMove()
        {
            // Completely ignore root motion - don't apply animator.deltaPosition or animator.deltaRotation
        }
        
        /// <summary>
        /// Update animator parameters based on current movement state.
        /// </summary>
        private void UpdateAnimation()
        {
            if (animator == null) return;
            
            // Use horizontal velocity magnitude for Speed parameter
            Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);
            float speedMagnitude = horizontalVel.magnitude;
            animator.SetFloat("Speed", speedMagnitude);
            animator.SetBool("IsGrounded", IsGrounded);
            // Note: NPCs don't sprint, so we don't set IsSprinting (it should default to false)
        }
        
        /// <summary>
        /// Set the behavior component at runtime.
        /// </summary>
        public void SetBehavior(NPCBehavior behavior)
        {
            // Deactivate old behavior
            if (currentBehavior != null)
            {
                currentBehavior.OnBehaviorDeactivated();
            }
            
            // Set new behavior
            currentBehavior = behavior;
            
            // Initialize and activate new behavior
            if (currentBehavior != null)
            {
                currentBehavior.Initialize(this, characterController, animator);
                currentBehavior.OnBehaviorActivated();
            }
        }
        
        /// <summary>
        /// Get the behavior component of a specific type.
        /// Useful for accessing behavior-specific methods.
        /// </summary>
        public T GetBehavior<T>() where T : NPCBehavior
        {
            return currentBehavior as T;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUIStyle style = new GUIStyle();
            style.fontSize = 12;
            style.normal.textColor = Color.white;
            
            // Get screen position of NPC
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            
            if (screenPos.z > 0) // Only show if in front of camera
            {
                float yPos = Screen.height - screenPos.y;
                GUI.Label(new Rect(screenPos.x - 100, yPos, 200, 20), $"NPC: {gameObject.name}", style);
                yPos += 20;
                string behaviorName = currentBehavior != null ? currentBehavior.GetBehaviorName() : "None";
                GUI.Label(new Rect(screenPos.x - 100, yPos, 200, 20), $"Behavior: {behaviorName}", style);
                yPos += 20;
                GUI.Label(new Rect(screenPos.x - 100, yPos, 200, 20), $"Speed: {currentSpeed:F2} m/s", style);
                
                // Show waypoint info if using WaypointPatrolBehavior
                WaypointPatrolBehavior patrolBehavior = GetBehavior<WaypointPatrolBehavior>();
                if (patrolBehavior != null)
                {
                    yPos += 20;
                    GUI.Label(new Rect(screenPos.x - 100, yPos, 200, 20), $"Waypoint: {patrolBehavior.GetCurrentWaypointIndex() + 1}/{patrolBehavior.GetWaypointCount()}", style);
                    yPos += 20;
                    GUI.Label(new Rect(screenPos.x - 100, yPos, 200, 20), $"Waiting: {patrolBehavior.IsWaitingAtWaypoint()}", style);
                }
            }
        }
    }
}
