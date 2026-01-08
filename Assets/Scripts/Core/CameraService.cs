using UnityEngine;

namespace Riftbourne.Core
{
    /// <summary>
    /// Centralized service for accessing the main camera.
    /// Provides a singleton pattern to avoid multiple Camera.main calls.
    /// </summary>
    public class CameraService : MonoBehaviour
    {
        public static CameraService Instance { get; private set; }

        private Camera mainCamera;

        public Camera MainCamera
        {
            get
            {
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }
                return mainCamera;
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                mainCamera = Camera.main;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Refresh camera reference if it becomes null (scene changes, etc.)
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }
    }
}

