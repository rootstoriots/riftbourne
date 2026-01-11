using UnityEngine;
using UnityEngine.SceneManagement;
using Riftbourne.Characters;
using Riftbourne.Exploration;
using System.Collections.Generic;

namespace Riftbourne.Core
{
    /// <summary>
    /// Handles transition from exploration to battle.
    /// Exports party data and loads battle scene.
    /// </summary>
    public class BattleSceneLoader : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string battleSceneName = "BattleScene";

        [Header("Unit Prefab")]
        [Tooltip("Prefab to use when creating Units from CharacterStates")]
        [SerializeField] private GameObject unitPrefab;

        /// <summary>
        /// Export party data from PartyManager and load battle scene.
        /// </summary>
        public void LoadBattleScene(string sceneName = null)
        {
            if (PartyManager.Instance == null)
            {
                Debug.LogError("BattleSceneLoader: PartyManager not available!");
                return;
            }

            List<CharacterState> party = PartyManager.Instance.GetPartyMembers();
            if (party == null || party.Count == 0)
            {
                Debug.LogWarning("BattleSceneLoader: No party members to load into battle!");
                return;
            }

            // Get or create SceneTransitionData
            SceneTransitionData transitionData = SceneTransitionData.Instance;
            if (transitionData == null)
            {
                GameObject transitionObj = new GameObject("SceneTransitionData");
                transitionData = transitionObj.AddComponent<SceneTransitionData>();
            }

            // Save player position before battle
            Vector3 playerPosition = Vector3.zero;
            ExplorationController playerController = FindFirstObjectByType<ExplorationController>();
            if (playerController != null)
            {
                playerPosition = playerController.transform.position;
                Debug.Log($"[POSITION SAVE] BattleSceneLoader: Saved player position: {playerPosition} (X:{playerPosition.x:F2}, Y:{playerPosition.y:F2}, Z:{playerPosition.z:F2})");
            }
            else
            {
                Debug.LogWarning("[POSITION SAVE] BattleSceneLoader: Could not find ExplorationController to save player position!");
            }

            // Store party data and position
            transitionData.PartyData = party;
            transitionData.ReturnSceneName = SceneManager.GetActiveScene().name;
            transitionData.BattleSceneName = sceneName ?? battleSceneName;
            transitionData.ExplorationPosition = playerPosition;

            Debug.Log($"BattleSceneLoader: Exporting {party.Count} party members to battle scene {transitionData.BattleSceneName}");

            // Load battle scene
            SceneManager.LoadScene(transitionData.BattleSceneName);
        }

        /// <summary>
        /// Export party data from PartyManager (for use by other systems).
        /// </summary>
        public List<CharacterState> ExportPartyData()
        {
            if (PartyManager.Instance == null)
            {
                Debug.LogError("BattleSceneLoader: PartyManager not available!");
                return new List<CharacterState>();
            }

            return PartyManager.Instance.GetPartyMembers();
        }

        /// <summary>
        /// Load battle scene with specific party data.
        /// </summary>
        public void LoadBattleScene(List<CharacterState> party, string sceneName = null)
        {
            if (party == null || party.Count == 0)
            {
                Debug.LogWarning("BattleSceneLoader: No party members provided!");
                return;
            }

            // Get or create SceneTransitionData
            SceneTransitionData transitionData = SceneTransitionData.Instance;
            if (transitionData == null)
            {
                GameObject transitionObj = new GameObject("SceneTransitionData");
                transitionData = transitionObj.AddComponent<SceneTransitionData>();
            }

            // Store party data
            transitionData.PartyData = party;
            transitionData.ReturnSceneName = SceneManager.GetActiveScene().name;
            transitionData.BattleSceneName = sceneName ?? battleSceneName;

            Debug.Log($"BattleSceneLoader: Loading battle scene {transitionData.BattleSceneName} with {party.Count} party members");

            // Load battle scene
            SceneManager.LoadScene(transitionData.BattleSceneName);
        }
    }
}
