using UnityEngine;
using TMPro;
using Riftbourne.Characters;
using Riftbourne.Core;
using System.Collections;

namespace Riftbourne.UI
{
    /// <summary>
    /// Displays floating damage/healing numbers above a unit.
    /// Shows red text for damage, green text for healing.
    /// Animates upward and fades out over approximately 1 second.
    /// </summary>
    public class DamageIndicator : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float animationDuration = 1f;
        [SerializeField] private float upwardMovement = 1.5f;
        [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);

        private TextMeshPro damageText;
        private CameraService cameraService;
        private Vector3 startPosition;
        private Coroutine animationCoroutine;

        /// <summary>
        /// Create and display a damage indicator above a unit.
        /// </summary>
        /// <param name="unit">The unit to display the indicator above</param>
        /// <param name="amount">The damage or healing amount</param>
        /// <param name="isHealing">True for healing (green), false for damage (red)</param>
        public static void Show(Unit unit, int amount, bool isHealing = false)
        {
            if (unit == null || amount <= 0)
            {
                return;
            }

            // Create a new GameObject for this indicator
            GameObject indicatorObj = new GameObject($"DamageIndicator_{amount}");
            indicatorObj.transform.SetParent(unit.transform);
            indicatorObj.transform.localPosition = Vector3.zero;

            DamageIndicator indicator = indicatorObj.AddComponent<DamageIndicator>();
            indicator.Initialize(unit, amount, isHealing);
        }

        private void Initialize(Unit unit, int amount, bool isHealing)
        {
            cameraService = CameraService.Instance;
            CreateIndicator(amount, isHealing);
            StartAnimation();
        }

        private void CreateIndicator(int amount, bool isHealing)
        {
            // Create text object using TextMeshPro for 3D world space rendering
            GameObject textObj = new GameObject("DamageText");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = offset;
            startPosition = textObj.transform.position;

            damageText = textObj.AddComponent<TextMeshPro>();
            
            // TextMeshPro should have a default font, but try to find one if needed
            if (damageText.font == null)
            {
                // Try to find any TMP font asset
                TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (fonts.Length > 0)
                {
                    damageText.font = fonts[0];
                    Debug.Log($"DamageIndicator: Using font {fonts[0].name}");
                }
                else
                {
                    Debug.LogWarning("DamageIndicator: No TextMeshPro font found! Text may not render. Make sure TextMeshPro is imported.");
                }
            }
            
            damageText.text = amount.ToString();
            damageText.fontSize = 4; // World space font size
            damageText.alignment = TextAlignmentOptions.Center;
            damageText.color = isHealing ? Color.green : Color.red;
            damageText.fontStyle = FontStyles.Bold;
            
            // Ensure the text mesh is updated and visible
            damageText.ForceMeshUpdate();
            
            Debug.Log($"DamageIndicator: Created indicator for {amount} {(isHealing ? "healing" : "damage")} at {startPosition}");

            // Make text face camera initially
            UpdateCameraFacing();
        }

        private void StartAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            animationCoroutine = StartCoroutine(AnimateIndicator());
        }

        private void LateUpdate()
        {
            // Continuously face camera every frame, even when camera moves
            // Use LateUpdate to ensure it runs after position updates in the coroutine
            if (damageText != null)
            {
                UpdateCameraFacing();
            }
        }

        private IEnumerator AnimateIndicator()
        {
            float elapsed = 0f;
            Vector3 endPosition = startPosition + Vector3.up * upwardMovement;
            Color startColor = damageText.color;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;

                // Move upward
                if (damageText != null)
                {
                    damageText.transform.position = Vector3.Lerp(startPosition, endPosition, t);
                }

                // Fade out
                if (damageText != null)
                {
                    Color currentColor = startColor;
                    currentColor.a = Mathf.Lerp(1f, 0f, t);
                    damageText.color = currentColor;
                }

                // Camera facing is handled in Update() method for consistency
                yield return null;
            }

            // Destroy after animation completes
            Destroy(gameObject);
        }

        private void UpdateCameraFacing()
        {
            if (damageText == null || damageText.transform == null)
            {
                return;
            }

            // Get camera - try CameraService first, fallback to Camera.main directly
            Camera camera = null;
            if (cameraService != null && cameraService.MainCamera != null)
            {
                camera = cameraService.MainCamera;
            }
            else
            {
                camera = Camera.main;
            }

            // Ensure we have a valid camera and transform
            if (camera == null || camera.transform == null)
            {
                return;
            }

            Transform textTransform = damageText.transform;
            Transform cameraTransform = camera.transform;
            
            // Store current position before rotation (LookAt can affect position if parented)
            Vector3 currentPos = textTransform.position;
            
            // Use the same approach as HPDisplay which works correctly
            textTransform.LookAt(cameraTransform);
            textTransform.Rotate(0, 180, 0);
            
            // Restore position in case LookAt moved it
            textTransform.position = currentPos;
        }

        private void OnDestroy()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
        }
    }
}

