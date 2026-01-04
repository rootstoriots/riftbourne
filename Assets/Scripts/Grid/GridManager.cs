using UnityEngine;
using UnityEngine.InputSystem;

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

        // Grid data
        private GridCell[,] grid;
        private GameObject gridVisualsParent;

        // Selection
        private GridCell selectedCell;
        private GameObject selectionHighlight;

        // Input
        private PlayerInputActions inputActions;

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

            inputActions = new PlayerInputActions();

            GenerateGrid();
            CreateGridVisuals();
            CreateSelectionHighlight();
        }

        private void OnEnable()
        {
            inputActions.Gameplay.Enable();
        }

        private void OnDisable()
        {
            inputActions.Gameplay.Disable();
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
                    Vector3 worldPosition = new Vector3(x * cellSize, 0, y * cellSize);
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
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
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
    }
}