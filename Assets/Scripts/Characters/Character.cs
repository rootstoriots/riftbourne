using UnityEngine;
using Riftbourne.Grid;
using Riftbourne.Core;
using System;
using System.Collections.Generic;

namespace Riftbourne.Characters
{
    public class Character : MonoBehaviour
    {
        [Header("Character Identity")]
        [SerializeField] private string characterName = "Warrior";

        [Header("Grid Position")]
        [SerializeField] private int gridX;
        [SerializeField] private int gridY;

        [Header("Movement")]
        [SerializeField] private int movementRange = 5;
        
        // Visual movement speed (not gameplay-relevant, just animation)
        private const float moveSpeed = 8f;

        // Public properties
        public string CharacterName => characterName;
        public int GridX => gridX;
        public int GridY => gridY;
        public int MovementRange => movementRange;
        public bool IsMoving { get; private set; }

        // Movement state
        private Vector3 targetPosition;
        private Action onMovementComplete; // Callback when movement finishes
        private int destinationGridX;  // Where we're moving TO
        private int destinationGridY;  // Where we're moving TO
        private int lastCellX;  // Last cell we checked for hazards
        private int lastCellY;
        private List<GridCell> currentPath; // Path to follow
        private int currentPathIndex; // Which cell in path we're moving to
        private int pendingMovementCost = 0; // Track movement cost for canceled movements

        // Cached manager references (protected so derived classes can access)
        protected GridManager gridManager;
        protected HazardManager hazardManager;

        private void Awake()
        {
            // Cache manager references
            gridManager = ManagerRegistry.Get<GridManager>();
            if (gridManager == null)
            {
                gridManager = GridManager.Instance;
            }
            
            hazardManager = ManagerRegistry.Get<HazardManager>();
            if (hazardManager == null)
            {
                hazardManager = FindFirstObjectByType<HazardManager>();
            }
        }

        private void Update()
        {
            if (IsMoving)
            {
                MoveTowardsTarget();
            }
        }

        /// <summary>
        /// Sets the character's grid position and snaps to world position
        /// </summary>
        public void SetGridPosition(int x, int y, Vector3 worldPosition)
        {
            UpdateGridOccupancy(x, y);
            gridX = x;
            gridY = y;
            transform.position = worldPosition + Vector3.up * 0.5f; // Offset so capsule sits on grid
            targetPosition = transform.position;
        }

        /// <summary>
        /// Initiates movement to a new grid position, optionally following a path.
        /// </summary>
        /// <param name="path">Optional path to follow. If null, moves in straight line.</param>
        /// <param name="onComplete">Optional callback when movement finishes</param>
        public void MoveTo(int x, int y, Vector3 worldPosition, Action onComplete = null, List<GridCell> path = null)
        {
            // If already moving, cancel previous movement to prevent backtracking
            if (IsMoving)
            {
                Debug.LogWarning($"{characterName} MoveTo called while already moving - canceling previous movement to prevent backtracking");
                
                // CRITICAL FIX: If we're canceling a movement, we need to ensure movement points are still spent
                // The previous movement callback would have spent points, but if we cancel it, the callback won't fire
                // So we need to spend the points now before canceling
                if (pendingMovementCost > 0)
                {
                    Unit unit = GetComponent<Unit>();
                    if (unit != null)
                    {
                        Debug.LogWarning($"{characterName} Canceling movement that cost {pendingMovementCost} points - spending them now to prevent infinite movement bug");
                        unit.SpendMovementPoints(pendingMovementCost);
                        pendingMovementCost = 0;
                    }
                }
                
                // Clear previous movement state to prevent stale callbacks
                Action oldCallback = onMovementComplete;
                onMovementComplete = null;
                
                // If old callback exists and hasn't fired yet, we should still try to invoke it
                // But since we've already spent the points above, the callback might fail - that's okay
                // The important thing is that points are spent
                
                currentPath = null;
                currentPathIndex = 0;
                // Don't set IsMoving to false here - we'll set it to true below for the new movement
            }
            
            // Store destination
            destinationGridX = x;
            destinationGridY = y;
            onMovementComplete = onComplete;
            
            // Track where we started for hazard checking
            lastCellX = gridX;
            lastCellY = gridY;
            
            // Setup path following or direct movement
            if (path != null && path.Count > 0)
            {
                currentPath = path;
                currentPathIndex = 0;
                targetPosition = currentPath[0].WorldPosition + Vector3.up * 0.5f;
                // Calculate and store movement cost for potential cancellation
                pendingMovementCost = path.Count - 1; // Path cost is path length - 1
                Debug.Log($"{characterName} following path of {currentPath.Count} cells to ({x}, {y})");
            }
            else
            {
                currentPath = null;
                targetPosition = worldPosition + Vector3.up * 0.5f;
                // Calculate and store movement cost for potential cancellation
                int distance = Mathf.Abs(x - gridX) + Mathf.Abs(y - gridY);
                pendingMovementCost = distance;
                Debug.Log($"{characterName} moving directly to ({x}, {y})");
            }
            
            IsMoving = true;
        }

        /// <summary>
        /// Smoothly moves character toward target position
        /// </summary>
        private void MoveTowardsTarget()
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            // Calculate current grid position based on actual world position
            int currentGridX = Mathf.FloorToInt(transform.position.x);
            int currentGridY = Mathf.FloorToInt(transform.position.z);

            // Check if we crossed into a new cell during movement
            if (currentGridX != lastCellX || currentGridY != lastCellY)
            {
                // We entered a new cell!
                Debug.Log($"[PATH] {characterName} crossed from ({lastCellX}, {lastCellY}) into ({currentGridX}, {currentGridY})");

                // Check for hazards when passing through this cell
                // Apply status effects (but not damage) when passing through hazard tiles
                if (gridManager != null && hazardManager != null)
                {
                    GridCell enteredCell = gridManager.GetCell(currentGridX, currentGridY);
                    if (enteredCell != null && enteredCell.Hazard != null)
                    {
                        Unit unit = GetComponent<Unit>();
                        if (unit != null)
                        {
                            // Apply status effects when passing through (no damage)
                            hazardManager.ApplyStatusEffectFromPassingThrough(unit, enteredCell);
                        }
                    }
                }

                // Update last cell position
                lastCellX = currentGridX;
                lastCellY = currentGridY;
            }

            // Check if we've arrived at current waypoint
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                
                // If following a path, move to next waypoint
                if (currentPath != null && currentPathIndex < currentPath.Count - 1)
                {
                    currentPathIndex++;
                    targetPosition = currentPath[currentPathIndex].WorldPosition + Vector3.up * 0.5f;
                    Debug.Log($"[PATH] {characterName} reached waypoint {currentPathIndex}/{currentPath.Count}, continuing to next");
                }
                else
                {
                    // Reached final destination
                    IsMoving = false;

                    // NOW update the grid position and occupancy
                    UpdateGridOccupancy(destinationGridX, destinationGridY);
                    gridX = destinationGridX;
                    gridY = destinationGridY;

                    Debug.Log($"{characterName} arrived at final destination ({gridX}, {gridY})");

                    // CRITICAL: Check for hazards at final destination
                    // This ensures units take damage when landing on a hazard tile
                    // NOTE: This applies to ALL units (both player and enemy/AI units)
                    Unit unit = GetComponent<Unit>();
                    if (unit == null)
                    {
                        Debug.LogWarning($"[MOVEMENT COMPLETE] {characterName} - Unit component is null");
                    }
                    else if (hazardManager == null)
                    {
                        Debug.LogWarning($"[MOVEMENT COMPLETE] {unit.UnitName} - HazardManager is null, cannot check for hazards");
                    }
                    else if (gridManager == null)
                    {
                        Debug.LogWarning($"[MOVEMENT COMPLETE] {unit.UnitName} - GridManager is null, cannot check for hazards");
                    }
                    else if (!gridManager.IsValidGridPosition(gridX, gridY))
                    {
                        Debug.LogWarning($"[MOVEMENT COMPLETE] {unit.UnitName} - Invalid grid position ({gridX}, {gridY})");
                    }
                    else
                    {
                        Debug.Log($"[MOVEMENT COMPLETE] {unit.UnitName} checking for hazards at destination ({gridX}, {gridY})");
                        GridCell destinationCell = gridManager.GetCell(gridX, gridY);
                        if (destinationCell == null)
                        {
                            Debug.LogWarning($"[MOVEMENT COMPLETE] {unit.UnitName} - Destination cell at ({gridX}, {gridY}) is null!");
                        }
                        else if (destinationCell.Hazard == null)
                        {
                            Debug.Log($"[MOVEMENT COMPLETE] {unit.UnitName} landed at ({gridX}, {gridY}) - no hazard on cell");
                        }
                        else
                        {
                            Debug.Log($"[MOVEMENT COMPLETE] {unit.UnitName} landed on {destinationCell.Hazard.Type} hazard at ({gridX}, {gridY})");
                            hazardManager.ApplyHazardDamageToUnit(unit, destinationCell);
                        }
                    }

                    // Clear path
                    currentPath = null;
                    currentPathIndex = 0;
                    
                    // Trigger callback if one was provided
                    Action callback = onMovementComplete;
                    onMovementComplete = null; // Clear callback BEFORE invoking to prevent re-entry
                    
                    // Clear pending movement cost since movement completed successfully
                    pendingMovementCost = 0;
                    
                    // Invoke callback - this should spend movement points
                    if (callback != null)
                    {
                        try
                        {
                            callback.Invoke();
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"{characterName} movement callback threw exception: {e.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a grid position is within movement range
        /// </summary>
        public bool IsInMovementRange(int targetX, int targetY)
        {
            int distance = Mathf.Abs(targetX - gridX) + Mathf.Abs(targetY - gridY);
            return distance <= movementRange;
        }

        /// <summary>
        /// Updates grid cell occupancy when moving
        /// </summary>
        private void UpdateGridOccupancy(int newX, int newY)
        {
            // Use cached gridManager reference
            if (gridManager == null) return;

            // Clear old position
            GridCell oldCell = gridManager.GetCell(gridX, gridY);
            if (oldCell != null && oldCell.OccupyingUnit == GetComponent<Unit>())
            {
                oldCell.OccupyingUnit = null;
            }

            // Set new position
            GridCell newCell = gridManager.GetCell(newX, newY);
            if (newCell != null)
            {
                newCell.OccupyingUnit = GetComponent<Unit>();
            }
        }
    }
}