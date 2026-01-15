using UnityEngine;

namespace Riftbourne.Items
{
    /// <summary>
    /// Represents loot items - vendor trash and crafting materials.
    /// These items can be sold or used for crafting.
    /// </summary>
    [CreateAssetMenu(fileName = "New Loot Item", menuName = "Riftbourne/Items/Loot Item")]
    public class LootItem : Item
    {
        [Header("Loot Properties")]
        [Tooltip("Is this item a crafting material?")]
        [SerializeField] private bool isCraftingMaterial = false;

        public bool IsCraftingMaterial => isCraftingMaterial;

        private void OnEnable()
        {
            itemType = ItemType.Loot;
            maxStackSize = 10;
        }
    }
}
