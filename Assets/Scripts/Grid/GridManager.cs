using UnityEngine;

namespace SongOfTheShattered.Grid
{
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 10;
        [SerializeField] private float cellSize = 1f;

        private void Start()
        {
            Debug.Log($"Grid Manager: {gridWidth}x{gridHeight} grid, cell size {cellSize}");
        }

        // We'll add grid generation next session
    }
}