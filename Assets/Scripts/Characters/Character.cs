using UnityEngine;
using Riftbourne.Grid;
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
                Debug.Log($"{characterName} following path of {currentPath.Count} cells to ({x}, {y})");
            }
            else
            {
                currentPath = null;
                targetPosition = worldPosition + Vector3.up * 0.5f;
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

                Unit unit = GetComponent<Unit>();
                if (unit != null)
                {
                    HazardManager hazardManager = FindFirstObjectByType<HazardManager>();
                    GridManager gridManager = FindFirstObjectByType<GridManager>();

                    if (hazardManager != null && gridManager != null &&
                        gridManager.IsValidGridPosition(currentGridX, currentGridY))
                    {
                        GridCell enteredCell = gridManager.GetCell(currentGridX, currentGridY);
                        if (enteredCell != null && enteredCell.Hazard != null)
                        {
                            Debug.Log($"[PATH] {unit.UnitName} takes hazard damage in cell ({currentGridX}, {currentGridY})");
                            hazardManager.ApplyHazardDamageToUnit(unit, enteredCell);
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

                    // Clear path
                    currentPath = null;
                    currentPathIndex = 0;
                    
                    // Trigger callback if one was provided
                    Action callback = onMovementComplete;
                    onMovementComplete = null; // Clear callback
                    callback?.Invoke();
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
            GridManager gridManager = FindFirstObjectByType<GridManager>();
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