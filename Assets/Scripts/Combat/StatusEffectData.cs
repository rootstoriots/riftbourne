using UnityEngine;
using Riftbourne.Characters;

namespace Riftbourne.Combat
{
    /// <summary>
    /// ScriptableObject that defines a status effect type.
    /// Create these in the Project window to define new status effects without coding!
    /// Right-click -> Create -> Riftbourne -> Status Effect Data
    /// </summary>
    [CreateAssetMenu(fileName = "New Status Effect", menuName = "Riftbourne/Status Effect Data")]
    public class StatusEffectData : ScriptableObject
    {
        [Header("Status Effect Identity")]
        [SerializeField] private string effectName = "Burn";
        [TextArea(2, 4)]
        [SerializeField] private string description = "Deals damage over time";

        [Header("Effect Behavior")]
        [Tooltip("Does this effect deal damage each turn?")]
        [SerializeField] private bool dealsDamage = true;
        [Tooltip("Damage dealt per turn (only used if dealsDamage is true)")]
        [SerializeField] private int damagePerTurn = 5;
        
        [Tooltip("Does this effect heal each turn?")]
        [SerializeField] private bool healsOverTime = false;
        [Tooltip("Healing per turn (only used if healsOverTime is true)")]
        [SerializeField] private int healingPerTurn = 0;

        [Tooltip("Does this effect modify movement speed?")]
        [SerializeField] private bool modifiesMovement = false;
        [Tooltip("Movement speed multiplier (1.0 = normal, 0.5 = half speed, 2.0 = double speed)")]
        [SerializeField] private float movementMultiplier = 1.0f;

        [Tooltip("Does this effect prevent actions?")]
        [SerializeField] private bool preventsActions = false;

        [Tooltip("Does this effect prevent movement?")]
        [SerializeField] private bool preventsMovement = false;

        [Header("Visual/UI")]
        [Tooltip("Color for UI display of this status effect")]
        [SerializeField] private Color displayColor = Color.red;
        [Tooltip("Icon sprite for this status effect (optional)")]
        [SerializeField] private Sprite icon;

        // Public properties
        public string EffectName => effectName;
        public string Description => description;
        public bool DealsDamage => dealsDamage;
        public int DamagePerTurn => damagePerTurn;
        public bool HealsOverTime => healsOverTime;
        public int HealingPerTurn => healingPerTurn;
        public bool ModifiesMovement => modifiesMovement;
        public float MovementMultiplier => movementMultiplier;
        public bool PreventsActions => preventsActions;
        public bool PreventsMovement => preventsMovement;
        public Color DisplayColor => displayColor;
        public Sprite Icon => icon;
    }
}

