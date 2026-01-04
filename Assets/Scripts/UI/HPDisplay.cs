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

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // Set canvas scale to be much smaller
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            // Create text object
            GameObject textObj = new GameObject("HPText");
            textObj.transform.SetParent(canvasObj.transform);
            textObj.transform.localPosition = Vector3.zero;

            hpText = textObj.AddComponent<Text>();
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpText.fontSize = 24;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.color = Color.white;

            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);

            // Make canvas face camera
            canvasObj.transform.rotation = Quaternion.Euler(0, 180, 0);
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