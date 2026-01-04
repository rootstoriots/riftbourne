using UnityEngine;

namespace Riftbourne.Characters
{
    public class Character : MonoBehaviour
    {
        [Header("Character Identity")]
        [SerializeField] private string characterName = "Warrior";

        [Header("Grid Position")]
        [SerializeField] private int gridX;
        [SerializeField] private int gridY;

        [Header("Movement")]
        [SerializeField] private int movementRange = 5;
        [SerializeField] private float moveSpeed = 5f;

        [Header("Stats (Future)")]
        [SerializeField] private int maxHP = 100;
        private int currentHP;

        // Public properties
        public string CharacterName => characterName;
        public int GridX => gridX;
        public int GridY => gridY;
        public int MovementRange => movementRange;
        public bool IsMoving { get; private set; }

        // Movement state
        private Vector3 targetPosition;

        private void Awake()
        {
            currentHP = maxHP;
        }

        private void Update()
        {
            if (IsMoving)
            {
                MoveTowardsTarget();
            }
        }

        /// <summary>
        /// Sets the character's grid position and snaps to world position
        /// </summary>
        public void SetGridPosition(int x, int y, Vector3 worldPosition)
        {
            gridX = x;
            gridY = y;
            transform.position = worldPosition + Vector3.up * 0.5f; // Offset so capsule sits on grid
            targetPosition = transform.position;
        }

        /// <summary>
        /// Initiates movement to a new grid position
        /// </summary>
        public void MoveTo(int x, int y, Vector3 worldPosition)
        {
            gridX = x;
            gridY = y;
            targetPosition = worldPosition + Vector3.up * 0.5f;
            IsMoving = true;

            Debug.Log($"{characterName} moving to grid position ({x}, {y})");
        }

        /// <summary>
        /// Smoothly moves character toward target position
        /// </summary>
        private void MoveTowardsTarget()
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            // Check if we've arrived
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                IsMoving = false;
                Debug.Log($"{characterName} arrived at destination");
            }
        }

        /// <summary>
        /// Checks if a grid position is within movement range
        /// </summary>
        public bool IsInMovementRange(int targetX, int targetY)
        {
            int distance = Mathf.Abs(targetX - gridX) + Mathf.Abs(targetY - gridY);
            return distance <= movementRange;
        }
    }
}