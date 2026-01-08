using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Combat;

namespace Riftbourne.Grid
{
    /// <summary>
    /// Handles pathfinding on the grid for tactical movement.
    /// Uses flood-fill algorithm to find all reachable cells within movement range.
    /// Enemies block paths, allies do not.
    /// </summary>
    public class Pathfinding
    {
        private GridManager gridManager;

        public Pathfinding(GridManager gridManager)
        {
            this.gridManager = gridManager;
        }

        /// <summary>
        /// Get all cells that are reachable within movement range.
        /// Enemies block paths, allies do not.
        /// </summary>
        public HashSet<GridCell> GetReachableCells(Unit unit, int maxRange)
        {
            HashSet<GridCell> reachable = new HashSet<GridCell>();
            
            // Validate unit and its grid position
            if (unit == null)
            {
                Debug.LogError("Pathfinding.GetReachableCells: Unit is null!");
                return reachable;
            }
            
            // Check if unit's grid position is valid
            if (!gridManager.IsValidGridPosition(unit.GridX, unit.GridY))
            {
                Debug.LogError($"Pathfinding.GetReachableCells: Unit {unit.UnitName} has invalid grid position ({unit.GridX}, {unit.GridY})! World position: {unit.transform.position}");
                return reachable;
            }
            
            Queue<PathNode> frontier = new Queue<PathNode>();

            // Start from current position
            GridCell startCell = gridManager.GetCell(unit.GridX, unit.GridY);
            if (startCell == null)
            {
                Debug.LogError($"Pathfinding.GetReachableCells: Could not get grid cell at ({unit.GridX}, {unit.GridY}) for unit {unit.UnitName}");
                return reachable;
            }

            // Track visited cells and their costs
            Dictionary<GridCell, int> costSoFar = new Dictionary<GridCell, int>();

            frontier.Enqueue(new PathNode(startCell, 0));
            costSoFar[startCell] = 0;

            while (frontier.Count > 0)
            {
                PathNode current = frontier.Dequeue();

                // Add to reachable set
                reachable.Add(current.cell);

                // Check all 4 cardinal neighbors
                foreach (GridCell neighbor in GetNeighbors(current.cell))
                {
                    // Calculate new cost
                    int newCost = current.cost + 1;

                    // Skip if beyond range
                    if (newCost > maxRange) continue;

                    // Skip if neighbor is blocked by an enemy
                    if (IsCellBlockedByEnemy(neighbor, unit)) continue;

                    // Skip if we've already found a cheaper path to this cell
                    if (costSoFar.ContainsKey(neighbor) && costSoFar[neighbor] <= newCost)
                        continue;

                    // Add to frontier
                    costSoFar[neighbor] = newCost;
                    frontier.Enqueue(new PathNode(neighbor, newCost));
                }
            }

            return reachable;
        }
        
        /// <summary>
        /// Get the actual path from start to destination.
        /// Returns list of cells to visit in order, or null if unreachable.
        /// </summary>
        public List<GridCell> GetPath(Unit unit, int targetX, int targetY)
        {
            // Validate unit and its grid position
            if (unit == null)
            {
                Debug.LogError("Pathfinding.GetPath: Unit is null!");
                return null;
            }
            
            // Check if unit's grid position is valid
            if (!gridManager.IsValidGridPosition(unit.GridX, unit.GridY))
            {
                Debug.LogError($"Pathfinding.GetPath: Unit {unit.UnitName} has invalid grid position ({unit.GridX}, {unit.GridY})! World position: {unit.transform.position}");
                return null;
            }
            
            GridCell startCell = gridManager.GetCell(unit.GridX, unit.GridY);
            GridCell endCell = gridManager.GetCell(targetX, targetY);
            
            if (startCell == null)
            {
                Debug.LogError($"Pathfinding.GetPath: Could not get start cell at ({unit.GridX}, {unit.GridY}) for unit {unit.UnitName}");
                return null;
            }
            
            if (endCell == null)
            {
                Debug.LogWarning($"Pathfinding.GetPath: Target cell ({targetX}, {targetY}) is invalid or out of bounds");
                return null;
            }
            
            // Use A* pathfinding
            Dictionary<GridCell, GridCell> cameFrom = new Dictionary<GridCell, GridCell>();
            Dictionary<GridCell, int> costSoFar = new Dictionary<GridCell, int>();
            
            Queue<PathNode> frontier = new Queue<PathNode>();
            frontier.Enqueue(new PathNode(startCell, 0));
            cameFrom[startCell] = null;
            costSoFar[startCell] = 0;
            
            while (frontier.Count > 0)
            {
                PathNode current = frontier.Dequeue();
                
                // Found destination!
                if (current.cell == endCell)
                {
                    return ReconstructPath(cameFrom, startCell, endCell);
                }
                
                foreach (GridCell neighbor in GetNeighbors(current.cell))
                {
                    int newCost = current.cost + 1;
                    
                    // Skip if blocked by enemy
                    if (IsCellBlockedByEnemy(neighbor, unit)) continue;
                    
                    // Skip if we found a better path
                    if (costSoFar.ContainsKey(neighbor) && costSoFar[neighbor] <= newCost)
                        continue;
                    
                    costSoFar[neighbor] = newCost;
                    cameFrom[neighbor] = current.cell;
                    frontier.Enqueue(new PathNode(neighbor, newCost));
                }
            }
            
            // No path found
            return null;
        }
        
        /// <summary>
        /// Reconstruct path from cameFrom dictionary.
        /// Returns path including start and end cells.
        /// </summary>
        private List<GridCell> ReconstructPath(Dictionary<GridCell, GridCell> cameFrom, GridCell start, GridCell end)
        {
            List<GridCell> path = new List<GridCell>();
            GridCell current = end;
            
            // Build path backwards from end to start
            while (current != null && current != start)
            {
                path.Add(current);
                if (cameFrom.ContainsKey(current))
                {
                    current = cameFrom[current];
                }
                else
                {
                    break;
                }
            }
            
            // Add start cell
            path.Add(start);
            
            // Reverse to get path from start to end
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Get the 4 cardinal neighbors of a cell (up, down, left, right).
        /// </summary>
        private List<GridCell> GetNeighbors(GridCell cell)
        {
            List<GridCell> neighbors = new List<GridCell>();

            // Check all 4 cardinal directions
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nx = cell.X + dx[i];
                int ny = cell.Y + dy[i];

                if (gridManager.IsValidGridPosition(nx, ny))
                {
                    GridCell neighbor = gridManager.GetCell(nx, ny);
                    if (neighbor != null && neighbor.IsWalkable)
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Check if a cell is blocked by an enemy unit.
        /// Allies do NOT block movement.
        /// </summary>
        private bool IsCellBlockedByEnemy(GridCell cell, Unit movingUnit)
        {
            if (cell.OccupyingUnit == null) return false;
            if (cell.OccupyingUnit == movingUnit) return false;

            // Ally doesn't block (same faction or allied)
            FactionRelationship factionRel = FactionRelationship.Instance;
            if (factionRel == null)
            {
                factionRel = Object.FindFirstObjectByType<FactionRelationship>();
            }
            
            if (factionRel != null)
            {
                if (factionRel.AreAllied(movingUnit.Faction, cell.OccupyingUnit.Faction))
                    return false;
            }
            else
            {
                // Fallback: same faction = ally
                if (cell.OccupyingUnit.Faction == movingUnit.Faction)
                    return false;
            }

            // Enemy blocks!
            return true;
        }

        /// <summary>
        /// Helper class for pathfinding nodes.
        /// </summary>
        private class PathNode
        {
            public GridCell cell;
            public int cost;

            public PathNode(GridCell cell, int cost)
            {
                this.cell = cell;
                this.cost = cost;
            }
        }
    }
}