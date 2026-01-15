using UnityEngine;

namespace Riftbourne.Items
{
    /// <summary>
    /// Represents key items - quest items and story items.
    /// These items cannot be sold, dropped, or traded (if character bound).
    /// </summary>
    [CreateAssetMenu(fileName = "New Key Item", menuName = "Riftbourne/Items/Key Item")]
    public class KeyItem : Item
    {
        [Header("Key Item Properties")]
        [Tooltip("Is this item bound to a specific character?")]
        [SerializeField] private bool isCharacterBound = false;

        [Tooltip("Which character owns this item (empty if not bound)")]
        [SerializeField] private string boundCharacterName = "";

        // Public properties
        public bool IsCharacterBound => isCharacterBound;
        public string BoundCharacterName => boundCharacterName;

        private void OnEnable()
        {
            itemType = ItemType.KeyItem;
            maxStackSize = 1; // Key items don't stack
            weight = 0f; // Key items always have 0 weight
        }

        private void OnValidate()
        {
            // Ensure weight is always 0 for key items
            if (weight != 0f)
            {
                weight = 0f;
            }
        }

        /// <summary>
        /// Checks if this item can be traded between party members.
        /// </summary>
        public bool CanTrade()
        {
            return !isCharacterBound;
        }

        /// <summary>
        /// Checks if this item can be sold to vendors.
        /// Key items cannot be sold.
        /// </summary>
        public bool CanSell()
        {
            return false;
        }

        /// <summary>
        /// Checks if this item can be dropped.
        /// Key items cannot be dropped.
        /// </summary>
        public bool CanDrop()
        {
            return false;
        }

        /// <summary>
        /// Checks if this item can be used.
        /// Key items cannot be used directly (they may trigger events when in inventory).
        /// </summary>
        public bool CanUse()
        {
            return false;
        }

        /// <summary>
        /// Override tooltip to add quest item tags.
        /// </summary>
        public override string GetTooltipText()
        {
            string tooltip = base.GetTooltipText();
            tooltip += "\n\n<b>[Quest Item]</b>";
            
            if (isCharacterBound && !string.IsNullOrEmpty(boundCharacterName))
            {
                tooltip += $"\n<b>[Character Bound: {boundCharacterName}]</b>";
            }
            else if (isCharacterBound)
            {
                tooltip += "\n<b>[Character Bound]</b>";
            }

            return tooltip;
        }
    }
}
