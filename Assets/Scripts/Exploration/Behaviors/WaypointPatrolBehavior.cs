using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Riftbourne.Exploration;

namespace Riftbourne.Exploration.Behaviors
{
    /// <summary>
    /// Waypoint patrolling behavior - NPC moves between waypoints in a loop.
    /// </summary>
    public class WaypointPatrolBehavior : NPCBehavior
    {
        [Header("Waypoint Settings")]
        [Tooltip("Ordered list of waypoints to patrol.")]
        [SerializeField] private List<NPCWaypoint> waypoints = new List<NPCWaypoint>();
        
        [Tooltip("Primary movement speed for waypoint patrolling.")]
        [SerializeField] private float primaryMoveSpeed = 3f;
        
        [Tooltip("Distance threshold to consider a waypoint reached.")]
        [SerializeField] private float waypointReachDistance = 0.5f;
        
        [Tooltip("Whether to loop back to the first waypoint after reaching the last one.")]
        [SerializeField] private bool loopWaypoints = true;
        
        [Tooltip("Acceleration/deceleration rate.")]
        [SerializeField] private float acceleration = 10f;
        
        // Waypoint patrolling state
        private int currentWaypointIndex = 0;
        private bool isWaitingAtWaypoint = false;
        private Coroutine waitCoroutine;
        
        public override void Initialize(NPCController controller, CharacterController charController, Animator anim)
        {
            base.Initialize(controller, charController, anim);
            ValidateWaypoints();
        }
        
        public override void OnBehaviorActivated()
        {
            // Reset to first waypoint when activated
            currentWaypointIndex = 0;
            isWaitingAtWaypoint = false;
            
            if (waitCoroutine != null)
            {
                if (npcController != null)
                {
                    npcController.StopCoroutine(waitCoroutine);
                }
                waitCoroutine = null;
            }
        }
        
        public override void OnBehaviorDeactivated()
        {
            // Stop any wait coroutines when deactivated
            if (waitCoroutine != null)
            {
                if (npcController != null)
                {
                    npcController.StopCoroutine(waitCoroutine);
                }
                waitCoroutine = null;
            }
            isWaitingAtWaypoint = false;
        }
        
        public override void UpdateBehavior(ref Vector3 velocity, ref float currentSpeed, float deltaTime)
        {
            // If no waypoints, default to idle
            if (waypoints == null || waypoints.Count == 0)
            {
                velocity.x = Mathf.Lerp(velocity.x, 0f, acceleration * deltaTime);
                velocity.z = Mathf.Lerp(velocity.z, 0f, acceleration * deltaTime);
                currentSpeed = 0f;
                return;
            }
            
            // If waiting at waypoint, don't move
            if (isWaitingAtWaypoint)
            {
                velocity.x = Mathf.Lerp(velocity.x, 0f, acceleration * deltaTime);
                velocity.z = Mathf.Lerp(velocity.z, 0f, acceleration * deltaTime);
                currentSpeed = 0f;
                return;
            }
            
            // Get current waypoint
            NPCWaypoint currentWaypoint = waypoints[currentWaypointIndex];
            if (currentWaypoint == null)
            {
                Debug.LogWarning($"[WaypointPatrolBehavior] {npcController?.gameObject.name ?? "NPC"}: Waypoint at index {currentWaypointIndex} is null!");
                AdvanceToNextWaypoint();
                return;
            }
            
            Vector3 targetPosition = currentWaypoint.Position;
            Vector3 currentPosition = npcController.transform.position;
            
            // Calculate direction to target (horizontal only)
            Vector3 direction = targetPosition - currentPosition;
            direction.y = 0f; // Keep movement on horizontal plane
            
            float distanceToTarget = direction.magnitude;
            
            // Check if we've reached the waypoint
            if (distanceToTarget <= waypointReachDistance)
            {
                // Reached waypoint, start waiting
                StartWaitingAtWaypoint(currentWaypoint, ref velocity, ref currentSpeed);
                return;
            }
            
            // Normalize direction
            direction.Normalize();
            
            // Calculate speed for this segment
            float segmentSpeed = primaryMoveSpeed;
            if (currentWaypoint.HasSpeedOverride)
            {
                segmentSpeed = currentWaypoint.SpeedOverride;
            }
            
            // Apply acceleration
            currentSpeed = Mathf.Lerp(currentSpeed, segmentSpeed, acceleration * deltaTime);
            
            // Apply movement direction
            velocity.x = direction.x * currentSpeed;
            velocity.z = direction.z * currentSpeed;
        }
        
        /// <summary>
        /// Start waiting at the current waypoint.
        /// </summary>
        private void StartWaitingAtWaypoint(NPCWaypoint waypoint, ref Vector3 velocity, ref float currentSpeed)
        {
            if (isWaitingAtWaypoint) return;
            
            isWaitingAtWaypoint = true;
            
            // Stop movement
            velocity.x = 0f;
            velocity.z = 0f;
            currentSpeed = 0f;
            
            // Start wait coroutine
            if (waitCoroutine != null && npcController != null)
            {
                npcController.StopCoroutine(waitCoroutine);
            }
            
            if (npcController != null)
            {
                waitCoroutine = npcController.StartCoroutine(WaitAtWaypointCoroutine(waypoint.WaitTime));
            }
        }
        
        /// <summary>
        /// Coroutine to wait at a waypoint for the specified duration.
        /// </summary>
        private IEnumerator WaitAtWaypointCoroutine(float waitDuration)
        {
            yield return new WaitForSeconds(waitDuration);
            
            isWaitingAtWaypoint = false;
            AdvanceToNextWaypoint();
        }
        
        /// <summary>
        /// Advance to the next waypoint in the sequence.
        /// </summary>
        private void AdvanceToNextWaypoint()
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                return;
            }
            
            currentWaypointIndex++;
            
            // Check if we've reached the end
            if (currentWaypointIndex >= waypoints.Count)
            {
                if (loopWaypoints)
                {
                    // Loop back to first waypoint
                    currentWaypointIndex = 0;
                }
                else
                {
                    // Stop at last waypoint
                    currentWaypointIndex = waypoints.Count - 1;
                }
            }
        }
        
        /// <summary>
        /// Validate the waypoints list and remove null entries.
        /// </summary>
        private void ValidateWaypoints()
        {
            if (waypoints == null)
            {
                waypoints = new List<NPCWaypoint>();
                return;
            }
            
            // Remove null waypoints
            for (int i = waypoints.Count - 1; i >= 0; i--)
            {
                if (waypoints[i] == null)
                {
                    waypoints.RemoveAt(i);
                    Debug.LogWarning($"[WaypointPatrolBehavior] {npcController?.gameObject.name ?? "NPC"}: Removed null waypoint at index {i}");
                }
            }
        }
        
        /// <summary>
        /// Add a waypoint to the patrol list.
        /// </summary>
        public void AddWaypoint(NPCWaypoint waypoint)
        {
            if (waypoint == null) return;
            
            if (waypoints == null)
            {
                waypoints = new List<NPCWaypoint>();
            }
            
            waypoints.Add(waypoint);
        }
        
        /// <summary>
        /// Clear all waypoints.
        /// </summary>
        public void ClearWaypoints()
        {
            if (waypoints != null)
            {
                waypoints.Clear();
            }
            currentWaypointIndex = 0;
        }
        
        /// <summary>
        /// Get the current waypoint index.
        /// </summary>
        public int GetCurrentWaypointIndex() => currentWaypointIndex;
        
        /// <summary>
        /// Get the total number of waypoints.
        /// </summary>
        public int GetWaypointCount() => waypoints != null ? waypoints.Count : 0;
        
        /// <summary>
        /// Check if currently waiting at a waypoint.
        /// </summary>
        public bool IsWaitingAtWaypoint() => isWaitingAtWaypoint;
        
        public override string GetBehaviorName()
        {
            return "Waypoint Patrol";
        }
    }
}
