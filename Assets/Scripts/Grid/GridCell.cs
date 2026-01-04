using UnityEngine;

namespace Riftbourne.Grid
{
    public class GridCell
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public Vector3 WorldPosition { get; private set; }
        public bool IsWalkable { get; set; }
        public GameObject VisualObject { get; set; }
        public Riftbourne.Characters.Unit OccupyingUnit { get; set; }

        public GridCell(int x, int y, Vector3 worldPosition)
        {
            X = x;
            Y = y;
            WorldPosition = worldPosition;
            IsWalkable = true;
            VisualObject = null;
        }

        public override string ToString()
        {
            return $"Cell ({X}, {Y}) at {WorldPosition}";
        }
    }
}
