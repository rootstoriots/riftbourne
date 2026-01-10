using UnityEngine;
using UnityEngine.InputSystem;
using Riftbourne.Grid;
using Riftbourne.Core;
using System.Collections.Generic;

namespace Riftbourne.Grid
{
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 10;
        [SerializeField] private float cellSize = 1f;

        [Header("Visual Settings")]
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Color normalLineColor = Color.white;
        [SerializeField] private Color selectedCellColor = Color.yellow;

        [Header("Range Visualization")]
        [SerializeField] private RangeVisualizer rangeVisualizer;

        // Grid data
        private GridCell[,] grid;
        private GameObject gridVisualsParent;
        private Pathfinding pathfinding;

        // Selection
        private GridCell selectedCell;
        private GameObject selectionHighlight;

        // Input
        private PlayerInputActions inputActions;

        // Camera service reference
        private CameraService cameraService;

        public static GridManager Instance { get; private set; }

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float CellSize => cellSize;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Register with ManagerRegistry for dependency injection
            ManagerRegistry.Register(this);

            inputActions = new PlayerInputActions();

            GenerateGrid();
            pathfinding = new Pathfinding(this);
            CreateGridVisuals();
            CreateSelectionHighlight();

            // Get camera service
            cameraService = CameraService.Instance;
        }

        private void OnEnable()
        {
            inputActions?.Gameplay.Enable();
        }

        private void OnDisable()
        {
            inputActions?.Gameplay.Disable();
        }

        private void OnDestroy()
        {
            ManagerRegistry.Unregister(this);
        }

        private void Update()
        {
            HandleCellSelection();
        }

        private void GenerateGrid()
        {
            grid = new GridCell[gridWidth, gridHeight];

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // Center cells within grid squares (0.5, 1.5, 2.5, etc.)
                    Vector3 worldPosition = new Vector3(
                        x * cellSize + cellSize * 0.5f, 
                        0, 
                        y * cellSize + cellSize * 0.5f
                    );
                    grid[x, y] = new GridCell(x, y, worldPosition);
                }
            }

            Debug.Log($"Grid generated: {gridWidth}x{gridHeight} cells");
        }

        private void CreateGridVisuals()
        {
            gridVisualsParent = new GameObject("Grid Visuals");
            gridVisualsParent.transform.parent = transform;

            // Create vertical lines (along Z-axis)
            for (int x = 0; x <= gridWidth; x++)
            {
                GameObject lineObj = new GameObject($"Vertical Line {x}");
                lineObj.transform.parent = gridVisualsParent.transform;

                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
                line.startColor = normalLineColor;
                line.endColor = normalLineColor;
                line.startWidth = 0.05f;
                line.endWidth = 0.05f;
                line.positionCount = 2;

                Vector3 startPos = new Vector3(x * cellSize, 0.01f, 0);
                Vector3 endPos = new Vector3(x * cellSize, 0.01f, gridHeight * cellSize);

                line.SetPosition(0, startPos);
                line.SetPosition(1, endPos);
            }

            // Create horizontal lines (along X-axis)
            for (int y = 0; y <= gridHeight; y++)
            {
                GameObject lineObj = new GameObject($"Horizontal Line {y}");
                lineObj.transform.parent = gridVisualsParent.transform;

                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
                line.startColor = normalLineColor;
                line.endColor = normalLineColor;
                line.startWidth = 0.05f;
                line.endWidth = 0.05f;
                line.positionCount = 2;

                Vector3 startPos = new Vector3(0, 0.01f, y * cellSize);
                Vector3 endPos = new Vector3(gridWidth * cellSize, 0.01f, y * cellSize);

                line.SetPosition(0, startPos);
                line.SetPosition(1, endPos);
            }

            Debug.Log("Grid visuals created");
        }

        private void CreateSelectionHighlight()
        {
            selectionHighlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            selectionHighlight.name = "Selection Highlight";
            selectionHighlight.transform.parent = transform;
            selectionHighlight.transform.rotation = Quaternion.Euler(90, 0, 0);
            selectionHighlight.transform.localScale = new Vector3(cellSize * 0.9f, cellSize * 0.9f, 1);

            // Create material for highlight
            Material highlightMat = new Material(Shader.Find("Sprites/Default"));
            highlightMat.color = selectedCellColor;
            selectionHighlight.GetComponent<Renderer>().material = highlightMat;

            // Remove collider (we don't need it)
            Destroy(selectionHighlight.GetComponent<Collider>());

            selectionHighlight.SetActive(false);

            Debug.Log("Selection highlight created");
        }

        private void HandleCellSelection()
        {
            // Check if mouse button was pressed this frame
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Get camera - use CameraService if available, otherwise fallback to Camera.main
                Camera cam = null;
                if (cameraService != null && cameraService.MainCamera != null)
                {
                    cam = cameraService.MainCamera;
                }
                else
                {
                    cam = Camera.main;
                }

                if (cam == null)
                {
                    Debug.LogWarning("No main camera found!");
                    return;
                }

                Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Vector3 hitPoint = hit.point;
                    int x = Mathf.FloorToInt(hitPoint.x / cellSize);
                    int z = Mathf.FloorToInt(hitPoint.z / cellSize);

                    if (IsValidGridPosition(x, z))
                    {
                        SelectCell(x, z);
                    }
                }
            }
        }

        public void SelectCell(int x, int y)
        {
            if (!IsValidGridPosition(x, y)) return;

            selectedCell = grid[x, y];

            // Position highlight
            Vector3 highlightPos = selectedCell.WorldPosition;
            highlightPos.y = 0.02f; // Slightly above grid lines
            selectionHighlight.transform.position = highlightPos;
            selectionHighlight.SetActive(true);

            Debug.Log($"Selected cell: ({x}, {y})");
        }

        public GridCell GetSelectedCell()
        {
            return selectedCell;
        }

        public GridCell GetCell(int x, int y)
        {
            if (IsValidGridPosition(x, y))
            {
                return grid[x, y];
            }
            return null;
        }

        public bool IsValidGridPosition(int x, int y)
        {
            return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
        }

        public Vector3 GetCenterPosition()
        {
            return new Vector3(
                (gridWidth * cellSize) / 2f,
                0,
                (gridHeight * cellSize) / 2f
            );
        }
        /// <summary>
        /// Get all cells that are actually reachable within movement range.
        /// Uses pathfinding to respect obstacles (enemies block, allies don't).
        /// </summary>
        public HashSet<GridCell> GetReachableCells(Characters.Unit unit, int range)
        {
            if (pathfinding == null) return new HashSet<GridCell>();
            return pathfinding.GetReachableCells(unit, range);
        }
        
        /// <summary>
        /// Get the actual path to a destination.
        /// Returns list of cells to visit in order.
        /// </summary>
        public List<GridCell> GetPath(Characters.Unit unit, int targetX, int targetY)
        {
            if (pathfinding == null) return null;
            return pathfinding.GetPath(unit, targetX, targetY);
        }
        
        /// <summary>
        /// Get all cells within movement range of a position.
        /// DEPRECATED: Use GetReachableCells instead for proper pathfinding.
        /// </summary>
        public List<GridCell> GetCellsInMovementRange(int startX, int startY, int range)
        {
            List<GridCell> cellsInRange = new List<GridCell>();

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // Calculate Manhattan distance
                    int distance = Mathf.Abs(x - startX) + Mathf.Abs(y - startY);

                    if (distance <= range && distance > 0) // Don't include current cell
                    {
                        GridCell cell = GetCell(x, y);
                        if (cell != null && cell.IsWalkable && cell.OccupyingUnit == null)
                        {
                            cellsInRange.Add(cell);
                        }
                    }
                }
            }

            return cellsInRange;
        }

        /// <summary>
        /// Get all cells within attack range of a position.
        /// Uses Chebyshev distance to allow diagonal attacks (8 directions).
        /// </summary>
        public List<GridCell> GetCellsInAttackRange(int startX, int startY, int range)
        {
            List<GridCell> cellsInRange = new List<GridCell>();

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // Calculate Chebyshev distance (max of dx and dy) to allow diagonal attacks
                    int dx = Mathf.Abs(x - startX);
                    int dy = Mathf.Abs(y - startY);
                    int distance = Mathf.Max(dx, dy);

                    if (distance <= range && distance > 0) // Don't include current cell
                    {
                        GridCell cell = GetCell(x, y);
                        if (cell != null && cell.OccupyingUnit != null)
                        {
                            cellsInRange.Add(cell);
                        }
                    }
                }
            }

            return cellsInRange;
        }

        /// <summary>
        /// Get all cells within skill range of a position.
        /// Uses Manhattan distance (sum of dx and dy) to match Skill.IsInRange().
        /// Shows all cells in range, not just occupied ones.
        /// </summary>
        public List<GridCell> GetCellsInSkillRange(int startX, int startY, int range)
        {
            List<GridCell> cellsInRange = new List<GridCell>();

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // Calculate Manhattan distance (sum of dx and dy) to match Skill.IsInRange()
                    int dx = Mathf.Abs(x - startX);
                    int dy = Mathf.Abs(y - startY);
                    int distance = dx + dy;

                    if (distance <= range && distance > 0) // Don't include current cell
                    {
                        GridCell cell = GetCell(x, y);
                        if (cell != null)
                        {
                            cellsInRange.Add(cell);
                        }
                    }
                }
            }

            return cellsInRange;
        }

        /// <summary>
        /// Show movement range visualization using proper pathfinding.
        /// </summary>
        public void ShowMovementRange(Characters.Unit unit, int range)
        {
            if (rangeVisualizer == null) return;

            HashSet<GridCell> reachableCells = GetReachableCells(unit, range);
            List<GridCell> cellList = new List<GridCell>(reachableCells);
            rangeVisualizer.ShowMovementRange(cellList);
        }
        
        /// <summary>
        /// Show movement range visualization (legacy - uses simple range check).
        /// DEPRECATED: Use ShowMovementRange(Unit, int) instead.
        /// </summary>
        public void ShowMovementRange(int startX, int startY, int range)
        {
            if (rangeVisualizer == null) return;

            List<GridCell> cells = GetCellsInMovementRange(startX, startY, range);
            rangeVisualizer.ShowMovementRange(cells);
        }

        /// <summary>
        /// Show attack range visualization.
        /// </summary>
        public void ShowAttackRange(int startX, int startY, int range)
        {
            if (rangeVisualizer == null) return;

            List<GridCell> cells = GetCellsInAttackRange(startX, startY, range);
            rangeVisualizer.ShowAttackRange(cells);
        }

        /// <summary>
        /// Show skill range visualization using Manhattan distance.
        /// </summary>
        public void ShowSkillRange(int startX, int startY, int range)
        {
            if (rangeVisualizer == null) return;

            List<GridCell> cells = GetCellsInSkillRange(startX, startY, range);
            rangeVisualizer.ShowAttackRange(cells); // Use attack range material for skills
        }

        /// <summary>
        /// Show AOE pattern visualization for a skill.
        /// </summary>
        public void ShowAOEPattern(Riftbourne.Skills.Skill skill, int sourceX, int sourceY, int targetX, int targetY)
        {
            if (rangeVisualizer == null || skill == null) return;

            if (skill.AOEType == Riftbourne.Skills.AOEType.None || skill.AOEPattern == Riftbourne.Skills.AOEPatternType.None)
            {
                // Not an AOE skill - just show range
                ShowSkillRange(sourceX, sourceY, skill.Range);
                return;
            }

            // Always clear highlights first to ensure clean display
            if (rangeVisualizer != null)
            {
                rangeVisualizer.ClearHighlights();
            }

            // Calculate affected cells using the same method as execution
            List<GridCell> affectedCells = Riftbourne.Skills.AOECalculator.GetAffectedCells(
                skill.AOEPattern,
                skill.AOEType,
                sourceX,
                sourceY,
                targetX,
                targetY,
                skill.AOESize,
                this
            );

            // Show the AOE pattern - this will display exactly what execution will affect
            if (rangeVisualizer != null && affectedCells != null && affectedCells.Count > 0)
            {
                rangeVisualizer.ShowAttackRange(affectedCells);
            }
        }

        /// <summary>
        /// Clear all range highlights.
        /// </summary>
        public void ClearRangeHighlights()
        {
            if (rangeVisualizer != null)
            {
                rangeVisualizer.ClearHighlights();
            }
        }
    }
}