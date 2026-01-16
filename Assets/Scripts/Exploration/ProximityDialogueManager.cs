using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Exploration
{
    /// <summary>
    /// Internal class to represent a dialogue request in the queue.
    /// </summary>
    internal class DialogueRequest
    {
        public ProximityDialogueComponent npcComponent;
        public ProximityDialogueData dialogueData;
        public DialogueEntry selectedEntry;

        public DialogueRequest(ProximityDialogueComponent npc, ProximityDialogueData data, DialogueEntry entry)
        {
            npcComponent = npc;
            dialogueData = data;
            selectedEntry = entry;
        }
    }

    /// <summary>
    /// Singleton manager that coordinates proximity dialogue playback.
    /// Ensures only one dialogue plays at a time by using a queue system.
    /// Prevents overlapping voices when multiple NPCs are nearby.
    /// </summary>
    public class ProximityDialogueManager : MonoBehaviour
    {
        public static ProximityDialogueManager Instance { get; private set; }

        [Header("Queue Settings")]
        [Tooltip("Maximum number of dialogue requests that can be queued. Prevents infinite queue buildup.")]
        [Range(1, 50)]
        [SerializeField] private int maxQueueSize = 10;

        [Header("Dialogue UI Settings")]
        [Tooltip("Font to use for all dialogue text. If null, will use default Unity font.")]
        [SerializeField] private Font dialogueFont;

        [Tooltip("Text color for dialogue.")]
        [SerializeField] private Color dialogueTextColor = Color.white;

        [Tooltip("Background color for dialogue box (semi-transparent).")]
        [SerializeField] private Color dialogueBackgroundColor = new Color(0, 0, 0, 0.7f);

        [Tooltip("Font size for dialogue text. With larger canvas (50x25), use smaller font size like 12-16 for readable text.")]
        [Range(10, 100)]
        [SerializeField] private int dialogueFontSize = 14;

        [Tooltip("Vertical offset from bottom of screen for subtitle (in pixels).")]
        [Range(0, 200)]
        [SerializeField] private float dialogueBottomOffset = 50f;

        [Tooltip("Horizontal padding from screen edges for subtitle (in pixels).")]
        [Range(0, 500)]
        [SerializeField] private float dialogueHorizontalPadding = 100f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console.")]
        [SerializeField] private bool showDebugLogs = false;

        // Queue system
        private Queue<DialogueRequest> dialogueQueue = new Queue<DialogueRequest>();
        private bool isPlayingDialogue = false;
        private ProximityDialogueUI currentDialogueUI;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // Note: We don't use DontDestroyOnLoad here - dialogue manager is scene-specific
        }

        /// <summary>
        /// Request dialogue playback for an NPC.
        /// Adds to queue if another dialogue is currently playing.
        /// </summary>
        /// <param name="npc">The NPC component requesting dialogue.</param>
        /// <param name="data">The dialogue data asset.</param>
        /// <param name="entry">The selected dialogue entry to play.</param>
        public void RequestDialogue(ProximityDialogueComponent npc, ProximityDialogueData data, DialogueEntry entry)
        {
            if (npc == null || data == null || entry == null)
            {
                Debug.LogWarning("[ProximityDialogueManager] Invalid dialogue request - null parameters!");
                return;
            }

            // Check if already in queue (prevent duplicates)
            if (dialogueQueue.Any(req => req.npcComponent == npc))
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[ProximityDialogueManager] {npc.gameObject.name} already in queue, skipping duplicate request.");
                }
                return;
            }

            // Check queue size limit
            if (dialogueQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning($"[ProximityDialogueManager] Queue is full ({maxQueueSize} items). Dropping request from {npc.gameObject.name}.");
                return;
            }

            // Create request
            DialogueRequest request = new DialogueRequest(npc, data, entry);

            if (isPlayingDialogue)
            {
                // Add to queue
                dialogueQueue.Enqueue(request);
                if (showDebugLogs)
                {
                    Debug.Log($"[ProximityDialogueManager] Added {npc.gameObject.name} to queue. Queue size: {dialogueQueue.Count}");
                }
            }
            else
            {
                // Play immediately
                PlayDialogue(request);
            }
        }

        /// <summary>
        /// Cancel a dialogue request if it's still in the queue.
        /// Called when NPC moves out of range.
        /// </summary>
        /// <param name="npc">The NPC component to cancel.</param>
        public void CancelDialogue(ProximityDialogueComponent npc)
        {
            if (npc == null) return;

            // Remove from queue if present
            var queueList = dialogueQueue.ToList();
            var toRemove = queueList.FirstOrDefault(req => req.npcComponent == npc);
            if (toRemove != null)
            {
                queueList.Remove(toRemove);
                dialogueQueue.Clear();
                foreach (var req in queueList)
                {
                    dialogueQueue.Enqueue(req);
                }

                if (showDebugLogs)
                {
                    Debug.Log($"[ProximityDialogueManager] Cancelled dialogue request for {npc.gameObject.name}. Queue size: {dialogueQueue.Count}");
                }
            }
        }

        /// <summary>
        /// Play a dialogue request.
        /// </summary>
        private void PlayDialogue(DialogueRequest request)
        {
            if (request == null || request.npcComponent == null)
            {
                Debug.LogWarning("[ProximityDialogueManager] Cannot play dialogue - invalid request!");
                ProcessNextInQueue();
                return;
            }

            isPlayingDialogue = true;

            // Notify NPC that dialogue started (updates cooldown)
            request.npcComponent.OnDialogueStarted();

            // Get or create the dialogue UI component
            if (currentDialogueUI == null)
            {
                // Try to find existing ProximityDialogueUI in scene
                currentDialogueUI = FindFirstObjectByType<ProximityDialogueUI>();
                
                // If not found, create one
                if (currentDialogueUI == null)
                {
                    GameObject uiObj = new GameObject("ProximityDialogueUI");
                    currentDialogueUI = uiObj.AddComponent<ProximityDialogueUI>();
                    Debug.LogWarning("[ProximityDialogueManager] No ProximityDialogueUI found in scene! Created one, but you should set up the UI manually in the scene.");
                }
            }

            // Show dialogue using the manually-created UI
            currentDialogueUI.ShowDialogue(request.selectedEntry, request.dialogueData, OnDialogueFinished);

            if (showDebugLogs)
            {
                Debug.Log($"[ProximityDialogueManager] Playing dialogue for {request.npcComponent.gameObject.name}: \"{request.selectedEntry.dialogueText}\"");
            }
        }

        /// <summary>
        /// Called by ProximityDialogueUI when dialogue finishes.
        /// Processes the next item in the queue.
        /// </summary>
        private void OnDialogueFinished()
        {
            isPlayingDialogue = false;
            // Don't set currentDialogueUI to null - we reuse the same UI component

            // Process next in queue
            ProcessNextInQueue();
        }

        /// <summary>
        /// Process the next dialogue request in the queue.
        /// </summary>
        private void ProcessNextInQueue()
        {
            if (dialogueQueue.Count > 0)
            {
                DialogueRequest nextRequest = dialogueQueue.Dequeue();
                PlayDialogue(nextRequest);
            }
        }

        /// <summary>
        /// Get the current queue size (for debugging).
        /// </summary>
        public int GetQueueSize()
        {
            return dialogueQueue.Count;
        }

        /// <summary>
        /// Check if a dialogue is currently playing.
        /// </summary>
        public bool IsPlayingDialogue()
        {
            return isPlayingDialogue;
        }
    }
}
