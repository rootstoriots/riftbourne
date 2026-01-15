using UnityEngine;
using UnityEngine.InputSystem;
using Riftbourne.Core;

namespace Riftbourne.Grid
{
    /// <summary>
    /// Handles hover feedback for grid cells and units.
    /// Shows a subtle highlight when mousing over cells or units.
    /// </summary>
    public class CellHoverHandler : MonoBehaviour
    {
        [Header("Hover Settings")]
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.6f); // White, more visible
        [SerializeField] private float hoverHeight = 0.12f; // Well above range highlights (0.02f) so it's clearly visible on top

        private GameObject hoverHighlight;
        private GridManager gridManager;
        private GridCell currentHoverCell;
        private CameraService cameraService;
        private bool enabled = true; // Flag to enable/disable hover handler

        private void Awake()
        {
            gridManager = ManagerRegistry.Get<GridManager>();
            cameraService = CameraService.Instance;
        }

        private void Start()
        {
            Debug.Log("CellHoverHandler: Starting...");
            
            // Try to get gridManager if not set
            if (gridManager == null)
            {
                gridManager = ManagerRegistry.Get<GridManager>();
            }
            
            if (gridManager == null)
            {
                Debug.LogError("CellHoverHandler: GridManager not found!");
                return;
            }
            
            // Try to get cameraService if not set
            if (cameraService == null)
            {
                cameraService = CameraService.Instance;
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

            // Create semi-transparent material with higher render queue to appear on top
            Material hoverMat = new Material(Shader.Find("Standard"));
            hoverMat.color = hoverColor;
            hoverMat.SetFloat("_Mode", 3); // Transparent mode
            hoverMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            hoverMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            hoverMat.SetInt("_ZWrite", 0); // Disable depth write for transparency
            hoverMat.DisableKeyword("_ALPHATEST_ON");
            hoverMat.EnableKeyword("_ALPHABLEND_ON");
            hoverMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            hoverMat.renderQueue = 3100; // Much higher than range visualizer (3000) to ensure it renders on top
            
            Renderer hoverRenderer = hoverHighlight.GetComponent<Renderer>();
            if (hoverRenderer != null)
            {
                hoverRenderer.material = hoverMat;
                hoverRenderer.sortingOrder = 100; // Also set sorting order for 2D sorting
                // Ensure the renderer respects the render queue
                hoverRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                hoverRenderer.receiveShadows = false;
            }

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
            // If disabled, hide highlight and return early
            if (!enabled)
            {
                if (hoverHighlight != null)
                {
                    hoverHighlight.SetActive(false);
                }
                currentHoverCell = null;
                return;
            }
            
            // Ensure hover highlight exists
            if (hoverHighlight == null)
            {
                if (gridManager != null)
                {
                    CreateHoverHighlight();
                }
                return;
            }

            if (gridManager == null || Mouse.current == null)
            {
                hoverHighlight.SetActive(false);
                return;
            }

            // Check if grid has been generated
            if (!gridManager.IsGridInitialized)
            {
                // Grid not generated yet, hide highlight
                hoverHighlight.SetActive(false);
                currentHoverCell = null;
                return;
            }

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
                hoverHighlight.SetActive(false);
                return;
            }

            // Use New Input System for mouse position
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            // Raycast to find what we're hovering over (use longer distance to ensure we hit)
            // Use LayerMask to ignore UI and other layers if needed
            int layerMask = ~0; // All layers
            if (Physics.Raycast(ray, out hit, 100f, layerMask))
            {
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
            if (hoverHighlight == null)
            {
                return;
            }

            GridCell cell = gridManager.GetCell(x, y);
            
            if (cell == null)
            {
                hoverHighlight.SetActive(false);
                currentHoverCell = null;
                return;
            }

            // Always update position and show (even if same cell, to ensure it's visible)
            currentHoverCell = cell;

            Vector3 highlightPos = cell.WorldPosition;
            highlightPos.y = hoverHeight;
            hoverHighlight.transform.position = highlightPos;
            hoverHighlight.SetActive(true);
        }

        /// <summary>
        /// Public method to get the currently hovered cell.
        /// </summary>
        public GridCell GetHoveredCell()
        {
            return currentHoverCell;
        }
        
        /// <summary>
        /// Enable or disable the hover handler.
        /// When disabled, hover highlights will not be shown.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                // Hide highlight when disabling
                if (hoverHighlight != null)
                {
                    hoverHighlight.SetActive(false);
                }
                currentHoverCell = null;
            }
            Debug.Log($"CellHoverHandler: Enabled = {enabled}");
        }
    }
}
