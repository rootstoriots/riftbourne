using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Core;

namespace Riftbourne.UI
{
    /// <summary>
    /// Manages damage and healing indicators for all units.
    /// Subscribes to GameEvents and creates floating text indicators.
    /// </summary>
    public class DamageIndicatorManager : MonoBehaviour
    {
        private static DamageIndicatorManager instance;

        public static DamageIndicatorManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject managerObj = new GameObject("DamageIndicatorManager");
                    instance = managerObj.AddComponent<DamageIndicatorManager>();
                    instance.enabled = true; // Ensure it's enabled so OnEnable is called
                    // Manually subscribe to events immediately (OnEnable may not be called yet)
                    GameEvents.OnUnitDamaged += instance.OnUnitDamaged;
                    GameEvents.OnUnitHealed += instance.OnUnitHealed;
                    DontDestroyOnLoad(managerObj);
                    Debug.Log("DamageIndicatorManager: Auto-initialized");
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Ensure we're subscribed to events
            if (!enabled)
            {
                enabled = true;
            }
        }

        private void OnEnable()
        {
            // Subscribe to damage and healing events
            GameEvents.OnUnitDamaged += OnUnitDamaged;
            GameEvents.OnUnitHealed += OnUnitHealed;
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            GameEvents.OnUnitDamaged -= OnUnitDamaged;
            GameEvents.OnUnitHealed -= OnUnitHealed;
        }

        private void OnUnitDamaged(Unit unit, int damageAmount)
        {
            if (unit != null && damageAmount > 0)
            {
                Debug.Log($"DamageIndicatorManager: Unit {unit.UnitName} took {damageAmount} damage");
                DamageIndicator.Show(unit, damageAmount, isHealing: false);
            }
        }

        private void OnUnitHealed(Unit unit, int healAmount)
        {
            if (unit != null && healAmount > 0)
            {
                Debug.Log($"DamageIndicatorManager: Unit {unit.UnitName} healed {healAmount}");
                DamageIndicator.Show(unit, healAmount, isHealing: true);
            }
        }
    }
}

