using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Riftbourne.UI
{
    /// <summary>
    /// Displays victory notification after battle ends.
    /// Requires player acknowledgment before proceeding.
    /// </summary>
    public class VictoryNotificationUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button acknowledgeButton;
        
        [Header("Messages")]
        [SerializeField] private string victoryTitle = "Victory!";
        [SerializeField] private string victoryMessage = "You have emerged victorious!";
        
        private Action onAcknowledged;
        
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
        }
        
        /// <summary>
        /// Show the victory notification.
        /// </summary>
        public void ShowVictory(Action onAcknowledgedCallback)
        {
            Debug.Log("VictoryNotificationUI: ShowVictory called");
            
            if (notificationPanel == null)
            {
                Debug.LogError("VictoryNotificationUI: notificationPanel is null! Cannot show victory notification.");
                onAcknowledgedCallback?.Invoke();
                return;
            }
            
            onAcknowledged = onAcknowledgedCallback;
            
            // Update UI
            if (titleText != null)
            {
                titleText.text = victoryTitle;
            }
            else
            {
                Debug.LogWarning("VictoryNotificationUI: titleText is null!");
            }
            
            if (messageText != null)
            {
                messageText.text = victoryMessage;
            }
            else
            {
                Debug.LogWarning("VictoryNotificationUI: messageText is null!");
            }
            
            // Show panel
            notificationPanel.SetActive(true);
            Debug.Log("VictoryNotificationUI: Panel activated");
            
            // Pause game time
            Time.timeScale = 0f;
        }
        
        /// <summary>
        /// Handle acknowledge button click.
        /// </summary>
        private void OnAcknowledgeClicked()
        {
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
