using UnityEngine;

namespace Riftbourne.Exploration
{
    /// <summary>
    /// Component attached to NPCs that detects player proximity and triggers dialogue playback.
    /// Works alongside NPCController to add dialogue functionality to NPCs.
    /// </summary>
    public class ProximityDialogueComponent : MonoBehaviour
    {
        [Header("Dialogue Data")]
        [Tooltip("The dialogue data asset containing dialogue lines for this NPC.")]
        [SerializeField] private ProximityDialogueData dialogueData;

        [Header("Dialogue Mode")]
        [Tooltip("If true, plays dialogue entries sequentially instead of randomly. Useful for monologues or conversations.")]
        [SerializeField] private bool useSequentialDialogue = false;

        [Tooltip("If sequential, loop back to first entry after last one.")]
        [SerializeField] private bool loopSequentialDialogue = true;

        [Header("Proximity Settings")]
        [Tooltip("Distance at which dialogue triggers when player enters range.")]
        [Range(1f, 20f)]
        [SerializeField] private float proximityDistance = 5f;

        [Tooltip("Cooldown period between dialogue plays (in seconds). Prevents spam when player re-enters range.")]
        [Range(1f, 300f)]
        [SerializeField] private float cooldownPeriod = 30f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console.")]
        [SerializeField] private bool showDebugLogs = false;

        [Tooltip("Show proximity range gizmo in Scene view.")]
        [SerializeField] private bool showGizmo = true;

        // State tracking
        private bool playerInRange = false;
        private float lastDialogueTime = -999f; // Initialize to allow first dialogue
        private GameObject playerObject;
        private int sequentialDialogueIndex = 0; // Current index for sequential dialogue

        private void Awake()
        {
            // Auto-detect player if not found
            FindPlayer();
        }

        private void Start()
        {
            // Ensure manager exists
            if (ProximityDialogueManager.Instance == null)
            {
                Debug.LogWarning($"[ProximityDialogueComponent] {gameObject.name}: ProximityDialogueManager not found in scene! Creating one.");
                GameObject managerObj = new GameObject("ProximityDialogueManager");
                managerObj.AddComponent<ProximityDialogueManager>();
            }
        }

        private void Update()
        {
            if (dialogueData == null || playerObject == null)
                return;

            // Check distance to player
            float distanceToPlayer = Vector3.Distance(transform.position, playerObject.transform.position);
            bool currentlyInRange = distanceToPlayer <= proximityDistance;

            // Only trigger when entering range (not every frame while in range)
            if (currentlyInRange && !playerInRange)
            {
                OnPlayerEnteredRange();
            }
            else if (!currentlyInRange && playerInRange)
            {
                OnPlayerExitedRange();
            }

            playerInRange = currentlyInRange;
        }

        /// <summary>
        /// Called when player enters proximity range.
        /// </summary>
        private void OnPlayerEnteredRange()
        {
            // Check cooldown
            float timeSinceLastDialogue = Time.time - lastDialogueTime;
            if (timeSinceLastDialogue < cooldownPeriod)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[ProximityDialogueComponent] {gameObject.name}: Player entered range but cooldown active ({timeSinceLastDialogue:F1}s / {cooldownPeriod}s)");
                }
                return;
            }

            // Select dialogue entry (sequential or random)
            DialogueEntry selectedEntry = null;
            
            if (useSequentialDialogue)
            {
                // Sequential mode: play entries in order
                DialogueEntry[] allEntries = dialogueData.GetAllEntries();
                if (allEntries != null && allEntries.Length > 0)
                {
                    selectedEntry = allEntries[sequentialDialogueIndex];
                    
                    // Advance index for next time
                    sequentialDialogueIndex++;
                    if (sequentialDialogueIndex >= allEntries.Length)
                    {
                        if (loopSequentialDialogue)
                        {
                            sequentialDialogueIndex = 0; // Loop back to start
                        }
                        else
                        {
                            sequentialDialogueIndex = allEntries.Length - 1; // Stay on last entry
                        }
                    }
                }
            }
            else
            {
                // Random mode: use weighted random selection
                selectedEntry = dialogueData.SelectRandomDialogue();
            }

            if (selectedEntry == null)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[ProximityDialogueComponent] {gameObject.name}: No dialogue entry selected!");
                }
                return;
            }

            // Request dialogue playback via manager
            if (ProximityDialogueManager.Instance != null)
            {
                ProximityDialogueManager.Instance.RequestDialogue(this, dialogueData, selectedEntry);
                
                if (showDebugLogs)
                {
                    string mode = useSequentialDialogue ? "sequential" : "random";
                    Debug.Log($"[ProximityDialogueComponent] {gameObject.name}: Requested dialogue ({mode}): \"{selectedEntry.dialogueText}\"");
                }
            }
        }

        /// <summary>
        /// Called when player exits proximity range.
        /// </summary>
        private void OnPlayerExitedRange()
        {
            // Cancel any pending dialogue in queue if player left range
            if (ProximityDialogueManager.Instance != null)
            {
                ProximityDialogueManager.Instance.CancelDialogue(this);
            }
        }

        /// <summary>
        /// Called by ProximityDialogueManager when dialogue actually starts playing.
        /// Updates the cooldown timer.
        /// </summary>
        public void OnDialogueStarted()
        {
            lastDialogueTime = Time.time;
        }

        /// <summary>
        /// Find the player GameObject using various methods.
        /// </summary>
        private void FindPlayer()
        {
            // Try to find ExplorationController (player movement component)
            ExplorationController controller = FindFirstObjectByType<ExplorationController>();
            if (controller != null)
            {
                playerObject = controller.gameObject;
                return;
            }

            // Try to find by tag
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                playerObject = taggedPlayer;
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

            if (playerObject == null)
            {
                Debug.LogWarning($"[ProximityDialogueComponent] {gameObject.name}: Could not find player object. Proximity detection may not work.");
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            // Draw proximity range as a wireframe sphere
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan, semi-transparent
            Gizmos.DrawWireSphere(transform.position, proximityDistance);
        }

        /// <summary>
        /// Get the current dialogue data.
        /// </summary>
        public ProximityDialogueData GetDialogueData()
        {
            return dialogueData;
        }
    }
}
