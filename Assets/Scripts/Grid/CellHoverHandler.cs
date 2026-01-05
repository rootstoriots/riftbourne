using UnityEngine;
using UnityEngine.InputSystem;

namespace Riftbourne.Grid
{
    /// <summary>
    /// Handles hover feedback for grid cells and units.
    /// Shows a subtle highlight when mousing over cells or units.
    /// </summary>
    public class CellHoverHandler : MonoBehaviour
    {
        [Header("Hover Settings")]
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.3f); // White, semi-transparent
        [SerializeField] private float hoverHeight = 0.025f; // Slightly above grid lines

        private GameObject hoverHighlight;
        private GridManager gridManager;
        private GridCell currentHoverCell;

        private void Start()
        {
            Debug.Log("CellHoverHandler: Starting...");
            gridManager = FindFirstObjectByType<GridManager>();
            
            if (gridManager == null)
            {
                Debug.LogError("CellHoverHandler: GridManager not found!");
                return;
            }
            
            Debug.Log($"CellHoverHandler: GridManager found, creating highlight...");
            CreateHoverHighlight();
        }

        private void CreateHoverHighlight()
        {
            hoverHighlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            hoverHighlight.name = "Hover Highlight";
            hoverHighlight.transform.parent = transform;
            hoverHighlight.transform.rotation = Quaternion.Euler(90, 0, 0);
            hoverHighlight.transform.localScale = new Vector3(gridManager.CellSize * 0.95f, gridManager.CellSize * 0.95f, 1);

            // Create semi-transparent material
            Material hoverMat = new Material(Shader.Find("Sprites/Default"));
            hoverMat.color = hoverColor;
            hoverHighlight.GetComponent<Renderer>().material = hoverMat;

            // Remove collider
            Destroy(hoverHighlight.GetComponent<Collider>());

            hoverHighlight.SetActive(false);

            Debug.Log("Hover highlight created");
        }

        private void Update()
        {
            UpdateHoverHighlight();
        }

        private void UpdateHoverHighlight()
        {
            if (gridManager == null || Camera.main == null || Mouse.current == null)
            {
                hoverHighlight?.SetActive(false);
                return;
            }

            // Use New Input System for mouse position
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            // Raycast to find what we're hovering over
            if (Physics.Raycast(ray, out hit))
            {
                // Debug first hit
                // Debug.Log($"Hit: {hit.collider.name} at {hit.point}");
                
                // Check if we hit a unit (has Unit component)
                Riftbourne.Characters.Unit hoveredUnit = hit.collider.GetComponent<Riftbourne.Characters.Unit>();
                
                if (hoveredUnit != null)
                {
                    // Hovering over a unit - show highlight at unit's grid position
                    ShowHoverAtCell(hoveredUnit.GridX, hoveredUnit.GridY);
                }
                else
                {
                    // Hovering over ground - calculate grid position from hit point
                    Vector3 hitPoint = hit.point;
                    int x = Mathf.FloorToInt(hitPoint.x);
                    int z = Mathf.FloorToInt(hitPoint.z);

                    if (gridManager.IsValidGridPosition(x, z))
                    {
                        ShowHoverAtCell(x, z);
                    }
                    else
                    {
                        hoverHighlight.SetActive(false);
                        currentHoverCell = null;
                    }
                }
            }
            else
            {
                // Not hovering over anything
                hoverHighlight.SetActive(false);
                currentHoverCell = null;
            }
        }

        private void ShowHoverAtCell(int x, int y)
        {
            GridCell cell = gridManager.GetCell(x, y);
            
            if (cell == null)
            {
                hoverHighlight.SetActive(false);
                currentHoverCell = null;
                return;
            }

            // Only update if we've moved to a different cell
            if (currentHoverCell != cell)
            {
                currentHoverCell = cell;

                Vector3 highlightPos = cell.WorldPosition;
                highlightPos.y = hoverHeight;
                hoverHighlight.transform.position = highlightPos;
                hoverHighlight.SetActive(true);
            }
        }

        /// <summary>
        /// Public method to get the currently hovered cell.
        /// </summary>
        public GridCell GetHoveredCell()
        {
            return currentHoverCell;
        }
    }
}
