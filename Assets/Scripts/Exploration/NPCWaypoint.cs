using UnityEngine;

namespace Riftbourne.Exploration
{
    /// <summary>
    /// Component attached to waypoint GameObjects for NPC patrolling.
    /// Defines wait time at this waypoint and optional speed override for the segment leading TO it.
    /// </summary>
    public class NPCWaypoint : MonoBehaviour
    {
        [Header("Waypoint Settings")]
        [Tooltip("Time in seconds to wait at this waypoint before moving to the next one.")]
        [SerializeField] private float waitTime = 1f;
        
        [Tooltip("Optional speed override for the segment TO this waypoint (from previous waypoint). " +
                 "If <= 0, uses NPC's primary move speed. Speed override applies to the segment leading TO this waypoint.")]
        [SerializeField] private float speedOverride = 0f;
        
        [Header("Debug")]
        [Tooltip("Show gizmo in scene view for easy waypoint visualization.")]
        [SerializeField] private bool showGizmo = true;
        
        [Tooltip("Gizmo color for this waypoint.")]
        [SerializeField] private Color gizmoColor = Color.yellow;
        
        public float WaitTime => waitTime;
        public float SpeedOverride => speedOverride;
        public Vector3 Position => transform.position;
        
        /// <summary>
        /// Check if this waypoint has a speed override.
        /// </summary>
        public bool HasSpeedOverride => speedOverride > 0f;
        
        private void OnDrawGizmos()
        {
            if (!showGizmo) return;
            
            // Draw a sphere at the waypoint position
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // Draw an upward arrow to show direction
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
            Gizmos.DrawLine(transform.position + Vector3.up * 1f, transform.position + Vector3.up * 0.7f + Vector3.forward * 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.up * 1f, transform.position + Vector3.up * 0.7f - Vector3.forward * 0.3f);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw a larger sphere when selected
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.75f);
        }
    }
}
