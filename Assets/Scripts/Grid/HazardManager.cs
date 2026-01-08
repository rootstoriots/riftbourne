using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Core;
using Riftbourne.Combat;

namespace Riftbourne.Grid
{
    /// <summary>
    /// Manages all grid hazards (fire tiles, ice patches, poison clouds, etc.)
    /// Handles creation, updates, visuals, and damage application
    /// </summary>
    public class HazardManager : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        [Tooltip("Hazard data configurations for each hazard type. Add one for each type you want to use.")]
        [SerializeField] private List<HazardData> hazardDataConfigs = new List<HazardData>();

        [Header("Legacy Fire Hazard Settings (Deprecated - use hazardDataConfigs)")]
        [SerializeField] private Material fireMaterial;

        private GridManager gridManager;

        private void Awake()
        {
            ManagerRegistry.Register(this);
            gridManager = ManagerRegistry.Get<GridManager>();
        }

        private void OnDestroy()
        {
            ManagerRegistry.Unregister(this);
        }

        /// <summary>
        /// Creates a hazard using HazardData on the specified grid cell.
        /// This is the main method for creating hazards - use this for new code.
        /// </summary>
        public void CreateHazard(HazardData hazardData, int x, int y, int directDamageOverride = 0, int durationOverride = 0)
        {
            if (hazardData == null)
            {
                Debug.LogError("HazardManager.CreateHazard: HazardData is null!");
                return;
            }

            // Ensure gridManager is available
            if (gridManager == null)
            {
                gridManager = ManagerRegistry.Get<GridManager>();
            }

            if (gridManager == null)
            {
                Debug.LogError($"HazardManager.CreateHazard: GridManager not available!");
                return;
            }

            if (!gridManager.IsValidGridPosition(x, y))
            {
                Debug.LogWarning($"Cannot create {hazardData.HazardName} hazard at invalid position ({x}, {y})");
                return;
            }

            GridCell cell = gridManager.GetCell(x, y);
            if (cell == null)
            {
                Debug.LogError($"HazardManager.CreateHazard: Cell at ({x}, {y}) is null!");
                return;
            }

            // If same hazard data already exists, refresh duration (unless it's permanent)
            if (cell.Hazard != null && cell.Hazard.Data == hazardData && !cell.Hazard.IsPermanent)
            {
                int newDuration = durationOverride > 0 ? durationOverride : 3; // Default
                cell.Hazard.RefreshDuration(newDuration);
                Debug.Log($"{hazardData.HazardName} at ({x}, {y}) refreshed to {cell.Hazard.RemainingTurns} turns");
                return;
            }

            // Use override values or defaults from HazardData
            int directDamage = directDamageOverride > 0 ? directDamageOverride : hazardData.DirectDamagePerTurn;
            int duration = durationOverride > 0 ? durationOverride : (hazardData.IsNatural ? -1 : 3);

            // Create new hazard
            HazardTile hazard = new HazardTile(hazardData, directDamage, duration);
            cell.Hazard = hazard;

            // Create visual using hazard data
            CreateHazardVisual(cell, hazard);

            string durationText = hazard.IsPermanent ? "permanent" : $"{duration} turns";
            Debug.Log($"{hazardData.HazardName} created at ({x}, {y}) - {directDamage} direct damage for {durationText}");

            // If a unit is already standing on this cell, apply effects immediately
            Unit unitOnCell = cell.OccupyingUnit;
            
            // Fallback: Search for units at this position if OccupyingUnit isn't set
            if (unitOnCell == null)
            {
                Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
                foreach (Unit unit in allUnits)
                {
                    if (unit != null && unit.IsAlive && unit.GridX == x && unit.GridY == y)
                    {
                        unitOnCell = unit;
                        Debug.Log($"[HAZARD CREATED] Found unit {unit.UnitName} at ({x}, {y}) via position check (OccupyingUnit was null)");
                        break;
                    }
                }
            }
            
            if (unitOnCell != null)
            {
                Debug.Log($"[HAZARD CREATED] Unit {unitOnCell.UnitName} is on hazard cell ({x}, {y}), applying immediate effects");
                ApplyHazardEffectsToUnit(unitOnCell, cell);
            }
            else
            {
                Debug.Log($"[HAZARD CREATED] No unit found at cell ({x}, {y}) when hazard was created");
            }
        }

        /// <summary>
        /// Legacy method for backward compatibility - creates hazard from enum type.
        /// </summary>
        public void CreateHazard(HazardTile.HazardType hazardType, int x, int y, int damagePerTurn, int duration)
        {
            // Try to find matching HazardData from configs
            HazardData matchingData = null;
            foreach (var data in hazardDataConfigs)
            {
                if (data != null && data.StatusEffectData != null)
                {
                    // Try to match by name (legacy support)
                    switch (hazardType)
                    {
                        case HazardTile.HazardType.Fire:
                            if (data.StatusEffectData.EffectName == "Burn")
                                matchingData = data;
                            break;
                        case HazardTile.HazardType.Poison:
                            if (data.StatusEffectData.EffectName == "Poison")
                                matchingData = data;
                            break;
                    }
                    if (matchingData != null) break;
                }
            }

            if (matchingData != null)
            {
                CreateHazard(matchingData, x, y, damagePerTurn, duration);
            }
            else
            {
                Debug.LogWarning($"No HazardData found for {hazardType}. Please use CreateHazard(HazardData, ...) instead.");
            }
        }


        /// <summary>
        /// Creates the visual representation for a hazard based on its HazardData configuration.
        /// </summary>
        private void CreateHazardVisual(GridCell cell, HazardTile hazard)
        {
            // Get hazard data directly from hazard
            HazardData data = hazard.Data;

            // Create horizontal quad ABOVE the ground plane
            GameObject hazardQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            string hazardName = data != null ? data.HazardName : hazard.Type.ToString();
            hazardQuad.name = $"{hazardName}_{cell.X}_{cell.Y}";
            
            float height = data != null ? data.VisualHeight : 0.03f;
            Vector3 scale = data != null ? data.VisualScale : Vector3.one * 0.8f;
            
            hazardQuad.transform.position = cell.WorldPosition + Vector3.up * height;
            hazardQuad.transform.rotation = Quaternion.Euler(90, 0, 0); // Horizontal
            hazardQuad.transform.localScale = scale;

            // Apply material
            Renderer renderer = hazardQuad.GetComponent<Renderer>();
            Material materialToUse = null;

            if (data != null && data.HazardMaterial != null)
            {
                materialToUse = data.HazardMaterial;
            }
            else if (data != null && data.StatusEffectData != null && data.StatusEffectData.EffectName == "Burn" && fireMaterial != null)
            {
                // Legacy fallback for fire (check by name)
                materialToUse = fireMaterial;
            }

            if (materialToUse != null)
            {
                renderer.material = materialToUse;
            }
            else
            {
                // Create fallback material
                Material fallbackMat = new Material(Shader.Find("Standard"));
                Color color = data != null ? data.FallbackColor : new Color(1f, 0.5f, 0f);
                fallbackMat.color = color;
                
                if (data != null && data.UseEmission)
                {
                    fallbackMat.EnableKeyword("_EMISSION");
                    fallbackMat.SetColor("_EmissionColor", data.EmissionColor * 0.5f);
                }
                else if (data != null && data.StatusEffectData != null && data.StatusEffectData.EffectName == "Burn")
                {
                    // Legacy fire emission (check by name)
                    fallbackMat.EnableKeyword("_EMISSION");
                    fallbackMat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0f) * 0.5f);
                }
                
                renderer.material = fallbackMat;
            }

            // Remove collider (we don't need it)
            Destroy(hazardQuad.GetComponent<Collider>());

            hazard.VisualObject = hazardQuad;
        }


        /// <summary>
        /// Applies all effects from a hazard to a unit (damage and status effects).
        /// </summary>
        private void ApplyHazardEffectsToUnit(Unit unit, GridCell cell)
        {
            if (unit == null || cell == null || cell.Hazard == null)
                return;

            // Apply direct damage
            ApplyHazardDamageToUnit(unit, cell);

            // Apply status effect if configured
            HazardData data = cell.Hazard.Data;
            if (data != null && data.AppliesStatusEffect && data.StatusEffectData != null)
            {
                ApplyStatusEffectFromHazard(unit, data);
            }
        }

        /// <summary>
        /// Applies a status effect to a unit based on hazard data configuration.
        /// NOTE: This applies to ALL units (both player and enemy/AI units).
        /// </summary>
        private void ApplyStatusEffectFromHazard(Unit unit, HazardData data)
        {
            if (data.StatusEffectData == null)
            {
                return;
            }

            // Get duration (use override if set, otherwise from StatusEffectData)
            int duration = data.GetStatusEffectDuration();
            if (duration <= 0)
            {
                duration = 3; // Default fallback
            }

            // Apply status effect using StatusEffectData directly
            unit.ApplyStatusEffect(data.StatusEffectData, duration);
        }

        /// <summary>
        /// Applies status effects to a unit when passing through a hazard tile (during movement).
        /// Only applies status effects, NOT damage (damage is only applied when starting/ending turn on hazard).
        /// NOTE: This applies to ALL units (both player and enemy/AI units).
        /// </summary>
        public void ApplyStatusEffectFromPassingThrough(Unit unit, GridCell cell)
        {
            if (unit == null || cell == null || cell.Hazard == null)
            {
                return;
            }

            HazardData data = cell.Hazard.Data;
            if (data == null || !data.AppliesStatusEffect || data.StatusEffectData == null)
            {
                return;
            }

            string unitType = unit.IsPlayerControlled ? "Player" : "Enemy";
            string hazardName = data.HazardName;
            Debug.Log($"[HAZARD PASS-THROUGH] {unitType} unit {unit.UnitName} passed through {hazardName} hazard at ({cell.X}, {cell.Y}), applying status effect");

            // Apply status effect (no damage for passing through)
            ApplyStatusEffectFromHazard(unit, data);
        }

        /// <summary>
        /// Updates all hazards (called at end of each turn by TurnManager)
        /// Decrements durations and removes expired hazards
        /// </summary>
        public void UpdateHazards()
        {
            // Ensure gridManager is available
            if (gridManager == null)
            {
                gridManager = ManagerRegistry.Get<GridManager>();
            }
            
            if (gridManager == null)
            {
                Debug.LogWarning("HazardManager.UpdateHazards() called but GridManager is not available!");
                return;
            }

            for (int x = 0; x < gridManager.GridWidth; x++)
            {
                for (int y = 0; y < gridManager.GridHeight; y++)
                {
                    GridCell cell = gridManager.GetCell(x, y);

                    if (cell != null && cell.Hazard != null)
                    {
                        // Skip decrement for permanent/natural hazards
                        if (!cell.Hazard.IsPermanent)
                        {
                            bool shouldRemove = cell.Hazard.DecrementDuration();

                            if (shouldRemove)
                            {
                                RemoveHazard(cell);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies hazard damage to unit at specified position
        /// Called by TurnManager when unit ends turn (DEPRECATED - keeping for compatibility)
        /// </summary>
        public void ApplyHazardDamage(Unit unit, int x, int y)
        {
            // Ensure gridManager is available
            if (gridManager == null)
            {
                gridManager = ManagerRegistry.Get<GridManager>();
            }

            if (gridManager == null || !gridManager.IsValidGridPosition(x, y))
                return;

            GridCell cell = gridManager.GetCell(x, y);
            if (cell == null)
                return;

            ApplyHazardDamageToUnit(unit, cell);
        }

        /// <summary>
        /// Applies hazard damage to unit from a specific cell.
        /// Only damages once per hazard type per turn.
        /// Also applies status effects if configured in HazardData.
        /// NOTE: This applies to ALL units (both player and enemy/AI units).
        /// </summary>
        public void ApplyHazardDamageToUnit(Unit unit, GridCell cell)
        {
            if (unit == null)
            {
                Debug.LogWarning("HazardManager.ApplyHazardDamageToUnit: Unit is null!");
                return;
            }

            if (cell == null)
            {
                Debug.LogWarning($"HazardManager.ApplyHazardDamageToUnit: Cell is null for {unit.UnitName}!");
                return;
            }

            string unitType = unit.IsPlayerControlled ? "Player" : "Enemy";
            Debug.Log($"[HAZARD DEBUG] ApplyHazardDamageToUnit called for {unitType} unit {unit.UnitName} at cell ({cell.X}, {cell.Y})");

            if (cell.Hazard == null)
            {
                Debug.LogWarning($"[HAZARD DEBUG] Cell ({cell.X}, {cell.Y}) has no hazard! Cannot apply damage to {unit.UnitName}");
                return;
            }

            // Get hazard name once for use throughout the method
            string hazardName = cell.Hazard.Data != null ? cell.Hazard.Data.HazardName : cell.Hazard.Type.ToString();
            Debug.Log($"[HAZARD DEBUG] Cell ({cell.X}, {cell.Y}) has {hazardName} hazard with {cell.Hazard.DirectDamagePerTurn} direct damage per turn");

            // Check if unit has already been damaged by this hazard type this turn
            if (unit.HasBeenDamagedByHazard(cell.Hazard.Type))
            {
                Debug.Log($"[HAZARD DEBUG] {unit.UnitName} already took {cell.Hazard.Type} damage this turn, skipping");
                return;
            }

            // Verify unit is actually on this cell
            if (unit.GridX != cell.X || unit.GridY != cell.Y)
            {
                Debug.LogWarning($"[HAZARD DEBUG] Unit {unit.UnitName} is at ({unit.GridX}, {unit.GridY}) but hazard cell is ({cell.X}, {cell.Y}) - position mismatch!");
                // Still apply damage if the unit is supposed to be on this cell
            }

            // Apply direct damage (hazards bypass defense)
            int damage = cell.Hazard.DirectDamagePerTurn;
            if (damage > 0)
            {
                Debug.Log($"[HAZARD DEBUG] Applying {damage} {hazardName} direct damage to {unit.UnitName} at ({cell.X}, {cell.Y}) (bypassing defense)");
                
                int actualDamage = unit.TakeDamageBypassDefense(damage);
                unit.MarkDamagedByHazard(cell.Hazard.Type);

                Debug.Log($"[HAZARD] {unitType} unit {unit.UnitName} took {actualDamage} {hazardName} direct damage at ({cell.X}, {cell.Y}). HP: {unit.CurrentHP}/{unit.MaxHP}");
            }

            // Apply status effect if configured in HazardData
            HazardData data = cell.Hazard.Data;
            if (data != null && data.AppliesStatusEffect && data.StatusEffectData != null)
            {
                ApplyStatusEffectFromHazard(unit, data);
            }
        }

        /// <summary>
        /// Removes hazard from cell and destroys visual
        /// </summary>
        private void RemoveHazard(GridCell cell)
        {
            if (cell.Hazard != null)
            {
                // Destroy visual
                if (cell.Hazard.VisualObject != null)
                {
                    Destroy(cell.Hazard.VisualObject);
                }

                string hazardName = cell.Hazard.Data != null ? cell.Hazard.Data.HazardName : cell.Hazard.Type.ToString();
                Debug.Log($"{hazardName} expired at ({cell.X}, {cell.Y})");
                cell.Hazard = null;
            }
        }
    }
}