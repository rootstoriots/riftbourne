using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Riftbourne.Combat;
using Riftbourne.Core;
using Riftbourne.Grid;
using System;

namespace Riftbourne.UI
{
    /// <summary>
    /// Displays battle win condition notification at battle start.
    /// Blocks battle progression until player acknowledges.
    /// </summary>
    public class BattleStakesNotificationUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text stakesText;
        [SerializeField] private Button acknowledgeButton;
        
        [Header("Text Templates")]
        [Tooltip("Template for KillAll condition: {0} = 'Defeat All Enemies'")]
        [SerializeField] private string killAllTemplate = "Defeat All Enemies";
        
        [Tooltip("Template for SurviveXRounds condition: {0} = round count")]
        [SerializeField] private string surviveRoundsTemplate = "Survive {0} Rounds";
        
        [Tooltip("Template for ProtectTarget condition: {0} = target name")]
        [SerializeField] private string protectTargetTemplate = "Defend {0}";
        
        [Tooltip("Template for ReachLocation condition: {0} = location description")]
        [SerializeField] private string reachLocationTemplate = "Reach {0}";
        
        [Tooltip("Template for Custom condition: {0} = custom text")]
        [SerializeField] private string customTemplate = "{0}";
        
        private EncounterData currentEncounter;
        private Action onAcknowledged;
        private GridManager gridManager;
        private RangeVisualizer rangeVisualizer;
        private CellHoverHandler cellHoverHandler;
        
        private void Awake()
        {
            // Hide panel initially
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }
            
            // Setup button
            if (acknowledgeButton != null)
            {
                acknowledgeButton.onClick.AddListener(OnAcknowledgeClicked);
            }
            
            // Get references to grid, range visualizer, and hover handler
            gridManager = GridManager.Instance;
            if (gridManager != null)
            {
                rangeVisualizer = gridManager.GetComponent<RangeVisualizer>();
                cellHoverHandler = gridManager.GetComponent<CellHoverHandler>();
                if (cellHoverHandler == null)
                {
                    // Try to find it elsewhere in the scene
                    cellHoverHandler = FindFirstObjectByType<CellHoverHandler>();
                }
            }
        }
        
        /// <summary>
        /// Show the stakes notification for the given encounter.
        /// </summary>
        public void ShowStakes(EncounterData encounter, Action onAcknowledgedCallback)
        {
            Debug.Log("BattleStakesNotificationUI: ShowStakes called");
            
            if (encounter == null)
            {
                Debug.LogWarning("BattleStakesNotificationUI: EncounterData is null!");
                onAcknowledgedCallback?.Invoke();
                return;
            }
            
            if (notificationPanel == null)
            {
                Debug.LogError("BattleStakesNotificationUI: notificationPanel is null! Cannot show stakes notification.");
                onAcknowledgedCallback?.Invoke();
                return;
            }
            
            currentEncounter = encounter;
            onAcknowledged = onAcknowledgedCallback;
            
            // Format stakes text based on victory condition
            string stakesMessage = FormatStakesMessage(encounter);
            
            Debug.Log($"BattleStakesNotificationUI: Showing stakes: {stakesMessage}");
            
            // Update UI
            if (titleText != null)
            {
                titleText.text = "Battle Objective";
            }
            else
            {
                Debug.LogWarning("BattleStakesNotificationUI: titleText is null!");
            }
            
            if (stakesText != null)
            {
                stakesText.text = stakesMessage;
            }
            else
            {
                Debug.LogWarning("BattleStakesNotificationUI: stakesText is null!");
            }
            
            // Disable grid visuals and range visualizer before showing UI
            DisableGridAndRangeVisualizer();
            
            // Show panel
            notificationPanel.SetActive(true);
            Debug.Log("BattleStakesNotificationUI: Panel activated");
            
            // Pause game time (optional - can be handled by TurnManager)
            Time.timeScale = 0f;
        }
        
        /// <summary>
        /// Disable grid visuals and range visualizer to prevent interaction during UI display.
        /// </summary>
        private void DisableGridAndRangeVisualizer()
        {
            // Block grid input
            if (gridManager != null)
            {
                gridManager.BlockInput(true);
                gridManager.SetGridVisualsVisible(false);
                Debug.Log("BattleStakesNotificationUI: Grid input blocked and visuals hidden");
            }
            
            // Disable range visualizer
            if (rangeVisualizer != null)
            {
                rangeVisualizer.SetEnabled(false);
                Debug.Log("BattleStakesNotificationUI: Range visualizer disabled");
            }
            
            // Disable hover handler
            if (cellHoverHandler != null)
            {
                cellHoverHandler.SetEnabled(false);
                Debug.Log("BattleStakesNotificationUI: Cell hover handler disabled");
            }
        }
        
        /// <summary>
        /// Re-enable grid visuals and range visualizer after UI closes.
        /// </summary>
        private void EnableGridAndRangeVisualizer()
        {
            // Unblock grid input
            if (gridManager != null)
            {
                gridManager.BlockInput(false);
                gridManager.SetGridVisualsVisible(true);
                Debug.Log("BattleStakesNotificationUI: Grid input unblocked and visuals shown");
            }
            
            // Enable range visualizer
            if (rangeVisualizer != null)
            {
                rangeVisualizer.SetEnabled(true);
                Debug.Log("BattleStakesNotificationUI: Range visualizer enabled");
            }
            
            // Enable hover handler
            if (cellHoverHandler != null)
            {
                cellHoverHandler.SetEnabled(true);
                Debug.Log("BattleStakesNotificationUI: Cell hover handler enabled");
            }
        }
        
        /// <summary>
        /// Format the stakes message based on victory condition.
        /// </summary>
        private string FormatStakesMessage(EncounterData encounter)
        {
            VictoryCondition condition = encounter.VictoryCondition;
            
            switch (condition)
            {
                case VictoryCondition.KillAll:
                    return killAllTemplate;
                    
                case VictoryCondition.SurviveXRounds:
                    int rounds = encounter.TurnLimit > 0 ? encounter.TurnLimit : 10; // Default fallback
                    return string.Format(surviveRoundsTemplate, rounds);
                    
                case VictoryCondition.ProtectTarget:
                    // TODO: Get target name from encounter when that field is added
                    return string.Format(protectTargetTemplate, "Target Character");
                    
                case VictoryCondition.ReachLocation:
                    // TODO: Get location description from encounter when that field is added
                    return string.Format(reachLocationTemplate, "Designated Location");
                    
                case VictoryCondition.Custom:
                    // TODO: Get custom text from encounter when that field is added
                    return string.Format(customTemplate, "Custom Objective");
                    
                default:
                    return killAllTemplate; // Default fallback
            }
        }
        
        /// <summary>
        /// Handle acknowledge button click.
        /// </summary>
        private void OnAcknowledgeClicked()
        {
            // Re-enable grid visuals and range visualizer before hiding UI
            EnableGridAndRangeVisualizer();
            
            // Hide panel
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }
            
            // Resume game time
            Time.timeScale = 1f;
            
            // Invoke callback
            onAcknowledged?.Invoke();
            onAcknowledged = null;
        }
        
        /// <summary>
        /// Check if notification is currently showing.
        /// </summary>
        public bool IsShowing()
        {
            return notificationPanel != null && notificationPanel.activeSelf;
        }
    }
}
