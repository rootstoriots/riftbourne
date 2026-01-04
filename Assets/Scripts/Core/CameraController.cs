using UnityEngine;

namespace Riftbourne.Core
{
    public class CameraController : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 90f; // Degrees per second
        [SerializeField] private Transform pivotPoint;

        [Header("Camera Settings")]
        [SerializeField] private float cameraDistance = 15f;
        [SerializeField] private float cameraHeight = 10f;
        [SerializeField] private float cameraAngle = 45f;

        private float currentRotation = 45f; // Start at 45 degrees (isometric)

        private void Start()
        {
            SetupCamera();
        }

        private void Update()
        {
            HandleRotationInput();
        }

        private void SetupCamera()
        {
            // If no pivot point assigned, create one at grid center
            if (pivotPoint == null)
            {
                GameObject pivot = new GameObject("CameraPivot");
                pivot.transform.position = new Vector3(5f, 0f, 5f); // Center of 10x10 grid
                pivotPoint = pivot.transform;
            }

            // Position camera relative to pivot
            UpdateCameraPosition();
        }

        private void HandleRotationInput()
        {
            float rotationInput = 0f;

            if (Input.GetKey(KeyCode.Q))
            {
                rotationInput = -1f; // Rotate left
            }
            else if (Input.GetKey(KeyCode.E))
            {
                rotationInput = 1f; // Rotate right
            }

            if (rotationInput != 0f)
            {
                currentRotation += rotationInput * rotationSpeed * Time.deltaTime;
                UpdateCameraPosition();
            }
        }

        private void UpdateCameraPosition()
        {
            // Calculate position around pivot point
            float radians = currentRotation * Mathf.Deg2Rad;
            float x = pivotPoint.position.x + Mathf.Sin(radians) * cameraDistance;
            float z = pivotPoint.position.z + Mathf.Cos(radians) * cameraDistance;

            transform.position = new Vector3(x, cameraHeight, z);
            
            // Always look at the pivot point
            transform.LookAt(pivotPoint.position);
        }
    }
}