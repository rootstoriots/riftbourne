using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Riftbourne.Items;

namespace Riftbourne.UI
{
    /// <summary>
    /// UI for examining items and revealing secrets.
    /// Displays item information and any discovered secrets.
    /// </summary>
    public class ItemExamineUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject examinePanel;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI secretsText;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (examinePanel == null)
                examinePanel = gameObject;

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HideExamine);
            }

            HideExamine();
        }

        /// <summary>
        /// Show examine UI for the specified item.
        /// </summary>
        public void ShowExamine(Item item)
        {
            if (item == null || examinePanel == null) return;

            // Set item information
            if (itemNameText != null)
            {
                itemNameText.text = item.ItemName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = item.Description;
            }

            // Check for secrets
            if (secretsText != null)
            {
                string secrets = GetItemSecrets(item);
                secretsText.text = secrets;
                secretsText.gameObject.SetActive(!string.IsNullOrEmpty(secrets));
            }

            examinePanel.SetActive(true);
        }

        /// <summary>
        /// Get secrets for the item (if any).
        /// </summary>
        private string GetItemSecrets(Item item)
        {
            // Check if item has examine data
            // Items can reference ItemExamineData ScriptableObject
            // For now, this is a basic implementation - can be expanded later
            // when items have a reference field to ItemExamineData
            
            // For now, return empty (no secrets)
            // Future: Check item.examineData field if added to Item class
            return "";
        }

        /// <summary>
        /// Hide the examine UI.
        /// </summary>
        public void HideExamine()
        {
            if (examinePanel != null)
            {
                examinePanel.SetActive(false);
            }
        }
    }
}
