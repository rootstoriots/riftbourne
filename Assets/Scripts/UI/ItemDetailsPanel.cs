using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Riftbourne.Items;

namespace Riftbourne.UI
{
    /// <summary>
    /// Static panel that displays item details when an item is selected.
    /// Shows name, rarity, stats, description, icon, and other relevant information.
    /// </summary>
    public class ItemDetailsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelObject;
        [SerializeField] private Image itemIconImage;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private TextMeshProUGUI itemTypeText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI weightText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image aurumShardsIcon;

        private void Awake()
        {
            if (panelObject == null)
                panelObject = gameObject;

            // Panel should always be visible (static)
            // Show empty state initially
            ShowEmptyState();
        }

        /// <summary>
        /// Show item details for the selected item.
        /// </summary>
        public void ShowItemDetails(Item item)
        {
            if (item == null || panelObject == null)
            {
                ShowEmptyState();
                return;
            }

            // Set icon
            if (itemIconImage != null)
            {
                itemIconImage.sprite = item.Icon;
                itemIconImage.enabled = item.Icon != null;
            }

            // Set content
            if (itemNameText != null)
            {
                itemNameText.text = item.ItemName;
            }

            if (rarityText != null)
            {
                rarityText.text = item.Rarity.ToString();
                rarityText.color = GetRarityColor(item.Rarity);
            }

            if (itemTypeText != null)
            {
                itemTypeText.text = GetHumanReadableItemType(item.ItemType);
            }

            if (descriptionText != null)
            {
                descriptionText.text = item.Description;
            }

            if (weightText != null)
            {
                weightText.text = $"Weight: {item.Weight:F2} kg";
            }

            if (valueText != null)
            {
                // Show just the number (icon will be next to it if available)
                valueText.text = item.BaseValue.ToString();
                
                // Enable icon if sprite is assigned
                if (aurumShardsIcon != null)
                {
                    aurumShardsIcon.enabled = aurumShardsIcon.sprite != null;
                }
            }

            // Show equipment-specific stats
            if (statsText != null)
            {
                statsText.text = GetItemStatsText(item);
            }

            // Panel is always visible, no need to activate
        }

        /// <summary>
        /// Show empty state when no item is selected.
        /// </summary>
        public void ShowEmptyState()
        {
            if (itemIconImage != null)
            {
                itemIconImage.enabled = false;
            }

            if (itemNameText != null)
            {
                itemNameText.text = "No item selected";
            }

            if (rarityText != null)
            {
                rarityText.text = "";
            }

            if (itemTypeText != null)
            {
                itemTypeText.text = "";
            }

            if (descriptionText != null)
            {
                descriptionText.text = "Select an item to view details";
            }

            if (weightText != null)
            {
                weightText.text = "";
            }

            if (valueText != null)
            {
                valueText.text = "";
            }

            if (aurumShardsIcon != null)
            {
                aurumShardsIcon.enabled = false;
            }

            if (statsText != null)
            {
                statsText.text = "";
            }
        }

        /// <summary>
        /// Get formatted stats text for the item.
        /// </summary>
        private string GetItemStatsText(Item item)
        {
            if (item is EquipmentItem equipment)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                if (equipment.AttackBonus != 0)
                    sb.AppendLine($"Attack: +{equipment.AttackBonus}");
                if (equipment.DefenseBonus != 0)
                    sb.AppendLine($"Defense: +{equipment.DefenseBonus}");
                if (equipment.StrengthBonus != 0)
                    sb.AppendLine($"Strength: +{equipment.StrengthBonus}");
                if (equipment.FinesseBonus != 0)
                    sb.AppendLine($"Finesse: +{equipment.FinesseBonus}");
                if (equipment.FocusBonus != 0)
                    sb.AppendLine($"Focus: +{equipment.FocusBonus}");
                if (equipment.SpeedBonus != 0)
                    sb.AppendLine($"Speed: +{equipment.SpeedBonus}");
                if (equipment.LuckBonus != 0)
                    sb.AppendLine($"Luck: +{equipment.LuckBonus}");

                if (equipment.HasDurability)
                {
                    sb.AppendLine($"Durability: {equipment.CurrentDurability:F0}/{equipment.MaxDurability:F0}");
                    if (equipment.IsBroken)
                        sb.AppendLine("<color=red>[BROKEN]</color>");
                }

                return sb.ToString();
            }

            return "";
        }


        /// <summary>
        /// Get human-readable name for item type.
        /// </summary>
        private string GetHumanReadableItemType(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Loot => "Loot",
                ItemType.ConsumableBattle => "Battle Consumable",
                ItemType.ConsumableNonBattle => "Consumable",
                ItemType.Equipment => "Equipment",
                ItemType.Container => "Container",
                ItemType.KeyItem => "Key Item",
                _ => itemType.ToString()
            };
        }

        /// <summary>
        /// Get color for item rarity.
        /// </summary>
        private Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => Color.white,
                ItemRarity.Uncommon => Color.green,
                ItemRarity.Rare => Color.blue,
                ItemRarity.Epic => new Color(0.64f, 0.21f, 0.93f), // Purple
                ItemRarity.Legendary => new Color(1f, 0.5f, 0f), // Orange
                _ => Color.white
            };
        }
    }
}
