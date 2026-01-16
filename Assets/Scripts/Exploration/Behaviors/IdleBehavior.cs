using UnityEngine;

namespace Riftbourne.Exploration.Behaviors
{
    /// <summary>
    /// Idle behavior - NPC stays in place and plays idle animation.
    /// </summary>
    public class IdleBehavior : NPCBehavior
    {
        [Header("Idle Settings")]
        [Tooltip("Acceleration rate for stopping movement when switching to idle.")]
        [SerializeField] private float deceleration = 10f;
        
        public override void UpdateBehavior(ref Vector3 velocity, ref float currentSpeed, float deltaTime)
        {
            // Gradually stop all movement
            velocity.x = Mathf.Lerp(velocity.x, 0f, deceleration * deltaTime);
            velocity.z = Mathf.Lerp(velocity.z, 0f, deceleration * deltaTime);
            currentSpeed = 0f;
        }
        
        public override string GetBehaviorName()
        {
            return "Idle";
        }
    }
}
