using UnityEngine;
using UnityEngine.UI;
using Riftbourne.Characters;

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

        private void Awake()
        {
            unit = GetComponentInParent<Unit>();
            CreateHPDisplay();
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
            canvasObj.transform.LookAt(Camera.main.transform);
            canvasObj.transform.Rotate(0, 180, 0);

            Debug.Log($"HPDisplay created for {unit.UnitName}: Canvas size = {canvasRect.sizeDelta}, Position = {canvasObj.transform.position}");
        }

        private void Update()
        {
            if (unit != null && hpText != null)
            {
                hpText.text = $"{unit.UnitName}\n{unit.CurrentHP}/{unit.MaxHP}";

                // Change color based on HP percentage
                float hpPercent = (float)unit.CurrentHP / unit.MaxHP;
                if (hpPercent > 0.5f)
                    hpText.color = Color.green;
                else if (hpPercent > 0.25f)
                    hpText.color = Color.yellow;
                else
                    hpText.color = Color.red;
            }

            // Always face the camera
            if (canvas != null && Camera.main != null)
            {
                canvas.transform.LookAt(Camera.main.transform);
                canvas.transform.Rotate(0, 180, 0);
            }
        }
    }
}