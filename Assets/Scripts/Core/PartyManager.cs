using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Combat;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Core
{
    /// <summary>
    /// Manages the player's party and which unit is currently selected for control.
    /// Singleton pattern for easy access from UI and input systems.
    /// Tracks CharacterStates for party management and Units for battle selection.
    /// </summary>
    public class PartyManager : MonoBehaviour
    {
        public static PartyManager Instance { get; private set; }

        [Header("Party Management")]
        [SerializeField] private int partySizeLimit = 6;
        private List<CharacterState> partyMembers = new List<CharacterState>();
        private CharacterState povCharacter;

        [Header("Selected Unit (Battle Mode)")]
        private Unit selectedUnit;

        [Header("Selection Visuals")]
        [SerializeField] private GameObject selectionRingPrefab;
        [SerializeField] private ObjectPool selectionRingPool;
        private GameObject currentSelectionRing;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip unitSelectionSound;

        public Unit SelectedUnit => selectedUnit;
        public CharacterState POVCharacter => povCharacter;
        public int PartySizeLimit => partySizeLimit;

        private TurnManager turnManager;
        private List<Unit> registeredPartyMembers = new List<Unit>(); // For backward compatibility during migration

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            turnManager = ManagerRegistry.Get<TurnManager>();

            // Initialize audio source if not assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
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

            // Play unit selection sound
            PlayUnitSelectionSound();

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
        /// Legacy method for backward compatibility (battle mode only).
        /// </summary>
        public List<Unit> GetPartyMembersAsUnits()
        {
            // Return a copy to prevent external modification
            return new List<Unit>(registeredPartyMembers);
        }

        #region CharacterState Party Management

        /// <summary>
        /// Add a character to the party.
        /// Validates party size limit.
        /// </summary>
        public bool AddPartyMember(CharacterState character)
        {
            if (character == null)
            {
                Debug.LogWarning("PartyManager: Cannot add null character to party!");
                return false;
            }

            if (partyMembers.Contains(character))
            {
                Debug.LogWarning($"PartyManager: Character {character.CharacterID} is already in party!");
                return false;
            }

            if (partyMembers.Count >= partySizeLimit)
            {
                Debug.LogWarning($"PartyManager: Party is full! Cannot add {character.CharacterID} (limit: {partySizeLimit})");
                return false;
            }

            partyMembers.Add(character);
            Debug.Log($"PartyManager: Added {character.CharacterID} to party ({partyMembers.Count}/{partySizeLimit})");

            // If no POV character is set and this character can be POV, set it
            if (povCharacter == null && character.Definition != null && character.Definition.IsPOVCharacter)
            {
                SetPOVCharacter(character);
            }

            return true;
        }

        /// <summary>
        /// Remove a character from the party.
        /// </summary>
        public bool RemovePartyMember(CharacterState character)
        {
            if (character == null) return false;

            if (partyMembers.Remove(character))
            {
                Debug.Log($"PartyManager: Removed {character.CharacterID} from party ({partyMembers.Count}/{partySizeLimit})");

                // If removed character was POV, clear POV or set first available POV character
                if (povCharacter == character)
                {
                    povCharacter = null;
                    // Try to find another POV character
                    foreach (var member in partyMembers)
                    {
                        if (member.Definition != null && member.Definition.IsPOVCharacter)
                        {
                            SetPOVCharacter(member);
                            break;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Set the POV (Point of View) character - the protagonist.
        /// </summary>
        public bool SetPOVCharacter(CharacterState character)
        {
            if (character == null)
            {
                Debug.LogWarning("PartyManager: Cannot set null character as POV!");
                return false;
            }

            if (!partyMembers.Contains(character))
            {
                Debug.LogWarning($"PartyManager: Cannot set {character.CharacterID} as POV - not in party!");
                return false;
            }

            if (character.Definition != null && !character.Definition.IsPOVCharacter)
            {
                Debug.LogWarning($"PartyManager: Character {character.CharacterID} is not marked as POV character!");
                return false;
            }

            povCharacter = character;
            Debug.Log($"PartyManager: Set {character.CharacterID} as POV character");
            return true;
        }

        /// <summary>
        /// Get all party members as CharacterStates.
        /// </summary>
        public List<CharacterState> GetPartyMembers()
        {
            // Return a copy to prevent external modification
            return new List<CharacterState>(partyMembers);
        }

        /// <summary>
        /// Check if a character is in the party.
        /// </summary>
        public bool IsInParty(CharacterState character)
        {
            return character != null && partyMembers.Contains(character);
        }


        /// <summary>
        /// Clear the entire party.
        /// </summary>
        public void ClearParty()
        {
            partyMembers.Clear();
            povCharacter = null;
            Debug.Log("PartyManager: Cleared party");
        }

        #endregion

        /// <summary>
        /// Automatically select the first available party member at game start.
        /// </summary>
        public void SelectFirstPartyMember()
        {
            List<Unit> party = GetPartyMembersAsUnits();
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

        /// <summary>
        /// Play unit selection sound effect.
        /// </summary>
        private void PlayUnitSelectionSound()
        {
            if (audioSource != null && unitSelectionSound != null)
            {
                audioSource.PlayOneShot(unitSelectionSound);
            }
        }
    }
}