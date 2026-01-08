using UnityEngine;
using UnityEngine.InputSystem;

namespace Riftbourne.Core
{
    public class CameraController : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private Transform pivotPoint;

        [Header("Mouse Control Settings")]
        [SerializeField] private float panSpeed = 2.0f;
        [SerializeField] private float rotationSensitivity = 2.0f;
        [SerializeField] private float zoomSpeed = 2.0f;
        [SerializeField] private float minZoomDistance = 5f;
        [SerializeField] private float maxZoomDistance = 30f;
        [SerializeField] private float minTiltAngle = 15f;
        [SerializeField] private float maxTiltAngle = 60f;
        [SerializeField] private float groundPlaneY = 0f;

        [Header("Camera Settings")]
        [SerializeField] private float cameraDistance = 15f;
        [SerializeField] private float cameraHeight = 10f;

        private PlayerInputActions inputActions;
        private float currentRotation = 45f;
        private float currentTiltAngle = 45f;
        private Mouse mouse;

        private void Awake()
        {
            // Initialize the new Input System
            inputActions = new PlayerInputActions();
            mouse = Mouse.current;
        }

        private void OnEnable()
        {
            inputActions?.Gameplay.Enable();
        }

        private void OnDisable()
        {
            inputActions?.Gameplay.Disable();
        }

        private void Start()
        {
            SetupCamera();
        }

        private void Update()
        {
            HandleRotationInput();
            HandleMousePan();
            HandleMouseRotation();
            HandleZoom();
        }

        private void SetupCamera()
        {
            if (pivotPoint == null)
            {
                GameObject pivot = new GameObject("CameraPivot");
                pivot.transform.position = new Vector3(5f, 0f, 5f);
                pivotPoint = pivot.transform;
            }

            // Initialize camera distance and tilt
            cameraDistance = Mathf.Clamp(cameraDistance, minZoomDistance, maxZoomDistance);
            UpdateCameraTilt();
            UpdateCameraPosition();
        }

        private void HandleRotationInput()
        {
            // Read the CameraRotate action value (keyboard Q/E)
            // Safety check - if inputActions is null, skip
            if (inputActions == null)
                return;

            float rotationInput = inputActions.Gameplay.CameraRotate.ReadValue<float>();

            if (rotationInput != 0f)
            {
                currentRotation += rotationInput * rotationSpeed * Time.deltaTime;
                UpdateCameraPosition();
            }
        }

        private void HandleMousePan()
        {
            if (mouse == null)
                return;

            // Check if right mouse button is pressed
            bool rightMousePressed = mouse.rightButton.isPressed;

            if (rightMousePressed)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                
                if (mouseDelta.magnitude > 0.01f)
                {
                    // Convert mouse delta to world space movement
                    // Get camera's right and forward vectors projected onto XZ plane
                    Vector3 cameraRight = transform.right;
                    cameraRight.y = 0f;
                    cameraRight.Normalize();

                    Vector3 cameraForward = transform.forward;
                    cameraForward.y = 0f;
                    cameraForward.Normalize();

                    // Pan in camera's local XZ space (inverted up/down, normal left/right)
                    // Mouse delta is already per-frame, so we use sensitivity directly
                    Vector3 panDirection = (-cameraForward * mouseDelta.y - cameraRight * mouseDelta.x) * panSpeed * 0.01f;
                    pivotPoint.position += panDirection;

                    UpdateCameraPosition();
                }
            }
        }

        private void HandleMouseRotation()
        {
            if (mouse == null)
                return;

            // Check if middle mouse button is pressed
            bool middleMousePressed = mouse.middleButton.isPressed;

            if (middleMousePressed)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();

                if (mouseDelta.magnitude > 0.01f)
                {
                    // Horizontal rotation (around Y-axis)
                    // Mouse delta is already per-frame, so we use sensitivity directly
                    currentRotation += mouseDelta.x * rotationSensitivity * 0.1f;

                    // Vertical rotation (tilt angle) - clamp to reasonable limits
                    // Prevent tilt from going to ground level (use minTiltAngle as minimum)
                    currentTiltAngle -= mouseDelta.y * rotationSensitivity * 0.1f;
                    currentTiltAngle = Mathf.Clamp(currentTiltAngle, minTiltAngle, 90f);

                    UpdateCameraPosition();
                }
            }
        }

        private void HandleZoom()
        {
            if (inputActions == null)
                return;

            float zoomInput = inputActions.Gameplay.CameraZoom.ReadValue<float>();

            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                // Zoom in/out by adjusting camera distance
                cameraDistance -= zoomInput * zoomSpeed;
                cameraDistance = Mathf.Clamp(cameraDistance, minZoomDistance, maxZoomDistance);

                // Update tilt based on zoom level
                UpdateCameraTilt();
                UpdateCameraPosition();
            }
        }

        private void UpdateCameraTilt()
        {
            // Normalize zoom distance (0 = min zoom, 1 = max zoom)
            float normalizedZoom = (cameraDistance - minZoomDistance) / (maxZoomDistance - minZoomDistance);
            normalizedZoom = Mathf.Clamp01(normalizedZoom);

            // Interpolate tilt angle based on zoom
            // When zoomed in (close): use minTiltAngle (more level)
            // When zoomed out (far): use maxTiltAngle (tilt down more)
            currentTiltAngle = Mathf.Lerp(minTiltAngle, maxTiltAngle, normalizedZoom);
        }

        private void UpdateCameraPosition()
        {
            // Use spherical coordinates to position camera
            float radians = currentRotation * Mathf.Deg2Rad;
            float tiltRadians = currentTiltAngle * Mathf.Deg2Rad;

            // Calculate position using spherical coordinates
            // Horizontal angle (azimuth) around Y-axis
            float x = Mathf.Sin(radians) * cameraDistance * Mathf.Cos(tiltRadians);
            float z = Mathf.Cos(radians) * cameraDistance * Mathf.Cos(tiltRadians);
            
            // Vertical component based on tilt angle
            float y = Mathf.Sin(tiltRadians) * cameraDistance;

            // Position camera relative to pivot point
            transform.position = pivotPoint.position + new Vector3(x, y, z);
            
            // Make camera look at pivot point
            transform.LookAt(pivotPoint.position);
        }
    }
}