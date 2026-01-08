using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Combat;
using System.Collections.Generic;

namespace Riftbourne.Core
{
    /// <summary>
    /// Manages the player's party and which unit is currently selected for control.
    /// Singleton pattern for easy access from UI and input systems.
    /// </summary>
    public class PartyManager : MonoBehaviour
    {
        public static PartyManager Instance { get; private set; }

        [Header("Selected Unit")]
        private Unit selectedUnit;

        [Header("Selection Visuals")]
        [SerializeField] private GameObject selectionRingPrefab;
        [SerializeField] private ObjectPool selectionRingPool;
        private GameObject currentSelectionRing;

        public Unit SelectedUnit => selectedUnit;

        private TurnManager turnManager;
        private List<Unit> registeredPartyMembers = new List<Unit>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            turnManager = ManagerRegistry.Get<TurnManager>();
        }

        private void Start()
        {
            // Find and register any units that already exist (for units spawned before PartyManager)
            Unit[] existingUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            foreach (Unit unit in existingUnits)
            {
                if (unit.Faction == Faction.Player)
                {
                    RegisterUnit(unit);
                }
            }

            // Auto-select first party member at battle start
            SelectFirstPartyMember();
        }

        /// <summary>
        /// Select a unit for player control.
        /// Creates a selection ring under the unit.
        /// Only allows selecting units in the current turn window.
        /// </summary>
        public void SelectUnit(Unit unit)
        {
            if (unit == null || unit.Faction != Faction.Player)
            {
                Debug.LogWarning("Cannot select null or non-player unit");
                return;
            }

            // Check if unit is in the current turn window
            if (turnManager != null && !turnManager.IsUnitInCurrentWindow(unit))
            {
                Debug.LogWarning($"Cannot select {unit.UnitName} - not in current turn window");
                return;
            }

            selectedUnit = unit;
            Debug.Log($"PartyManager: Selected {unit.UnitName}");

            UpdateSelectionRing();
        }

        /// <summary>
        /// Register a player-controlled unit with the party manager.
        /// </summary>
        public void RegisterUnit(Unit unit)
        {
            if (unit == null || unit.Faction != Faction.Player)
            {
                Debug.LogWarning("PartyManager: Cannot register null or non-player unit!");
                return;
            }

            if (registeredPartyMembers.Contains(unit))
            {
                Debug.LogWarning($"PartyManager: Unit {unit.UnitName} is already registered!");
                return;
            }

            registeredPartyMembers.Add(unit);
            Debug.Log($"PartyManager: Registered {unit.UnitName}");
        }

        /// <summary>
        /// Unregister a unit from the party manager.
        /// </summary>
        public void UnregisterUnit(Unit unit)
        {
            if (unit == null) return;

            if (registeredPartyMembers.Remove(unit))
            {
                Debug.Log($"PartyManager: Unregistered {unit.UnitName}");
                
                // If the unregistered unit was selected, clear selection
                if (selectedUnit == unit)
                {
                    selectedUnit = null;
                    UpdateSelectionRing();
                }
            }
        }

        /// <summary>
        /// Get all player-controlled units registered with the party manager.
        /// </summary>
        public List<Unit> GetPartyMembers()
        {
            // Return a copy to prevent external modification
            return new List<Unit>(registeredPartyMembers);
        }

        /// <summary>
        /// Automatically select the first available party member at game start.
        /// </summary>
        public void SelectFirstPartyMember()
        {
            List<Unit> party = GetPartyMembers();
            if (party.Count > 0)
            {
                SelectUnit(party[0]);
            }
        }

        private void UpdateSelectionRing()
        {
            // Return old ring to pool
            if (currentSelectionRing != null)
            {
                if (selectionRingPool != null)
                {
                    selectionRingPool.Return(currentSelectionRing);
                }
                else
                {
                    Destroy(currentSelectionRing);
                }
                currentSelectionRing = null;
            }

            if (selectedUnit == null) return;

            // Get ring from pool or create new
            if (selectionRingPool != null)
            {
                currentSelectionRing = selectionRingPool.Get();
                if (currentSelectionRing != null)
                {
                    currentSelectionRing.transform.SetParent(selectedUnit.transform);
                    currentSelectionRing.transform.localPosition = new Vector3(0, 0.01f, 0);
                    currentSelectionRing.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }
            }
            else if (selectionRingPrefab != null)
            {
                // Fallback to instantiate if no pool
                currentSelectionRing = Instantiate(selectionRingPrefab, selectedUnit.transform);
                currentSelectionRing.transform.localPosition = new Vector3(0, 0.01f, 0);
                currentSelectionRing.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            else
            {
                // Create simple ring if no prefab
                CreateSimpleSelectionRing();
            }
        }

        private void CreateSimpleSelectionRing()
        {
            currentSelectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            currentSelectionRing.name = "SelectionRing";
            currentSelectionRing.transform.SetParent(selectedUnit.transform);
            currentSelectionRing.transform.localPosition = new Vector3(0, 0.01f, 0);
            currentSelectionRing.transform.localRotation = Quaternion.Euler(0, 0, 0);
            currentSelectionRing.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f); // Wide and flat

            // White transparent material
            Renderer renderer = currentSelectionRing.GetComponent<Renderer>();
            Material ringMat = new Material(Shader.Find("Standard"));
            ringMat.color = new Color(1f, 1f, 1f, 0.5f);
            ringMat.SetFloat("_Mode", 3); // Transparent mode
            ringMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ringMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ringMat.SetInt("_ZWrite", 0);
            ringMat.DisableKeyword("_ALPHATEST_ON");
            ringMat.EnableKeyword("_ALPHABLEND_ON");
            ringMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            ringMat.renderQueue = 3000;
            renderer.material = ringMat;

            // Remove collider
            Destroy(currentSelectionRing.GetComponent<Collider>());
        }
    }
}