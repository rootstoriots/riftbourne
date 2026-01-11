using UnityEngine;
using System;

namespace Riftbourne.Exploration
{
    /// <summary>
    /// Component for proximity-based interaction detection.
    /// Uses trigger collider to detect when player enters/exits zone.
    /// NO button prompts - just detection for now.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractionZone : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Tag to identify player (leave empty to auto-detect)")]
        [SerializeField] private string playerTag = "Player";
        
        [Tooltip("Log detection to console")]
        [SerializeField] private bool logDetection = true;
        
        [Header("References")]
        [Tooltip("Player GameObject (auto-detected if not assigned)")]
        [SerializeField] private GameObject playerObject;
        
        private Collider zoneCollider;
        private bool playerInZone = false;
        
        // Events for future use
        public event Action OnPlayerEntered;
        public event Action OnPlayerExited;
        
        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            if (zoneCollider == null)
            {
                Debug.LogError($"InteractionZone on {gameObject.name}: No Collider component found! Adding BoxCollider.");
                zoneCollider = gameObject.AddComponent<BoxCollider>();
            }
            
            // Ensure collider is a trigger
            zoneCollider.isTrigger = true;
            
            // Auto-detect player if not assigned
            if (playerObject == null)
            {
                FindPlayer();
            }
        }
        
        private void FindPlayer()
        {
            // Try to find by tag first
            if (!string.IsNullOrEmpty(playerTag))
            {
                GameObject taggedPlayer = GameObject.FindGameObjectWithTag(playerTag);
                if (taggedPlayer != null)
                {
                    playerObject = taggedPlayer;
                    return;
                }
            }
            
            // Try to find ExplorationController (player movement component)
            ExplorationController controller = FindFirstObjectByType<ExplorationController>();
            if (controller != null)
            {
                playerObject = controller.gameObject;
                return;
            }
            
            // Last resort: find any GameObject with "Player" in name
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Player", System.StringComparison.OrdinalIgnoreCase))
                {
                    playerObject = obj;
                    return;
                }
            }
            
            if (playerObject == null && logDetection)
            {
                Debug.LogWarning($"InteractionZone on {gameObject.name}: Could not find player object. Proximity detection may not work.");
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check if the entering object is the player
            if (IsPlayer(other.gameObject))
            {
                playerInZone = true;
                
                if (logDetection)
                {
                    Debug.Log($"[InteractionZone] Player entered zone: {gameObject.name}");
                }
                
                OnPlayerEntered?.Invoke();
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            // Check if the exiting object is the player
            if (IsPlayer(other.gameObject))
            {
                playerInZone = false;
                
                if (logDetection)
                {
                    Debug.Log($"[InteractionZone] Player exited zone: {gameObject.name}");
                }
                
                OnPlayerExited?.Invoke();
            }
        }
        
        /// <summary>
        /// Check if a GameObject is the player.
        /// </summary>
        private bool IsPlayer(GameObject obj)
        {
            if (obj == null) return false;
            
            // Direct reference match
            if (obj == playerObject) return true;
            
            // Tag match
            if (!string.IsNullOrEmpty(playerTag) && obj.CompareTag(playerTag))
                return true;
            
            // Component match (ExplorationController)
            if (obj.GetComponent<ExplorationController>() != null)
                return true;
            
            // Name match (fallback)
            if (obj.name.Contains("Player", System.StringComparison.OrdinalIgnoreCase))
                return true;
            
            return false;
        }
        
        /// <summary>
        /// Check if player is currently in the zone.
        /// </summary>
        public bool IsPlayerInZone()
        {
            return playerInZone;
        }
        
        private void OnDrawGizmos()
        {
            // Draw wireframe of trigger zone in editor
            if (zoneCollider == null)
                zoneCollider = GetComponent<Collider>();
            
            if (zoneCollider != null && zoneCollider.isTrigger)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f); // Semi-transparent green
                
                if (zoneCollider is BoxCollider boxCollider)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(boxCollider.center, boxCollider.size);
                }
                else if (zoneCollider is SphereCollider sphereCollider)
                {
                    Gizmos.DrawSphere(transform.position + sphereCollider.center, sphereCollider.radius);
                }
            }
        }
    }
}
