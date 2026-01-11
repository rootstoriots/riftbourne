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

            // Collect all Unit states
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
                StartCoroutine(TransitionToExplorationCoroutine());
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
