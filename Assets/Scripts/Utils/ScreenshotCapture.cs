using UnityEngine;
using System.IO;

namespace Riftbourne.Utils
{
    /// <summary>
    /// Utility for capturing and saving screenshots for save files.
    /// </summary>
    public static class ScreenshotCapture
    {
        /// <summary>
        /// Capture a screenshot and save it to the specified path.
        /// </summary>
        public static bool CaptureAndSave(string filePath)
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Capture screenshot
                Texture2D screenshot = CaptureScreenshot();
                if (screenshot == null)
                {
                    Debug.LogError("ScreenshotCapture: Failed to capture screenshot");
                    return false;
                }

                // Convert to PNG bytes
                byte[] pngData = screenshot.EncodeToPNG();
                if (pngData == null || pngData.Length == 0)
                {
                    Debug.LogError("ScreenshotCapture: Failed to encode screenshot to PNG");
                    Object.Destroy(screenshot);
                    return false;
                }

                // Save to file
                File.WriteAllBytes(filePath, pngData);
                Debug.Log($"ScreenshotCapture: Saved screenshot to {filePath}");

                // Clean up
                Object.Destroy(screenshot);

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ScreenshotCapture: Failed to save screenshot to {filePath}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Capture a screenshot as a Texture2D without UI.
        /// Uses the main camera's RenderTexture to exclude UI elements.
        /// </summary>
        public static Texture2D CaptureScreenshot()
        {
            try
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("ScreenshotCapture: Main camera not found!");
                    return null;
                }

                int width = Screen.width;
                int height = Screen.height;

                // Create a RenderTexture to capture the camera view without UI
                RenderTexture renderTexture = new RenderTexture(width, height, 24);
                RenderTexture previousRT = mainCamera.targetTexture;

                // Set camera to render to our texture
                mainCamera.targetTexture = renderTexture;
                mainCamera.Render();

                // Restore camera's previous target
                mainCamera.targetTexture = previousRT;

                // Read pixels from RenderTexture
                RenderTexture.active = renderTexture;
                Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();

                // Clean up
                RenderTexture.active = null;
                renderTexture.Release();
                Object.Destroy(renderTexture);

                return screenshot;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ScreenshotCapture: Failed to capture screenshot: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load a screenshot from file as a Texture2D.
        /// </summary>
        public static Texture2D LoadScreenshot(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"ScreenshotCapture: Screenshot file not found: {filePath}");
                    return null;
                }

                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);
                
                if (texture.LoadImage(fileData))
                {
                    return texture;
                }
                else
                {
                    Debug.LogError($"ScreenshotCapture: Failed to load image from {filePath}");
                    Object.Destroy(texture);
                    return null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ScreenshotCapture: Failed to load screenshot from {filePath}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Capture screenshot asynchronously (for use in coroutines).
        /// </summary>
        public static System.Collections.IEnumerator CaptureScreenshotCoroutine(System.Action<Texture2D> onComplete)
        {
            // Wait for end of frame to ensure everything is rendered
            yield return new WaitForEndOfFrame();

            Texture2D screenshot = CaptureScreenshot();
            onComplete?.Invoke(screenshot);
        }
    }
}
