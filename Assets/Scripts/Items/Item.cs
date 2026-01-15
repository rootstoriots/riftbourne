using UnityEngine;

namespace Riftbourne.Items
{
    /// <summary>
    /// Base abstract class for all items in the game.
    /// Provides common properties and functionality for all item types.
    /// </summary>
    public abstract class Item : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] protected string itemName;
        [TextArea(2, 4)]
        [SerializeField] protected string description;
        [SerializeField] protected Sprite icon;

        [Header("Item Properties")]
        [SerializeField] protected ItemRarity rarity = ItemRarity.Common;
        [SerializeField] protected float weight = 0f; // Weight in kg
        [SerializeField] protected int baseValue = 0; // Value in Aurum Shards
        [SerializeField] protected int maxStackSize = 1;

        // Readonly property set by derived classes
        protected ItemType itemType;

        // Public properties
        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemRarity Rarity => rarity;
        public float Weight => weight;
        public int BaseValue => baseValue;
        public int MaxStackSize => maxStackSize;
        public ItemType ItemType => itemType;

        /// <summary>
        /// Gets formatted tooltip text for this item.
        /// Includes name, rarity, weight, value, and description.
        /// </summary>
        public virtual string GetTooltipText()
        {
            string tooltip = $"<b>{itemName}</b>\n";
            tooltip += $"<color=#{GetRarityColor()}>{rarity}</color>\n";
            tooltip += $"Weight: {weight} kg\n";
            tooltip += $"Value: {baseValue} Aurum Shards\n";
            tooltip += $"\n{description}";
            return tooltip;
        }

        /// <summary>
        /// Gets the color hex code for the rarity level.
        /// </summary>
        protected string GetRarityColor()
        {
            return rarity switch
            {
                ItemRarity.Common => "FFFFFF",      // White
                ItemRarity.Uncommon => "1EFF00",   // Green
                ItemRarity.Rare => "0070DD",        // Blue
                ItemRarity.Epic => "A335EE",        // Purple
                ItemRarity.Legendary => "FF8000",   // Orange
                _ => "FFFFFF"
            };
        }
    }
}
