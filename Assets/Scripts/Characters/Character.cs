using UnityEngine;
using Riftbourne.Grid;
using System;

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
        [SerializeField] private float moveSpeed = 5f;

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
        /// Initiates movement to a new grid position
        /// </summary>
        /// <param name="onComplete">Optional callback when movement finishes</param>
        public void MoveTo(int x, int y, Vector3 worldPosition, Action onComplete = null)
        {
            // Store destination but DON'T update gridX/gridY yet
            destinationGridX = x;
            destinationGridY = y;
            targetPosition = worldPosition + Vector3.up * 0.5f;
            IsMoving = true;
            onMovementComplete = onComplete;

            // Track where we started for hazard checking
            lastCellX = gridX;
            lastCellY = gridY;

            Debug.Log($"{characterName} moving from ({gridX}, {gridY}) to ({x}, {y})");
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

            // Check if we've arrived at final destination
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                IsMoving = false;

                // NOW update the grid position and occupancy
                UpdateGridOccupancy(destinationGridX, destinationGridY);
                gridX = destinationGridX;
                gridY = destinationGridY;

                Debug.Log($"{characterName} arrived at destination ({gridX}, {gridY})");

                // Mark as moved (if this is a Unit)
                Unit unit = GetComponent<Unit>();
                if (unit != null)
                {
                    unit.MarkAsMoved();
                }

                // Trigger callback if one was provided
                Action callback = onMovementComplete;
                onMovementComplete = null; // Clear callback
                callback?.Invoke();
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