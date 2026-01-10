using System.Collections.Generic;
using UnityEngine;
using Riftbourne.Grid;

namespace Riftbourne.Skills
{
    /// <summary>
    /// Calculates which cells are affected by AOE skills based on pattern type.
    /// </summary>
    public static class AOECalculator
    {
        /// <summary>
        /// Get all cells affected by an AOE skill.
        /// </summary>
        /// <param name="pattern">The AOE pattern type</param>
        /// <param name="aoeType">Whether AOE is from source or true AOE</param>
        /// <param name="sourceX">Caster's X position</param>
        /// <param name="sourceY">Caster's Y position</param>
        /// <param name="targetX">Target X position (clicked location or target unit)</param>
        /// <param name="targetY">Target Y position (clicked location or target unit)</param>
        /// <param name="aoeSize">Size/radius of the AOE effect</param>
        /// <param name="gridManager">GridManager to get cells from</param>
        /// <returns>List of cells affected by the AOE</returns>
        public static List<GridCell> GetAffectedCells(
            AOEPatternType pattern,
            AOEType aoeType,
            int sourceX,
            int sourceY,
            int targetX,
            int targetY,
            int aoeSize,
            GridManager gridManager)
        {
            if (pattern == AOEPatternType.None || gridManager == null)
            {
                return new List<GridCell>();
            }

            List<GridCell> affectedCells = new List<GridCell>();

            // Determine the center point of the AOE
            int centerX, centerY;
            if (aoeType == AOEType.FromSource)
            {
                // AOE expands from caster
                centerX = sourceX;
                centerY = sourceY;
            }
            else // TrueAOE
            {
                // AOE centers on target location
                centerX = targetX;
                centerY = targetY;
            }

            switch (pattern)
            {
                case AOEPatternType.LineLimited:
                    affectedCells = GetLineLimitedCells(sourceX, sourceY, targetX, targetY, aoeSize, gridManager);
                    break;

                case AOEPatternType.LinePassthrough:
                    affectedCells = GetLinePassthroughCells(sourceX, sourceY, targetX, targetY, aoeSize, gridManager);
                    break;

                case AOEPatternType.Cloud:
                    affectedCells = GetCloudCells(centerX, centerY, aoeSize, gridManager);
                    break;

                case AOEPatternType.Fan:
                    affectedCells = GetFanCells(sourceX, sourceY, targetX, targetY, aoeSize, gridManager);
                    break;
            }

            return affectedCells;
        }

        /// <summary>
        /// Get cells for LineLimited pattern - only closest unit in line is affected.
        /// </summary>
        private static List<GridCell> GetLineLimitedCells(int sourceX, int sourceY, int targetX, int targetY, int maxLength, GridManager gridManager)
        {
            List<GridCell> cells = new List<GridCell>();
            List<GridCell> lineCells = GetLineCells(sourceX, sourceY, targetX, targetY, maxLength, gridManager);

            // Find the closest unit in the line
            GridCell closestUnitCell = null;
            int closestDistance = int.MaxValue;

            foreach (GridCell cell in lineCells)
            {
                if (cell.OccupyingUnit != null)
                {
                    int distance = Mathf.Abs(cell.X - sourceX) + Mathf.Abs(cell.Y - sourceY);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestUnitCell = cell;
                    }
                }
            }

            // If we found a unit, only affect that cell
            if (closestUnitCell != null)
            {
                cells.Add(closestUnitCell);
            }
            // If no unit found, affect the target cell (or closest cell to target)
            else if (lineCells.Count > 0)
            {
                // Add the cell closest to the target
                GridCell closestToTarget = lineCells[0];
                int minDistToTarget = int.MaxValue;
                foreach (GridCell cell in lineCells)
                {
                    int dist = Mathf.Abs(cell.X - targetX) + Mathf.Abs(cell.Y - targetY);
                    if (dist < minDistToTarget)
                    {
                        minDistToTarget = dist;
                        closestToTarget = cell;
                    }
                }
                cells.Add(closestToTarget);
            }

            return cells;
        }

        /// <summary>
        /// Get cells for LinePassthrough pattern - all cells in line are affected.
        /// </summary>
        private static List<GridCell> GetLinePassthroughCells(int sourceX, int sourceY, int targetX, int targetY, int maxLength, GridManager gridManager)
        {
            return GetLineCells(sourceX, sourceY, targetX, targetY, maxLength, gridManager);
        }

        /// <summary>
        /// Get all cells in a line from source to target (up to maxLength).
        /// Uses Bresenham line algorithm to ensure all cells are adjacent (no checkerboard pattern).
        /// </summary>
        private static List<GridCell> GetLineCells(int sourceX, int sourceY, int targetX, int targetY, int maxLength, GridManager gridManager)
        {
            List<GridCell> cells = new List<GridCell>();

            int dx = targetX - sourceX;
            int dy = targetY - sourceY;
            int absDx = Mathf.Abs(dx);
            int absDy = Mathf.Abs(dy);

            if (absDx == 0 && absDy == 0)
            {
                // Same cell - return empty (can't target self)
                return cells;
            }

            // Calculate actual distance
            int distance = Mathf.Max(absDx, absDy);

            // Limit to maxLength
            if (distance > maxLength)
            {
                // Scale down to maxLength
                float scale = (float)maxLength / distance;
                targetX = sourceX + Mathf.RoundToInt(dx * scale);
                targetY = sourceY + Mathf.RoundToInt(dy * scale);
                dx = targetX - sourceX;
                dy = targetY - sourceY;
                absDx = Mathf.Abs(dx);
                absDy = Mathf.Abs(dy);
                distance = Mathf.Max(absDx, absDy);
            }

            // Use proper Bresenham line algorithm for adjacent cells
            // This ensures all cells are connected, even for diagonals
            int x0 = sourceX;
            int y0 = sourceY;
            int x1 = targetX;
            int y1 = targetY;
            
            int x = x0;
            int y = y0;
            
            int stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
            int stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);
            
            // Recalculate deltas after potential scaling
            int deltaX = Mathf.Abs(x1 - x0);
            int deltaY = Mathf.Abs(y1 - y0);
            
            // Standard Bresenham line algorithm
            // This ensures all cells are adjacent, including diagonals
            int error = deltaX - deltaY;
            
            // Add starting point
            if (gridManager.IsValidGridPosition(x, y))
            {
                GridCell cell = gridManager.GetCell(x, y);
                if (cell != null && !cells.Contains(cell))
                {
                    cells.Add(cell);
                }
            }
            
            // Bresenham line drawing - continue until we reach target
            int maxIterations = distance + 10; // Safety margin
            int iterations = 0;
            
            while ((x != x1 || y != y1) && iterations < maxIterations)
            {
                iterations++;
                int error2 = error * 2;
                
                if (error2 > -deltaY)
                {
                    error -= deltaY;
                    x += stepX;
                }
                
                if (error2 < deltaX)
                {
                    error += deltaX;
                    y += stepY;
                }
                
                if (gridManager.IsValidGridPosition(x, y))
                {
                    GridCell cell = gridManager.GetCell(x, y);
                    if (cell != null && !cells.Contains(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }
            
            // Always add target point if we haven't reached it
            if ((x != x1 || y != y1) && gridManager.IsValidGridPosition(x1, y1))
            {
                GridCell targetCell = gridManager.GetCell(x1, y1);
                if (targetCell != null && !cells.Contains(targetCell))
                {
                    cells.Add(targetCell);
                }
            }
            
            if (iterations >= maxIterations)
            {
                Debug.LogWarning($"GetLineCells: Reached max iterations ({maxIterations}) from ({sourceX}, {sourceY}) to ({targetX}, {targetY})");
            }

            return cells;
        }

        /// <summary>
        /// Get cells for Cloud pattern - circular area around center point.
        /// Uses Chebyshev distance for diamond/square pattern (more grid-friendly than true circle).
        /// </summary>
        private static List<GridCell> GetCloudCells(int centerX, int centerY, int radius, GridManager gridManager)
        {
            List<GridCell> cells = new List<GridCell>();

            // Check all cells within radius (Chebyshev distance for grid-friendly circles)
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (!gridManager.IsValidGridPosition(x, y))
                        continue;

                    // Calculate Chebyshev distance (max of dx and dy)
                    int dx = Mathf.Abs(x - centerX);
                    int dy = Mathf.Abs(y - centerY);
                    int distance = Mathf.Max(dx, dy);

                    if (distance <= radius && distance > 0) // Don't include center cell
                    {
                        GridCell cell = gridManager.GetCell(x, y);
                        if (cell != null)
                        {
                            cells.Add(cell);
                        }
                    }
                }
            }

            return cells;
        }

        /// <summary>
        /// Get cells for Fan pattern - expanding cone from source toward target.
        /// Pattern: 1 cell at distance 1, 3 cells at distance 2, 5 cells at distance 3, etc.
        /// </summary>
        private static List<GridCell> GetFanCells(int sourceX, int sourceY, int targetX, int targetY, int maxRange, GridManager gridManager)
        {
            List<GridCell> cells = new List<GridCell>();

            // Calculate direction from source to target
            int dx = targetX - sourceX;
            int dy = targetY - sourceY;

            // If target is same as source, return empty (can't fan from self)
            if (dx == 0 && dy == 0)
            {
                return cells;
            }

            // Determine primary direction (cardinal or diagonal)
            // For fan, we need to determine which direction the fan faces
            bool isCardinal = (dx == 0 || dy == 0);
            
            // Determine step direction
            int stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
            int stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

            // Fan expands outward from source in the direction of target
            // At distance d, we affect (2d + 1) cells in a line perpendicular to the direction
            // Pattern: 3, 5, 7, 9, etc. (wider fan starting at 3)
            
            // Only support cardinal directions (horizontal or vertical)
            if (!isCardinal)
            {
                // Diagonal directions not supported - return empty
                return cells;
            }
            
            for (int distance = 1; distance <= maxRange; distance++)
            {
                int cellsAtDistance = 2 * distance + 1; // 3, 5, 7, 9, etc.
                int halfWidth = (cellsAtDistance - 1) / 2; // How many cells to each side of center

                // Calculate the center point at this distance
                int centerX = sourceX + (stepX * distance);
                int centerY = sourceY + (stepY * distance);

                // Cardinal directions only (horizontal or vertical)
                if (dx == 0) // Vertical line
                {
                    // Fan expands horizontally (perpendicular to vertical direction)
                    for (int offset = -halfWidth; offset <= halfWidth; offset++)
                    {
                        int cellX = centerX + offset;
                        int cellY = centerY;
                        
                        if (gridManager.IsValidGridPosition(cellX, cellY))
                        {
                            GridCell cell = gridManager.GetCell(cellX, cellY);
                            if (cell != null)
                            {
                                cells.Add(cell);
                            }
                        }
                    }
                }
                else // dy == 0, horizontal line
                {
                    // Fan expands vertically (perpendicular to horizontal direction)
                    for (int offset = -halfWidth; offset <= halfWidth; offset++)
                    {
                        int cellX = centerX;
                        int cellY = centerY + offset;
                        
                        if (gridManager.IsValidGridPosition(cellX, cellY))
                        {
                            GridCell cell = gridManager.GetCell(cellX, cellY);
                            if (cell != null)
                            {
                                cells.Add(cell);
                            }
                        }
                    }
                }
            }

            return cells;
        }
    }
}
