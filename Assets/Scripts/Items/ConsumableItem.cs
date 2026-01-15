using UnityEngine;
using System.Collections.Generic;

namespace Riftbourne.Items
{
    /// <summary>
    /// Represents consumable items that can be used to apply effects.
    /// Can be used in combat, outside combat, or both.
    /// </summary>
    [CreateAssetMenu(fileName = "New Consumable Item", menuName = "Riftbourne/Items/Consumable Item")]
    public class ConsumableItem : Item
    {
        [Header("Usage Context")]
        [Tooltip("Can this item be used during combat?")]
        [SerializeField] private bool usableInCombat = true;
        
        [Tooltip("Can this item be used outside of combat?")]
        [SerializeField] private bool usableOutOfCombat = true;

        [Header("Combat Properties")]
        [Tooltip("Duration in battles (0 = instant effect, >0 = buff lasts X battles)")]
        [SerializeField] private int combatDuration = 0;

        [Header("Targeting")]
        [Tooltip("Who or what can this item target?")]
        [SerializeField] private ConsumableTargetType targetType = ConsumableTargetType.Self;
        
        [Tooltip("Range in grid cells for combat targeting")]
        [SerializeField] private int range = 1;

        [Header("Effects")]
        [Tooltip("List of effects this consumable applies")]
        [SerializeField] private List<ConsumableEffect> effects = new List<ConsumableEffect>();

        // Public properties
        public bool UsableInCombat => usableInCombat;
        public bool UsableOutOfCombat => usableOutOfCombat;
        public int CombatDuration => combatDuration;
        public ConsumableTargetType TargetType => targetType;
        public int Range => range;
        public List<ConsumableEffect> Effects => effects;

        private void OnEnable()
        {
            // Set item type based on usage flags
            if (usableInCombat)
            {
                itemType = ItemType.ConsumableBattle;
            }
            else
            {
                itemType = ItemType.ConsumableNonBattle;
            }
            
            maxStackSize = 10;
        }

        /// <summary>
        /// Checks if this consumable can be used in the current context.
        /// </summary>
        public bool CanUseInCurrentContext(bool inCombat)
        {
            if (inCombat)
            {
                return usableInCombat;
            }
            else
            {
                return usableOutOfCombat;
            }
        }

        /// <summary>
        /// Gets a formatted description of all effects this consumable applies.
        /// </summary>
        public string GetEffectsDescription()
        {
            if (effects == null || effects.Count == 0)
            {
                return "No effects";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                string effectText = GetEffectDescription(effect);
                sb.Append(effectText);
                
                if (i < effects.Count - 1)
                {
                    sb.Append("\n");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a formatted description for a single effect.
        /// </summary>
        private string GetEffectDescription(ConsumableEffect effect)
        {
            return effect.effectType switch
            {
                ConsumableEffectType.Heal => $"Heal {effect.magnitude} HP (restores current HP)",
                ConsumableEffectType.Damage => $"Deal {effect.magnitude} damage",
                ConsumableEffectType.BuffStat => $"Buff {effect.affectedStat} by {effect.magnitude} for {effect.duration} turns",
                ConsumableEffectType.DebuffStat => $"Debuff {effect.affectedStat} by {effect.magnitude} for {effect.duration} turns",
                ConsumableEffectType.RemoveStatusEffect => $"Remove status effect",
                ConsumableEffectType.ApplyStatusEffect => $"Apply status effect",
                _ => "Unknown effect"
            };
        }

        /// <summary>
        /// Override tooltip to include effects description.
        /// </summary>
        public override string GetTooltipText()
        {
            string tooltip = base.GetTooltipText();
            tooltip += $"\n\n<b>Effects:</b>\n{GetEffectsDescription()}";
            return tooltip;
        }
    }
}
