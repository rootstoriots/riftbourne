using UnityEngine;
using UnityEngine.InputSystem;

namespace Riftbourne.Core
{
    public class CameraController : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private Transform pivotPoint;

        [Header("Camera Settings")]
        [SerializeField] private float cameraDistance = 15f;
        [SerializeField] private float cameraHeight = 10f;

        private PlayerInputActions inputActions;
        private float currentRotation = 45f;

        private void Awake()
        {
            // Initialize the new Input System
            inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            inputActions.Gameplay.Enable();
        }

        private void OnDisable()
        {
            inputActions.Gameplay.Disable();
        }

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
            if (pivotPoint == null)
            {
                GameObject pivot = new GameObject("CameraPivot");
                pivot.transform.position = new Vector3(5f, 0f, 5f);
                pivotPoint = pivot.transform;
            }

            UpdateCameraPosition();
        }

        private void HandleRotationInput()
        {
            // Read the CameraRotate action value
            float rotationInput = inputActions.Gameplay.CameraRotate.ReadValue<float>();

            if (rotationInput != 0f)
            {
                currentRotation += rotationInput * rotationSpeed * Time.deltaTime;
                UpdateCameraPosition();
            }
        }

        private void UpdateCameraPosition()
        {
            float radians = currentRotation * Mathf.Deg2Rad;
            float x = pivotPoint.position.x + Mathf.Sin(radians) * cameraDistance;
            float z = pivotPoint.position.z + Mathf.Cos(radians) * cameraDistance;

            transform.position = new Vector3(x, cameraHeight, z);
            transform.LookAt(pivotPoint.position);
        }
    }
}