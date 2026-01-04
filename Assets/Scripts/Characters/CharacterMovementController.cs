using UnityEngine;
using Riftbourne.Grid;

namespace Riftbourne.Characters
{
    public class CharacterMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Character character;

        private GridManager gridManager;

        private void Start()
        {
            gridManager = GridManager.Instance;

            if (character == null)
            {
                character = GetComponent<Character>();
            }

            // Place character at starting position on grid
            if (gridManager != null)
            {
                GridCell startCell = gridManager.GetCell(0, 0);
                if (startCell != null)
                {
                    character.SetGridPosition(0, 0, startCell.WorldPosition);
                    Debug.Log($"{character.CharacterName} placed at grid (0, 0)");
                }
            }
        }

        private void Update()
        {
            // Check if a cell is selected and character is not moving
            if (!character.IsMoving)
            {
                HandleMovementInput();
            }
        }

        private void HandleMovementInput()
        {
            GridCell selectedCell = gridManager.GetSelectedCell();

            if (selectedCell != null)
            {
                int targetX = selectedCell.X;
                int targetY = selectedCell.Y;

                // Check if it's a different cell than current position
                if (targetX != character.GridX || targetY != character.GridY)
                {
                    // Check if target is within movement range
                    if (character.IsInMovementRange(targetX, targetY))
                    {
                        character.MoveTo(targetX, targetY, selectedCell.WorldPosition);
                        Debug.Log($"Moving {character.CharacterName} to ({targetX}, {targetY})");
                    }
                    else
                    {
                        Debug.Log($"Target ({targetX}, {targetY}) is out of movement range!");
                    }
                }
            }
        }
    }
}