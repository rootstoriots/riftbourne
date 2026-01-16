using UnityEngine;

namespace Riftbourne.Exploration.Behaviors
{
    /// <summary>
    /// Base class for NPC behavior strategies.
    /// Each behavior type (Idle, WaypointPatrol, etc.) implements this class.
    /// Uses Strategy pattern for flexible, swappable NPC behaviors.
    /// </summary>
    public abstract class NPCBehavior : MonoBehaviour
    {
        protected NPCController npcController;
        protected CharacterController characterController;
        protected Animator animator;
        
        /// <summary>
        /// Initialize the behavior with required references.
        /// Called by NPCController when behavior is assigned.
        /// </summary>
        public virtual void Initialize(NPCController controller, CharacterController charController, Animator anim)
        {
            npcController = controller;
            characterController = charController;
            animator = anim;
        }
        
        /// <summary>
        /// Called every frame to update behavior logic.
        /// Should update velocity.x and velocity.z based on behavior.
        /// </summary>
        /// <param name="velocity">Current velocity vector (modify x and z components)</param>
        /// <param name="currentSpeed">Current movement speed (modify as needed)</param>
        /// <param name="deltaTime">Time since last frame</param>
        public abstract void UpdateBehavior(ref Vector3 velocity, ref float currentSpeed, float deltaTime);
        
        /// <summary>
        /// Called when behavior is activated (switched to).
        /// Use this for initialization or state reset.
        /// </summary>
        public virtual void OnBehaviorActivated() { }
        
        /// <summary>
        /// Called when behavior is deactivated (switched away from).
        /// Use this for cleanup or state saving.
        /// </summary>
        public virtual void OnBehaviorDeactivated() { }
        
        /// <summary>
        /// Get the display name of this behavior (for UI/debugging).
        /// </summary>
        public abstract string GetBehaviorName();
    }
}
