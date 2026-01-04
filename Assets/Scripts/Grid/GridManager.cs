using UnityEngine;

namespace Riftbourne.Grid
{
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 10;
        [SerializeField] private float cellSize = 1f;

        [Header("Visual Settings")]
        [SerializeField] private Color gridColor = Color.white;
        [SerializeField] private float lineWidth = 0.05f;

        private GridCell[,] grid;
        private GameObject gridParent;

        private void Start()
        {
            GenerateGrid();
            DrawGridLines();
        }

        private void GenerateGrid()
        {
            grid = new GridCell[gridWidth, gridHeight];

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 worldPos = new Vector3(x * cellSize, 0, y * cellSize);
                    grid[x, y] = new GridCell(x, y, worldPos);
                }
            }

            Debug.Log($"Grid generated: {gridWidth}x{gridHeight} with {gridWidth * gridHeight} cells");
        }

        private void DrawGridLines()
        {
            gridParent = new GameObject("GridLines");

            // Draw vertical lines (along Z axis)
            for (int x = 0; x <= gridWidth; x++)
            {
                Vector3 start = new Vector3(x * cellSize, 0.01f, 0);
                Vector3 end = new Vector3(x * cellSize, 0.01f, gridHeight * cellSize);
                CreateLine(start, end, $"VerticalLine_{x}");
            }

            // Draw horizontal lines (along X axis)
            for (int y = 0; y <= gridHeight; y++)
            {
                Vector3 start = new Vector3(0, 0.01f, y * cellSize);
                Vector3 end = new Vector3(gridWidth * cellSize, 0.01f, y * cellSize);
                CreateLine(start, end, $"HorizontalLine_{y}");
            }
        }

        private void CreateLine(Vector3 start, Vector3 end, string lineName)
        {
            GameObject lineObj = new GameObject(lineName);
            lineObj.transform.SetParent(gridParent.transform);

            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);

            // Line appearance
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = gridColor;
            lineRenderer.endColor = gridColor;

            // Make sure lines render properly
            lineRenderer.useWorldSpace = true;
        }

        public GridCell GetCell(int x, int y)
        {
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                return grid[x, y];
            }
            return null;
        }
    }
}