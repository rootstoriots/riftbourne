using UnityEngine;
using System.Collections.Generic;

namespace Riftbourne.Grid
{
    /// <summary>
    /// Visualizes movement and attack ranges on the grid with colored highlights.
    /// </summary>
    public class RangeVisualizer : MonoBehaviour
    {
        [Header("Highlight Materials")]
        [SerializeField] private Material movementRangeMaterial;
        [SerializeField] private Material attackRangeMaterial;
        [SerializeField] private Material invalidRangeMaterial;

        private List<GameObject> activeHighlights = new List<GameObject>();

        /// <summary>
        /// Show green highlights for valid movement cells.
        /// </summary>
        public void ShowMovementRange(List<GridCell> cells)
        {
            ClearHighlights();

            foreach (GridCell cell in cells)
            {
                CreateHighlight(cell, movementRangeMaterial, 0.04f);
            }

            Debug.Log($"RangeVisualizer: Showing {cells.Count} movement cells");
        }

        /// <summary>
        /// Show red highlights for valid attack targets.
        /// </summary>
        public void ShowAttackRange(List<GridCell> cells)
        {
            ClearHighlights();

            foreach (GridCell cell in cells)
            {
                CreateHighlight(cell, attackRangeMaterial, 0.04f);
            }

            Debug.Log($"RangeVisualizer: Showing {cells.Count} attack cells");
        }

        /// <summary>
        /// Clear all range highlights.
        /// </summary>
        public void ClearHighlights()
        {
            foreach (GameObject highlight in activeHighlights)
            {
                if (highlight != null)
                {
                    Destroy(highlight);
                }
            }
            activeHighlights.Clear();
        }

        private void CreateHighlight(GridCell cell, Material material, float yOffset)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlight.name = $"RangeHighlight_{cell.X}_{cell.Y}";
            highlight.transform.SetParent(transform);

            // Position at cell center (WorldPosition is already centered)
            highlight.transform.position = new Vector3(
                cell.WorldPosition.x,
                yOffset,
                cell.WorldPosition.z
            );

            // Rotate to be horizontal
            highlight.transform.rotation = Quaternion.Euler(90, 0, 0);

            // Scale to fit cell (90% size for visibility)
            highlight.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            // Remove collider (we don't need physics)
            Destroy(highlight.GetComponent<Collider>());

            // Apply material
            MeshRenderer renderer = highlight.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.material = material;
            }

            activeHighlights.Add(highlight);
        }
    }
}