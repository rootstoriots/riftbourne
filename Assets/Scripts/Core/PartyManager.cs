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
        private GameObject currentSelectionRing;

        public Unit SelectedUnit => selectedUnit;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
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
            if (unit == null || !unit.IsPlayerControlled)
            {
                Debug.LogWarning("Cannot select null or non-player unit");
                return;
            }

            // Check if unit is in the current turn window
            TurnManager turnManager = FindFirstObjectByType<TurnManager>();
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
        /// Get all player-controlled units in the scene.
        /// </summary>
        public List<Unit> GetPartyMembers()
        {
            Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            List<Unit> party = new List<Unit>();

            foreach (Unit unit in allUnits)
            {
                if (unit.IsPlayerControlled)
                {
                    party.Add(unit);
                }
            }

            return party;
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
            // Destroy old ring
            if (currentSelectionRing != null)
            {
                Destroy(currentSelectionRing);
            }

            if (selectedUnit == null) return;

            // Create new ring
            if (selectionRingPrefab != null)
            {
                currentSelectionRing = Instantiate(selectionRingPrefab, selectedUnit.transform);
                currentSelectionRing.transform.localPosition = new Vector3(0, 0.01f, 0); // Just above ground
                currentSelectionRing.transform.localRotation = Quaternion.Euler(90, 0, 0); // Flat on ground
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