using UnityEngine;
using Riftbourne.Exploration;
using System.Collections;

namespace Riftbourne.Core
{
    /// <summary>
    /// Restores player position when returning from battle.
    /// Attach this to a GameObject in the exploration scene.
    /// </summary>
    public class ExplorationPositionRestorer : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Delay before attempting to restore position (to ensure other initializers run first)")]
        [SerializeField] private float restoreDelay = 1.0f;
        
        [Tooltip("Only restore position if returning from battle (not on first load)")]
        [SerializeField] private bool onlyRestoreFromBattle = true;
        
        [Tooltip("How many times to re-apply position (in case something overrides it)")]
        [SerializeField] private int reapplyAttempts = 3;
        
        [Tooltip("Delay between reapply attempts")]
        [SerializeField] private float reapplyDelay = 0.5f;
        
        private void Awake()
        {
            Debug.Log("[POSITION RESTORE] ExplorationPositionRestorer: Awake() called - attempting immediate position restore");
            // Try to restore position immediately in Awake (before Start methods run)
            TryRestorePositionImmediate();
        }
        
        private void Start()
        {
            Debug.Log("[POSITION RESTORE] ExplorationPositionRestorer: Start() called - starting delayed restore coroutine");
            // Also run coroutine as backup in case immediate restore didn't work
            StartCoroutine(RestorePlayerPositionCoroutine());
        }
        
        /// <summary>
        /// Try to restore position immediately in Awake (before player is visible).
        /// </summary>
        private void TryRestorePositionImmediate()
        {
            if (SceneTransitionData.Instance == null)
            {
                Debug.Log("[POSITION RESTORE] ExplorationPositionRestorer: No SceneTransitionData in Awake - will try in Start");
                return;
            }
            
            Vector3 savedPosition = SceneTransitionData.Instance.ExplorationPosition;
            if (savedPosition == Vector3.zero)
            {
                Debug.Log("[POSITION RESTORE] ExplorationPositionRestorer: Position is zero in Awake - likely first load");
                return;
            }
            
            // Try to find ExplorationController immediately
            ExplorationController playerController = FindFirstObjectByType<ExplorationController>();
            if (playerController != null)
            {
                Debug.Log($"[POSITION RESTORE] ExplorationPositionRestorer: Found player in Awake! Setting position immediately: {savedPosition}");
                playerController.transform.position = savedPosition;
                Debug.Log($"[POSITION RESTORE] ExplorationPositionRestorer: ✓ Position set in Awake - player should spawn at correct location");
            }
            else
            {
                Debug.Log("[POSITION RESTORE] ExplorationPositionRestorer: Player not found in Awake - will try in Start coroutine");
            }
        }
        
        /// <summary>
        /// Coroutine to restore player position after scene loads.
        /// This is a backup in case ExplorationController didn't restore in Awake.
        /// </summary>
        private IEnumerator RestorePlayerPositionCoroutine()
        {
            // Wait for scene to fully load and other initializers to run
            yield return new WaitForSeconds(restoreDelay);
            
            Debug.Log("[POSITION RESTORE] ExplorationPositionRestorer: Starting backup position restoration...");
            
            // Check if we have a saved position
            if (SceneTransitionData.Instance == null)
            {
                Debug.Log("[POSITION RESTORE] ExplorationPositionRestorer: No SceneTransitionData found. No position to restore.");
                yield break;
            }
            
            Vector3 savedPosition = SceneTransitionData.Instance.ExplorationPosition;
            
            // If position is already zero, ExplorationController likely already restored it
            if (savedPosition == Vector3.zero)
            {
                Debug.Log("[POSITION RESTORE] ExplorationPositionRestorer: Position already cleared - likely already restored by ExplorationController in Awake.");
                yield break;
            }
            
            Debug.Log($"[POSITION RESTORE] ExplorationPositionRestorer: Saved position from SceneTransitionData: {savedPosition} (X:{savedPosition.x:F2}, Y:{savedPosition.y:F2}, Z:{savedPosition.z:F2})");
            
            // Check if we should restore (only if returning from battle)
            if (onlyRestoreFromBattle && savedPosition == Vector3.zero)
            {
                Debug.Log("[POSITION RESTORE] ExplorationPositionRestorer: Position is zero - likely first load, not restoring.");
                yield break;
            }
            
            // Wait until ExplorationController exists
            ExplorationController playerController = null;
            int maxAttempts = 50;
            int attempts = 0;
            
            Debug.Log("[POSITION RESTORE] ExplorationPositionRestorer: Looking for ExplorationController...");
            while (playerController == null && attempts < maxAttempts)
            {
                playerController = FindFirstObjectByType<ExplorationController>();
                if (playerController == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    attempts++;
                }
            }
            
            if (playerController != null)
            {
                Vector3 currentPosition = playerController.transform.position;
                Debug.Log($"[POSITION RESTORE] ExplorationPositionRestorer: Found player at current position: {currentPosition} (X:{currentPosition.x:F2}, Y:{currentPosition.y:F2}, Z:{currentPosition.z:F2})");
                Debug.Log($"[POSITION RESTORE] ExplorationPositionRestorer: Restoring to saved position: {savedPosition} (X:{savedPosition.x:F2}, Y:{savedPosition.y:F2}, Z:{savedPosition.z:F2})");
                
                // Apply position multiple times to ensure it sticks (in case something overrides it)
                for (int attempt = 0; attempt < reapplyAttempts; attempt++)
                {
                    playerController.transform.position = savedPosition;
                    
                    // Wait and verify
                    yield return new WaitForSeconds(reapplyDelay);
                    Vector3 newPosition = playerController.transform.position;
                    float distance = Vector3.Distance(newPosition, savedPosition);
                    
                    Debug.Log($"[POSITION RESTORE] ExplorationPositionRestorer: Attempt {attempt + 1}/{reapplyAttempts} - Position: {newPosition} (X:{newPosition.x:F2}, Y:{newPosition.y:F2}, Z:{newPosition.z:F2}), Distance from target: {distance:F2}");
                    
                    if (distance < 0.1f)
                    {
                        Debug.Log($"[POSITION RESTORE] ExplorationPositionRestorer: ✓ Position successfully restored on attempt {attempt + 1}!");
                        break;
                    }
                    else if (attempt < reapplyAttempts - 1)
                    {
                        Debug.LogWarning($"[POSITION RESTORE] ExplorationPositionRestorer: ⚠ Position was overridden (distance: {distance:F2}), re-applying...");
                    }
                    else
                    {
                        Debug.LogWarning($"[POSITION RESTORE] ExplorationPositionRestorer: ⚠ Position may have been overridden after all attempts! Expected: {savedPosition}, Final: {newPosition}");
                    }
                }
                
                // Clear the saved position after restoration (so it doesn't interfere with future loads)
                if (SceneTransitionData.Instance != null)
                {
                    SceneTransitionData.Instance.ExplorationPosition = Vector3.zero;
                    Debug.Log("[POSITION RESTORE] ExplorationPositionRestorer: Cleared saved position from SceneTransitionData");
                }
            }
            else
            {
                Debug.LogWarning("[POSITION RESTORE] ExplorationPositionRestorer: Could not find ExplorationController to restore position!");
            }
        }
    }
}
