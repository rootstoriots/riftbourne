using UnityEngine;

namespace Riftbourne.Items
{
    /// <summary>
    /// Represents container items like bags and backpacks.
    /// These items provide additional inventory slots and reduce encumbrance.
    /// </summary>
    [CreateAssetMenu(fileName = "New Container Item", menuName = "Riftbourne/Items/Container Item")]
    public class ContainerItem : Item
    {
        [Header("Container Properties")]
        [Tooltip("How many items can be stored inside this container")]
        [SerializeField] private int slotCapacity = 10;

        [Tooltip("Encumbrance reduction as decimal (0.5 = 50% reduction)")]
        [Range(0f, 1f)]
        [SerializeField] private float encumbranceReduction = 0.15f;

        // Public properties
        public int SlotCapacity => slotCapacity;
        public float EncumbranceReduction => encumbranceReduction;

        private void OnEnable()
        {
            itemType = ItemType.Container;
            maxStackSize = 1; // Containers don't stack
        }

        /// <summary>
        /// Gets a formatted description of the container's capacity and weight reduction.
        /// </summary>
        public string GetCapacityDescription()
        {
            int reductionPercent = Mathf.RoundToInt(encumbranceReduction * 100f);
            return $"Capacity: {slotCapacity} slots, Weight reduction: {reductionPercent}%";
        }

        /// <summary>
        /// Override tooltip to include capacity information.
        /// </summary>
        public override string GetTooltipText()
        {
            string tooltip = base.GetTooltipText();
            tooltip += $"\n\n<b>{GetCapacityDescription()}</b>";
            return tooltip;
        }
    }
}
