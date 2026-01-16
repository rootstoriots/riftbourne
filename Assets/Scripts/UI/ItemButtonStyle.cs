using UnityEngine;
using Riftbourne.Items;

namespace Riftbourne.UI
{
    /// <summary>
    /// Defines the visual style for item buttons based on item type.
    /// Contains button sprite, base color, and rarity color multipliers.
    /// </summary>
    [CreateAssetMenu(fileName = "New Item Button Style", menuName = "Riftbourne/UI/Item Button Style")]
    public class ItemButtonStyle : ScriptableObject
    {
        [Header("Item Type")]
        [Tooltip("The item type this style applies to")]
        [SerializeField] private ItemType itemType = ItemType.Loot;

        [Header("Button Appearance")]
        [Tooltip("The sprite/overlay image for the button background")]
        [SerializeField] private Sprite buttonSprite;
        [Tooltip("Base color for the button overlay")]
        [SerializeField] private Color baseColor = Color.white;

        [Header("Rarity Color Multipliers")]
        [Tooltip("Color multiplier for Common rarity (applied to base color)")]
        [SerializeField] private Color commonColor = Color.white;
        [Tooltip("Color multiplier for Uncommon rarity")]
        [SerializeField] private Color uncommonColor = new Color(0.8f, 1f, 0.8f); // Light green tint
        [Tooltip("Color multiplier for Rare rarity")]
        [SerializeField] private Color rareColor = new Color(0.8f, 0.8f, 1f); // Light blue tint
        [Tooltip("Color multiplier for Epic rarity")]
        [SerializeField] private Color epicColor = new Color(1f, 0.8f, 1f); // Light purple tint
        [Tooltip("Color multiplier for Legendary rarity")]
        [SerializeField] private Color legendaryColor = new Color(1f, 0.9f, 0.7f); // Light orange tint

        [Header("Transparency")]
        [Tooltip("Alpha value for the button overlay (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float overlayAlpha = 0.5f;

        // Properties
        public ItemType ItemType => itemType;
        public Sprite ButtonSprite => buttonSprite;
        public Color BaseColor => baseColor;
        public float OverlayAlpha => overlayAlpha;

        /// <summary>
        /// Get the color multiplier for a specific rarity.
        /// </summary>
        public Color GetRarityColor(ItemRarity rarity)
        {
            Color multiplier = rarity switch
            {
                ItemRarity.Common => commonColor,
                ItemRarity.Uncommon => uncommonColor,
                ItemRarity.Rare => rareColor,
                ItemRarity.Epic => epicColor,
                ItemRarity.Legendary => legendaryColor,
                _ => commonColor
            };

            // Apply base color and alpha
            Color finalColor = baseColor * multiplier;
            finalColor.a = overlayAlpha;
            return finalColor;
        }
    }
}
