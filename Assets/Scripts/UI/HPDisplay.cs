using UnityEngine;
using UnityEngine.UI;
using Riftbourne.Characters;
using Riftbourne.Core;
using System.Collections;

namespace Riftbourne.UI
{
    public class HPDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Unit unit;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Text hpText;

        [Header("Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2, 0);
        [SerializeField] private float missDisplayDuration = 1.5f;

        private CameraService cameraService;
        private Coroutine missDisplayCoroutine;

        private void Awake()
        {
            unit = GetComponentInParent<Unit>();
            cameraService = CameraService.Instance;
            CreateHPDisplay();
        }

        private void OnEnable()
        {
            if (unit != null)
            {
                unit.OnHPChanged += UpdateHPDisplay;
                // Update immediately when enabled
                UpdateHPDisplay(unit.CurrentHP, unit.MaxHP);
            }
            
            // Subscribe to attack miss events
            GameEvents.OnAttackMissed += OnAttackMissed;
        }

        private void OnDisable()
        {
            if (unit != null)
            {
                unit.OnHPChanged -= UpdateHPDisplay;
            }
            
            // Unsubscribe from attack miss events
            GameEvents.OnAttackMissed -= OnAttackMissed;
        }

        private void CreateHPDisplay()
        {
            // Create canvas for this unit
            GameObject canvasObj = new GameObject("HPCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = offset;

            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100; // Render on top of everything

            // Add GraphicRaycaster so UI renders properly
            canvasObj.AddComponent<GraphicRaycaster>();

            // Set canvas to a reasonable world size
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2, 1); // 2 units wide, 1 unit tall in world space

            // Add a background image so we can see the canvas
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Create text object
            GameObject textObj = new GameObject("HPText");
            textObj.transform.SetParent(canvasObj.transform);
            textObj.transform.localPosition = Vector3.zero;

            hpText = textObj.AddComponent<Text>();
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpText.fontSize = 32;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.color = Color.white;
            hpText.resizeTextForBestFit = true;
            hpText.resizeTextMinSize = 10;
            hpText.resizeTextMaxSize = 32;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero; // Fill parent

            // Make canvas face camera initially
            if (cameraService != null && cameraService.MainCamera != null)
            {
                canvasObj.transform.LookAt(cameraService.MainCamera.transform);
                canvasObj.transform.Rotate(0, 180, 0);
            }

            Debug.Log($"HPDisplay created for {unit.UnitName}: Canvas size = {canvasRect.sizeDelta}, Position = {canvasObj.transform.position}");
        }

        private void Update()
        {
            // Only update camera facing - HP updates are event-driven
            if (canvas != null && cameraService != null && cameraService.MainCamera != null)
            {
                canvas.transform.LookAt(cameraService.MainCamera.transform);
                canvas.transform.Rotate(0, 180, 0);
            }
        }

        /// <summary>
        /// Event handler for HP changes. Updates the display text and color.
        /// </summary>
        private void UpdateHPDisplay(int currentHP, int maxHP)
        {
            if (unit == null || hpText == null) return;

            hpText.text = $"{unit.UnitName}\n{currentHP}/{maxHP}";

            // Change color based on HP percentage
            float hpPercent = (float)currentHP / maxHP;
            if (hpPercent > 0.5f)
                hpText.color = Color.green;
            else if (hpPercent > 0.25f)
                hpText.color = Color.yellow;
            else
                hpText.color = Color.red;
        }

        /// <summary>
        /// Event handler for when an attack misses this unit.
        /// Displays "Missed!" temporarily on the HP display.
        /// </summary>
        private void OnAttackMissed(Unit targetUnit)
        {
            // Only show miss if this is the unit that was missed
            if (targetUnit == unit && hpText != null)
            {
                // Stop any existing miss display coroutine
                if (missDisplayCoroutine != null)
                {
                    StopCoroutine(missDisplayCoroutine);
                }
                
                // Start new miss display
                missDisplayCoroutine = StartCoroutine(ShowMissedMessage());
            }
        }

        /// <summary>
        /// Coroutine that displays "Missed!" message temporarily, then restores HP display.
        /// </summary>
        private IEnumerator ShowMissedMessage()
        {
            if (hpText == null || unit == null) yield break;

            // Store original text and color
            string originalText = hpText.text;
            Color originalColor = hpText.color;

            // Show "Missed!" message
            hpText.text = "Missed!";
            hpText.color = Color.yellow; // Yellow color for miss indication

            // Wait for the display duration
            yield return new WaitForSeconds(missDisplayDuration);

            // Restore original HP display
            if (hpText != null && unit != null)
            {
                UpdateHPDisplay(unit.CurrentHP, unit.MaxHP);
            }

            missDisplayCoroutine = null;
        }
    }
}