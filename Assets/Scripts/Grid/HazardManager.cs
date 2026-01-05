using UnityEngine;
using Riftbourne.Characters;

namespace Riftbourne.Grid
{
    /// <summary>
    /// Manages all grid hazards (fire tiles, ice patches, poison clouds, etc.)
    /// Handles creation, updates, visuals, and damage application
    /// </summary>
    public class HazardManager : MonoBehaviour
    {
        [Header("Fire Hazard Settings")]
        [SerializeField] private Material fireMaterial;

        private GridManager gridManager;

        private void Awake()
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        /// <summary>
        /// Creates a fire hazard on the specified grid cell
        /// </summary>
        public void CreateFireHazard(int x, int y, int damagePerTurn, int duration)
        {
            if (!gridManager.IsValidGridPosition(x, y))
            {
                Debug.LogWarning($"Cannot create fire hazard at invalid position ({x}, {y})");
                return;
            }

            GridCell cell = gridManager.GetCell(x, y);

            // If fire already exists, refresh duration
            if (cell.Hazard != null && cell.Hazard.Type == HazardTile.HazardType.Fire)
            {
                cell.Hazard.RefreshDuration(duration);
                Debug.Log($"Fire at ({x}, {y}) refreshed to {cell.Hazard.RemainingTurns} turns");
                return;
            }

            // Create new fire hazard
            HazardTile fire = new HazardTile(HazardTile.HazardType.Fire, damagePerTurn, duration);
            cell.Hazard = fire;

            // Create visual
            CreateFireVisual(cell, fire);

            Debug.Log($"Fire created at ({x}, {y}) - {damagePerTurn} damage for {duration} turns");
        }

        private void CreateFireVisual(GridCell cell, HazardTile hazard)
        {
            // Create horizontal quad ABOVE the ground plane
            GameObject fireQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fireQuad.name = $"Fire_{cell.X}_{cell.Y}";
            fireQuad.transform.position = cell.WorldPosition + Vector3.up * 0.15f; // RAISED to 0.15
            fireQuad.transform.rotation = Quaternion.Euler(90, 0, 0); // Horizontal
            fireQuad.transform.localScale = Vector3.one * 0.8f; // Slightly smaller so it's clearly on the cell

            // Apply fire material (orange/red)
            Renderer renderer = fireQuad.GetComponent<Renderer>();

            if (fireMaterial != null)
            {
                renderer.material = fireMaterial;
            }
            else
            {
                // Fallback: BRIGHT orange with emission
                Material fallbackMat = new Material(Shader.Find("Standard"));
                fallbackMat.color = new Color(1f, 0.5f, 0f); // Orange
                fallbackMat.EnableKeyword("_EMISSION");
                fallbackMat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0f) * 0.5f); // Glowing orange
                renderer.material = fallbackMat;
            }

            // Remove collider (we don't need it)
            Destroy(fireQuad.GetComponent<Collider>());

            hazard.VisualObject = fireQuad;
        }

        /// <summary>
        /// Updates all hazards (called at end of each turn by TurnManager)
        /// Decrements durations and removes expired hazards
        /// </summary>
        public void UpdateHazards()
        {
            for (int x = 0; x < gridManager.GridWidth; x++)
            {
                for (int y = 0; y < gridManager.GridHeight; y++)
                {
                    GridCell cell = gridManager.GetCell(x, y);

                    if (cell.Hazard != null)
                    {
                        bool shouldRemove = cell.Hazard.DecrementDuration();

                        if (shouldRemove)
                        {
                            RemoveHazard(cell);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies hazard damage to unit at specified position
        /// Called by TurnManager when unit ends turn
        /// </summary>
        public void ApplyHazardDamage(Unit unit, int x, int y)
        {
            if (!gridManager.IsValidGridPosition(x, y))
                return;

            GridCell cell = gridManager.GetCell(x, y);

            if (cell.Hazard != null)
            {
                int damage = cell.Hazard.DamagePerTurn;
                unit.TakeDamage(damage);

                Debug.Log($"{unit.UnitName} takes {damage} {cell.Hazard.Type} damage at ({x}, {y})");
            }
        }

        /// <summary>
        /// Removes hazard from cell and destroys visual
        /// </summary>
        private void RemoveHazard(GridCell cell)
        {
            if (cell.Hazard != null)
            {
                // Destroy visual
                if (cell.Hazard.VisualObject != null)
                {
                    Destroy(cell.Hazard.VisualObject);
                }

                Debug.Log($"Fire expired at ({cell.X}, {cell.Y})");
                cell.Hazard = null;
            }
        }
    }
}