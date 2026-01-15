using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Combat;
using Riftbourne.Core;
using System.Collections.Generic;

namespace Riftbourne.Core
{
    /// <summary>
    /// Handles battle end logic.
    /// Subscribes to GameEvents.OnCombatEnded and transitions back to exploration.
    /// Attach this to a GameObject in the battle scene.
    /// </summary>
    public class BattleEndHandler : MonoBehaviour
    {
        [Header("Exploration Scene")]
        [SerializeField] private string explorationSceneName = "ExplorationScene";

        [Header("Delay Settings")]
        [Tooltip("Delay before transitioning back to exploration (in seconds)")]
        [SerializeField] private float transitionDelay = 2f;

        private ExplorationSceneLoader explorationLoader;
        private bool battleEnded = false;

        private void Awake()
        {
            // Get or create ExplorationSceneLoader
            explorationLoader = FindFirstObjectByType<ExplorationSceneLoader>();
            if (explorationLoader == null)
            {
                GameObject loaderObj = new GameObject("ExplorationSceneLoader");
                explorationLoader = loaderObj.AddComponent<ExplorationSceneLoader>();
            }
        }

        private void OnEnable()
        {
            // Subscribe to combat end event
            GameEvents.OnCombatEnded += HandleCombatEnded;
        }

        private void OnDisable()
        {
            // Unsubscribe from combat end event
            GameEvents.OnCombatEnded -= HandleCombatEnded;
        }

        /// <summary>
        /// Handle combat ended event.
        /// </summary>
        private void HandleCombatEnded(bool playerVictory)
        {
            if (battleEnded) return; // Prevent multiple calls

            battleEnded = true;
            Debug.Log($"BattleEndHandler: Combat ended - Player Victory: {playerVictory}");

            if (!playerVictory)
            {
                Debug.Log("BattleEndHandler: Player defeated! Handling defeat...");
                // TODO: Handle player defeat (game over, retry, etc.)
                return;
            }

            // Show victory notification → spoils → loot → transition flow
            StartCoroutine(VictoryFlowCoroutine());
        }
        
        /// <summary>
        /// Coroutine handling the victory flow: notification → spoils → loot → transition.
        /// </summary>
        private System.Collections.IEnumerator VictoryFlowCoroutine()
        {
            Debug.Log("BattleEndHandler: Starting victory flow coroutine");
            
            // Step 1: Show victory notification
            // Use FindObjectsByType with IncludeInactive to find inactive GameObjects
            Riftbourne.UI.VictoryNotificationUI[] allVictoryUI = FindObjectsByType<Riftbourne.UI.VictoryNotificationUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Riftbourne.UI.VictoryNotificationUI victoryUI = allVictoryUI != null && allVictoryUI.Length > 0 ? allVictoryUI[0] : null;
            
            Debug.Log($"BattleEndHandler: Searching for VictoryNotificationUI... Found {allVictoryUI?.Length ?? 0} instance(s)");
            
            if (victoryUI != null)
            {
                Debug.Log($"BattleEndHandler: Found VictoryNotificationUI on GameObject '{victoryUI.gameObject.name}', showing notification");
                bool notificationAcknowledged = false;
                victoryUI.ShowVictory(() => { notificationAcknowledged = true; });
                
                // Wait for acknowledgment
                while (!notificationAcknowledged)
                {
                    yield return null;
                }
                Debug.Log("BattleEndHandler: Victory notification acknowledged");
            }
            else
            {
                Debug.LogWarning("BattleEndHandler: VictoryNotificationUI not found in scene! Skipping victory notification.");
                Debug.LogWarning("BattleEndHandler: Make sure VictoryNotificationUI component exists in the scene (can be on inactive GameObject).");
            }
            
            // Step 2: Show spoils screen
            Riftbourne.UI.BattleSpoilsUI[] allSpoilsUI = FindObjectsByType<Riftbourne.UI.BattleSpoilsUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Riftbourne.UI.BattleSpoilsUI spoilsUI = allSpoilsUI != null && allSpoilsUI.Length > 0 ? allSpoilsUI[0] : null;
            
            Debug.Log($"BattleEndHandler: Searching for BattleSpoilsUI... Found {allSpoilsUI?.Length ?? 0} instance(s)");
            
            if (spoilsUI != null && BattleStatisticsTracker.Instance != null)
            {
                Debug.Log($"BattleEndHandler: Found BattleSpoilsUI on GameObject '{spoilsUI.gameObject.name}', showing spoils");
                bool spoilsAcknowledged = false;
                spoilsUI.ShowSpoils(BattleStatisticsTracker.Instance.Statistics, () => { spoilsAcknowledged = true; });
                
                // Wait for acknowledgment
                while (!spoilsAcknowledged)
                {
                    yield return null;
                }
                Debug.Log("BattleEndHandler: Spoils screen acknowledged");
            }
            else
            {
                if (spoilsUI == null)
                {
                    Debug.LogWarning("BattleEndHandler: BattleSpoilsUI not found in scene! Skipping spoils screen.");
                    Debug.LogWarning("BattleEndHandler: Make sure BattleSpoilsUI component exists in the scene (can be on inactive GameObject).");
                }
                if (BattleStatisticsTracker.Instance == null)
                {
                    Debug.LogWarning("BattleEndHandler: BattleStatisticsTracker.Instance is null! Skipping spoils screen.");
                }
            }
            
            // Step 3: Show loot selection
            Riftbourne.UI.LootSelectionUI[] allLootUI = FindObjectsByType<Riftbourne.UI.LootSelectionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Riftbourne.UI.LootSelectionUI lootUI = allLootUI != null && allLootUI.Length > 0 ? allLootUI[0] : null;
            
            Debug.Log($"BattleEndHandler: Searching for LootSelectionUI... Found {allLootUI?.Length ?? 0} instance(s)");
            
            if (lootUI != null && LootManager.Instance != null)
            {
                Debug.Log($"BattleEndHandler: Found LootSelectionUI on GameObject '{lootUI.gameObject.name}', checking for loot");
                // Get loot from LootManager using public methods
                List<Riftbourne.Inventory.InventorySlot> loot = LootManager.Instance.GetBattleLoot();
                int currency = LootManager.Instance.GetBattleCurrency();
                
                Debug.Log($"BattleEndHandler: Loot count: {loot?.Count ?? 0}, Currency: {currency}");
                
                // Always show loot selection, even if empty (so player can see there's no loot)
                // But log if there's nothing to show
                if (loot == null || (loot.Count == 0 && currency == 0))
                {
                    Debug.Log("BattleEndHandler: No loot or currency to display, but showing loot selection anyway");
                }
                
                Debug.Log("BattleEndHandler: Calling ShowLoot on LootSelectionUI");
                bool lootComplete = false;
                lootUI.ShowLoot(loot ?? new List<Riftbourne.Inventory.InventorySlot>(), currency, () => 
                { 
                    Debug.Log("BattleEndHandler: LootSelectionUI onComplete callback invoked");
                    lootComplete = true; 
                });
                
                Debug.Log($"BattleEndHandler: Waiting for loot selection to complete. lootComplete = {lootComplete}");
                
                // Wait for loot selection
                int waitFrames = 0;
                while (!lootComplete)
                {
                    yield return null;
                    waitFrames++;
                    if (waitFrames % 60 == 0) // Log every 60 frames (~1 second)
                    {
                        Debug.Log($"BattleEndHandler: Still waiting for loot selection... (waited {waitFrames} frames)");
                    }
                }
                Debug.Log("BattleEndHandler: Loot selection complete");
            }
            else
            {
                if (lootUI == null)
                {
                    Debug.LogWarning("BattleEndHandler: LootSelectionUI not found in scene! Skipping loot selection.");
                    Debug.LogWarning("BattleEndHandler: Make sure LootSelectionUI component exists in the scene (can be on inactive GameObject).");
                }
                if (LootManager.Instance == null)
                {
                    Debug.LogWarning("BattleEndHandler: LootManager.Instance is null! Skipping loot selection.");
                }
            }
            
            // Step 4: Update party and transition
            List<Unit> battleUnits = new List<Unit>();
            if (PartyManager.Instance != null)
            {
                battleUnits = PartyManager.Instance.GetPartyMembersAsUnits();
            }
            else
            {
                // Fallback: find all player units in scene
                Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
                foreach (Unit unit in allUnits)
                {
                    if (unit != null && unit.Faction == Faction.Player && unit.IsAlive)
                    {
                        battleUnits.Add(unit);
                    }
                }
            }

            // Update party from battle and transition back to exploration
            if (explorationLoader != null)
            {
                if (battleUnits.Count > 0)
                {
                    // Update party from battle units
                    explorationLoader.UpdatePartyFromBattle(battleUnits);
                }

                // Trigger autosave after successful battle
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.AutoSave();
                }

                // Transition back to exploration after delay
                yield return StartCoroutine(TransitionToExplorationCoroutine());
            }
            else
            {
                Debug.LogError("BattleEndHandler: ExplorationSceneLoader not available!");
            }
        }

        /// <summary>
        /// Coroutine to transition to exploration scene after delay.
        /// </summary>
        private System.Collections.IEnumerator TransitionToExplorationCoroutine()
        {
            yield return new WaitForSeconds(transitionDelay);

            if (explorationLoader != null)
            {
                explorationLoader.LoadExplorationScene(explorationSceneName);
            }
        }
    }
}
